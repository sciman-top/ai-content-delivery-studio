using System.Text;
using System.Text.RegularExpressions;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Core.Sources;
using UglyToad.PdfPig;

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

    public Task<ScientificDocumentExtraction> ExtractAsync(
        ScientificDocumentExtractionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequest(request);

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
        var requiredContent = HasMissingRequiredContent(blocks)
            ? ScientificRequiredContentStatus.Missing
            : ScientificRequiredContentStatus.Complete;
        var extraction = ScientificDocumentExtraction.Create(
            request.SourceAssetId,
            request.SourceSha256,
            ScientificExtractorIdentity.Create("pdfpig-scientific-document-extractor", "1.0"),
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

    private static List<ScientificSourceBlock> ExtractPdfBlocks(
        ScientificDocumentExtractionRequest request,
        CancellationToken cancellationToken)
    {
        var blocks = new List<ScientificSourceBlock>();
        var offset = 0;
        using var document = PdfDocument.Open(request.OriginalPath!);
        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = NormalizeWhitespace(page.Text);
            if (text.Length == 0)
            {
                continue;
            }

            blocks.Add(ScientificSourceBlock.Create(
                $"page-{page.Number}-paragraph-1",
                ScientificSourceBlockKind.Paragraph,
                ScientificSourceLocation.Create(
                    page.Number,
                    $"page {page.Number}",
                    boundingRegion: null,
                    ScientificCharacterRange.Create(offset, offset + text.Length)),
                text,
                isRequired: true,
                ScientificRecoveryStatus.NotRequired));
            offset += text.Length;
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
            var kind = ClassifyTextBlock(request.SourceKind, text);
            var recoveryStatus = kind is ScientificSourceBlockKind.Formula
                or ScientificSourceBlockKind.Table
                ? ScientificRecoveryStatus.Recovered
                : ScientificRecoveryStatus.NotRequired;
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

    private static ScientificSourceBlockKind ClassifyTextBlock(
        SourceAssetKind sourceKind,
        string text)
    {
        if (sourceKind != SourceAssetKind.Markdown)
        {
            return ScientificSourceBlockKind.Paragraph;
        }

        if (text.StartsWith('#'))
        {
            return ScientificSourceBlockKind.Heading;
        }

        if ((text.StartsWith("$$", StringComparison.Ordinal)
                && text.EndsWith("$$", StringComparison.Ordinal))
            || text.StartsWith("FORMULA:", StringComparison.Ordinal))
        {
            return ScientificSourceBlockKind.Formula;
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

        return ScientificSourceBlockKind.Paragraph;
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

    private static bool HasMissingRequiredContent(
        IEnumerable<ScientificSourceBlock> blocks)
    {
        return blocks.Any(block =>
            block.IsRequired
            && block.RecoveryStatus is ScientificRecoveryStatus.Missing
                or ScientificRecoveryStatus.Uncertain);
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
}
