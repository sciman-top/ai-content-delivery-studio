using System.Text;
using System.Text.RegularExpressions;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Core.Sources;
using ContentDeliveryStudio.Infrastructure.Sources;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace ContentDeliveryStudio.Infrastructure.ScientificFigures;

public sealed partial class PdfPigScientificDocumentExtractor : IScientificDocumentExtractor
{
    private static readonly SourceAssetKind[] SupportedKinds =
    [
        SourceAssetKind.Pdf,
        SourceAssetKind.Markdown,
        SourceAssetKind.Text,
        SourceAssetKind.Paste,
    ];

    private readonly SourceIngestionBudget _budget;

    public PdfPigScientificDocumentExtractor()
        : this(SourceIngestionBudget.Default)
    {
    }

    internal PdfPigScientificDocumentExtractor(SourceIngestionBudget budget)
    {
        _budget = budget ?? throw new ArgumentNullException(nameof(budget));
    }

    public Task<ScientificDocumentExtraction> ExtractAsync(
        ScientificDocumentExtractionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequest(request);

        if (request.SourceKind == SourceAssetKind.Pdf)
        {
            _budget.ValidateSourceFile(request.OriginalPath!);
        }
        else
        {
            _budget.AddExtractedText(0, request.SourceText.Length);
        }

        var blocks = request.SourceKind == SourceAssetKind.Pdf
            ? ExtractPdfBlocks(request, cancellationToken)
            : ExtractTextBlocks(request, cancellationToken);
        var diagnostics = BuildDiagnostics(request, blocks.Count == 0);

        if (blocks.Count == 0)
        {
            blocks.Add(ScientificSourceBlock.Create(
                "unrecoverable-region-1",
                ScientificSourceBlockKind.UnrecoverableRegion,
                ScientificSourceLocation.Create(
                    pageNumber: 1,
                    section: "unrecoverable source region",
                    ScientificBoundingRegion.Create(0, 0, 1, 1),
                    characterRange: null),
                originalText: null,
                isRequired: true,
                ScientificRecoveryStatus.Missing));
        }

        AddMissingRequiredContent(request.RequiredContent, blocks);
        var requiredContent = DetermineRequiredContentStatus(blocks);
        var extraction = ScientificDocumentExtraction.Create(
            request.SourceAssetId,
            request.SourceSha256,
            ScientificExtractorIdentity.Create("pdfpig-scientific-document-extractor", "1.1"),
            ScientificExtractionQuality.Create(
                request.IsScanned,
                ocrApplied: false,
                request.ReadingOrder,
                requiredContent),
            blocks,
            diagnostics);
        return Task.FromResult(extraction);
    }

    private static void ValidateRequest(ScientificDocumentExtractionRequest request)
    {
        if (!SupportedKinds.Contains(request.SourceKind))
        {
            throw new InvalidOperationException(
                $"Scientific source kind is not supported: {request.SourceKind}.");
        }

        if (request.SourceAssetId == Guid.Empty)
        {
            throw new ArgumentException("Source asset id cannot be empty.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new ArgumentException("Display name cannot be empty.", nameof(request));
        }

        if (request.RequiredContent is null)
        {
            throw new ArgumentException("Required content cannot be null.", nameof(request));
        }

        var unsupportedRequirement = request.RequiredContent
            .FirstOrDefault(requirement => !Enum.IsDefined(requirement));
        if (!Enum.IsDefined(unsupportedRequirement))
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                unsupportedRequirement,
                "Required scientific content kind is not supported.");
        }

        if (request.SourceKind == SourceAssetKind.Pdf
            && (string.IsNullOrWhiteSpace(request.OriginalPath)
                || !File.Exists(request.OriginalPath)))
        {
            throw new FileNotFoundException(
                "Scientific PDF extraction requires an existing local file.",
                request.OriginalPath);
        }

        if (request.SourceKind != SourceAssetKind.Pdf
            && string.IsNullOrWhiteSpace(request.SourceText))
        {
            throw new ArgumentException(
                "Text-bearing scientific extraction requires source text.",
                nameof(request));
        }
    }

    private List<ScientificSourceBlock> ExtractPdfBlocks(
        ScientificDocumentExtractionRequest request,
        CancellationToken cancellationToken)
    {
        var blocks = new List<ScientificSourceBlock>();
        var offset = 0;
        var extractedCharacterCount = 0;
        using var document = PdfDocument.Open(request.OriginalPath!);
        _budget.ValidatePdfPageCount(document.NumberOfPages);
        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pageText = ContentOrderTextExtractor.GetText(page, true);
            var pageBlocks = pageText
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(NormalizeWhitespace)
                .Where(text => text.Length > 0)
                .ToArray();
            if (pageBlocks.Length == 0)
            {
                continue;
            }

            var section = $"page {page.Number}";
            for (var index = 0; index < pageBlocks.Length; index++)
            {
                var text = pageBlocks[index];
                extractedCharacterCount = _budget.AddExtractedText(extractedCharacterCount, text.Length);
                var kind = ClassifyScholarlyBlock(SourceAssetKind.Pdf, text);
                if (kind == ScientificSourceBlockKind.Heading)
                {
                    section = text;
                }

                var startOffset = offset;
                var endOffset = startOffset + text.Length;
                blocks.Add(ScientificSourceBlock.Create(
                    $"page-{page.Number}-block-{index + 1}",
                    kind,
                    ScientificSourceLocation.Create(
                        page.Number,
                        section,
                        boundingRegion: null,
                        ScientificCharacterRange.Create(startOffset, endOffset)),
                    text,
                    isRequired: true,
                    RecoveryStatusFor(kind)));
                offset = endOffset + 1;
            }
        }

        return blocks;
    }

    private static List<ScientificSourceBlock> ExtractTextBlocks(
        ScientificDocumentExtractionRequest request,
        CancellationToken cancellationToken)
    {
        var blocks = new List<ScientificSourceBlock>();
        var matches = TextBlockRegex().Matches(request.SourceText);
        var index = 0;
        foreach (Match match in matches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = match.Value.Trim();
            if (text.Length == 0)
            {
                continue;
            }

            index++;
            var kind = ClassifyScholarlyBlock(request.SourceKind, text);
            var recoveryStatus = RecoveryStatusFor(kind);
            var startOffset = match.Index + match.Value.IndexOf(text, StringComparison.Ordinal);
            blocks.Add(ScientificSourceBlock.Create(
                $"block-{index}",
                kind,
                ScientificSourceLocation.Create(
                    pageNumber: 1,
                    section: SectionFor(kind, index),
                    boundingRegion: null,
                    ScientificCharacterRange.Create(startOffset, startOffset + text.Length)),
                text,
                isRequired: true,
                recoveryStatus));
        }

        return blocks;
    }

    private static ScientificSourceBlockKind ClassifyScholarlyBlock(
        SourceAssetKind sourceKind,
        string text)
    {
        if (sourceKind == SourceAssetKind.Markdown && text.StartsWith('#'))
        {
            return ScientificSourceBlockKind.Heading;
        }

        if (text.StartsWith("TABLE:", StringComparison.OrdinalIgnoreCase)
            || HasRepeatedTableDelimiter(text))
        {
            return ScientificSourceBlockKind.Table;
        }

        if ((text.StartsWith("$$", StringComparison.Ordinal)
                && text.EndsWith("$$", StringComparison.Ordinal))
            || text.StartsWith("FORMULA:", StringComparison.OrdinalIgnoreCase)
            || FormulaRegex().IsMatch(text))
        {
            return ScientificSourceBlockKind.Formula;
        }

        if (CaptionRegex().IsMatch(text))
        {
            return ScientificSourceBlockKind.Caption;
        }

        if (CitationRegex().IsMatch(text))
        {
            return ScientificSourceBlockKind.Reference;
        }

        var lines = text.Split(
            ['\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length >= 2
            && lines.All(line => line.StartsWith('|') && line.EndsWith('|'))
            && lines.Any(line => line.Contains("---", StringComparison.Ordinal)))
        {
            return ScientificSourceBlockKind.Table;
        }

        if (lines.Length == 1 && HeadingRegex().IsMatch(text))
        {
            return ScientificSourceBlockKind.Heading;
        }

        return ScientificSourceBlockKind.Paragraph;
    }

    private static ScientificRecoveryStatus RecoveryStatusFor(ScientificSourceBlockKind kind)
    {
        return kind is ScientificSourceBlockKind.Caption
            or ScientificSourceBlockKind.Reference
            or ScientificSourceBlockKind.Formula
            or ScientificSourceBlockKind.Table
            ? ScientificRecoveryStatus.Recovered
            : ScientificRecoveryStatus.NotRequired;
    }

    private static bool HasRepeatedTableDelimiter(string text)
    {
        return text.Count(character => character is '|' or '\t') >= 2;
    }

    private static IReadOnlyList<ScientificExtractionDiagnostic> BuildDiagnostics(
        ScientificDocumentExtractionRequest request,
        bool hasNoText)
    {
        var diagnostics = new List<ScientificExtractionDiagnostic>();
        if (request.UseOcr)
        {
            diagnostics.Add(ScientificExtractionDiagnostic.Create(
                "ocr-not-supported",
                ScientificDiagnosticSeverity.Blocking,
                "OCR was requested but is not supported by the current scientific extractor."));
        }

        if (request.ReadingOrder == ScientificReadingOrderStatus.Corrupted)
        {
            diagnostics.Add(ScientificExtractionDiagnostic.Create(
                "reading-order-corrupted",
                ScientificDiagnosticSeverity.Blocking,
                "Source reading order is corrupted and cannot establish evidence order."));
        }
        else if (request.ReadingOrder == ScientificReadingOrderStatus.Uncertain)
        {
            diagnostics.Add(ScientificExtractionDiagnostic.Create(
                "reading-order-uncertain",
                ScientificDiagnosticSeverity.Blocking,
                "Source reading order could not be established reliably."));
        }

        if (request.SourceKind == SourceAssetKind.Pdf && !hasNoText)
        {
            diagnostics.Add(ScientificExtractionDiagnostic.Create(
                "pdfpig-content-order",
                ScientificDiagnosticSeverity.Information,
                "PDF text was read through PdfPig content-order extraction; scholarly structure remains heuristic and evidence-bound."));
        }

        if (hasNoText)
        {
            diagnostics.Add(ScientificExtractionDiagnostic.Create(
                "no-usable-text",
                ScientificDiagnosticSeverity.Blocking,
                "No usable source text was recovered."));
        }

        return diagnostics;
    }

    private static void AddMissingRequiredContent(
        IReadOnlyList<ScientificRequiredContentKind> requirements,
        ICollection<ScientificSourceBlock> blocks)
    {
        foreach (var requirement in requirements.Distinct())
        {
            var blockKind = requirement switch
            {
                ScientificRequiredContentKind.Formula => ScientificSourceBlockKind.Formula,
                ScientificRequiredContentKind.Table => ScientificSourceBlockKind.Table,
                ScientificRequiredContentKind.Caption => ScientificSourceBlockKind.Caption,
                ScientificRequiredContentKind.Citation => ScientificSourceBlockKind.Reference,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(requirements),
                    requirement,
                    "Required scientific content kind is not supported."),
            };
            if (blocks.Any(block => block.Kind == blockKind))
            {
                continue;
            }

            var searchedLocation = blocks.First().Location;
            blocks.Add(ScientificSourceBlock.Create(
                $"required-{requirement.ToString().ToLowerInvariant()}-missing",
                blockKind,
                searchedLocation,
                originalText: null,
                isRequired: true,
                ScientificRecoveryStatus.Missing));
        }
    }

    private static ScientificRequiredContentStatus DetermineRequiredContentStatus(
        IEnumerable<ScientificSourceBlock> blocks)
    {
        var missing = blocks.Any(block =>
            block.IsRequired
            && block.RecoveryStatus is ScientificRecoveryStatus.Missing);
        if (missing)
        {
            return ScientificRequiredContentStatus.Missing;
        }

        return blocks.Any(block =>
                block.IsRequired && block.RecoveryStatus is ScientificRecoveryStatus.Uncertain)
            ? ScientificRequiredContentStatus.Uncertain
            : ScientificRequiredContentStatus.Complete;
    }

    private static string SectionFor(ScientificSourceBlockKind kind, int index)
    {
        return kind == ScientificSourceBlockKind.Heading
            ? $"heading {index}"
            : $"block {index}";
    }

    private static string NormalizeWhitespace(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var pendingWhitespace = false;
        foreach (var character in text)
        {
            if (char.IsWhiteSpace(character))
            {
                pendingWhitespace = true;
                continue;
            }

            if (pendingWhitespace && builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(character);
            pendingWhitespace = false;
        }

        return builder.ToString();
    }

    [GeneratedRegex(@"(?ms)(?:\A|\r?\n[ \t]*\r?\n)(?<block>.*?)(?=\r?\n[ \t]*\r?\n|\z)")]
    private static partial Regex TextBlockRegex();

    [GeneratedRegex(@"^(?:fig(?:ure)?\.?|table)\s*(?:[a-z]?\d+|[ivx]+)\s*[\s.:\-]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CaptionRegex();

    [GeneratedRegex(@"^(?:\[\d+\]|\(\d+\)|doi\s*:)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CitationRegex();

    [GeneratedRegex(@"^[a-z][^.!?\r\n]{0,120}\s=\s[^.!?\r\n]{1,120}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FormulaRegex();

    [GeneratedRegex(@"^(?:\d+(?:\.\d+)*\s+)?(?:[A-Z][A-Za-z0-9\-]*\s*){1,8}$", RegexOptions.CultureInvariant)]
    private static partial Regex HeadingRegex();
}
