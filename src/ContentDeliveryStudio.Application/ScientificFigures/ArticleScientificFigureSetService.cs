using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ContentDeliveryStudio.Application.ScientificFigures;

public sealed record ArticleSourceFigureAsset(
    string AssetId,
    int PageNumber,
    int PageImageIndex,
    int PixelWidth,
    int PixelHeight,
    double PageLeft,
    double PageBottom,
    double PageWidth,
    double PageHeight,
    string Sha256,
    byte[] PngBytes);

public sealed record ArticleSourceFigureAudit(
    string SourceSha256,
    int PageCount,
    IReadOnlyList<ArticleSourceFigureAsset> Assets);

public interface IArticleSourceFigureExtractor
{
    ArticleSourceFigureAudit Extract(string sourcePdfPath);
}

public sealed record ArticleSourceEvidenceBoard(
    byte[] PngBytes,
    string Sha256,
    int PixelWidth,
    int PixelHeight,
    IReadOnlyList<string> SourceAssetIds);

public interface IArticleSourceEvidenceBoardRenderer
{
    ArticleSourceEvidenceBoard Render(ArticleSourceFigureAudit audit);
}

public interface IArticleScientificFigureCandidateRenderer
{
    ScientificSvgArtifact Render(ArticleScientificFigureCandidate candidate, int presentationAttempt);
}

public sealed record ArticleCandidateVisualContractFinding(
    string Code,
    string Evidence);

public sealed record ArticleCandidateVisualContractReport(
    IReadOnlyList<ArticleCandidateVisualContractFinding> Findings)
{
    public bool Passed => Findings.Count == 0;
}

public sealed class ArticleCandidateVisualContractReviewer
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";
    private static readonly string[] WorkflowAnnotationMarkers =
    [
        "Gate 1",
        "替代/解释来源",
        "候选图 | 非按比例",
    ];

    public ArticleCandidateVisualContractReport Review(
        ArticleScientificFigureCandidate candidate,
        ScientificSvgArtifact artifact,
        ScientificFigureExportBundle exports)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(exports);
        var findings = new List<ArticleCandidateVisualContractFinding>();
        XDocument document;
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
            };
            using var textReader = new StringReader(artifact.Svg);
            using var xmlReader = XmlReader.Create(textReader, settings);
            document = XDocument.Load(xmlReader, LoadOptions.None);
        }
        catch (XmlException exception)
        {
            return new ArticleCandidateVisualContractReport(
                [new ArticleCandidateVisualContractFinding("candidate-svg-invalid", exception.Message)]);
        }

        var root = document.Root;
        if (root?.Name != Svg + "svg")
        {
            findings.Add(new ArticleCandidateVisualContractFinding(
                "candidate-svg-root-invalid",
                "Candidate output does not have an SVG root."));
            return new ArticleCandidateVisualContractReport(findings);
        }

        var width = Number(root, "width");
        var height = Number(root, "height");
        if (!double.IsFinite(width) || !double.IsFinite(height) || width <= 0 || height <= 0)
        {
            findings.Add(new ArticleCandidateVisualContractFinding(
                "candidate-canvas-dimensions-invalid",
                "Candidate SVG requires finite positive width and height."));
        }

        var visibleText = root.Descendants(Svg + "text").Select(item => item.Value).ToArray();
        if (root.Element(Svg + "title") is null || root.Element(Svg + "desc") is null)
        {
            findings.Add(new ArticleCandidateVisualContractFinding(
                "candidate-accessibility-missing",
                "Candidate SVG requires both title and description."));
        }

        var metadata = root.Element(Svg + "metadata")?.Value;
        if (metadata is null
            || !metadata.Contains("gate1=pending", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new ArticleCandidateVisualContractFinding(
                "candidate-authority-metadata-missing",
                "Pending scientific candidates must preserve the Gate 1 boundary in SVG metadata."));
        }

        var visibleWorkflowAnnotation = visibleText.FirstOrDefault(text =>
            string.Equals(text.Trim(), candidate.ReplacementRationale.Trim(), StringComparison.Ordinal)
            || WorkflowAnnotationMarkers.Any(marker =>
                text.Contains(marker, StringComparison.OrdinalIgnoreCase)));
        if (visibleWorkflowAnnotation is not null)
        {
            findings.Add(new ArticleCandidateVisualContractFinding(
                "candidate-workflow-annotation-visible",
                "Publication artwork must keep planning rationale, source notes, and review status in audit metadata rather than visible text."));
        }

        if (candidate.SourceFigureReferences.Count == 0)
        {
            findings.Add(new ArticleCandidateVisualContractFinding(
                "candidate-source-figure-reference-missing",
                "Replacement candidate has no source-figure reference."));
        }

        foreach (var text in root.Descendants(Svg + "text"))
        {
            var x = Number(text, "x");
            var y = Number(text, "y");
            if (!double.IsFinite(x) || !double.IsFinite(y))
            {
                findings.Add(new ArticleCandidateVisualContractFinding(
                    "candidate-text-coordinate-invalid",
                    "Visible text requires finite x and y coordinates."));
            }
            else if (x < 0 || x > width || y < 0 || y > height)
            {
                findings.Add(new ArticleCandidateVisualContractFinding(
                    "candidate-text-outside-canvas",
                    $"Visible text is outside the canvas at ({x}, {y})."));
            }
        }

        var pngs = exports.Artifacts.Where(item =>
            string.Equals(item.Format, "png", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (pngs.Length != 1 || pngs[0].Bytes.Length == 0)
        {
            findings.Add(new ArticleCandidateVisualContractFinding(
                "candidate-png-missing",
                "Candidate review requires exactly one non-empty PNG."));
        }

        return new ArticleCandidateVisualContractReport(findings);
    }

    private static double Number(XElement element, string attribute)
    {
        var value = (string?)element.Attribute(attribute);
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : double.NaN;
    }
}

public sealed record ArticleCandidateRepairRecord(
    int Attempt,
    string Reason,
    string Layer);

public sealed record ArticleScientificFigureSetItemResult(
    ArticleScientificFigureCandidate Candidate,
    ScientificSvgArtifact? Svg,
    ScientificFigureExportBundle? Exports,
    ArticleSourceEvidenceBoard? EvidenceBoard,
    ArticleCandidateVisualContractReport ContractReview,
    ArticleOpticalScientificReviewReport DeterministicScientificReview,
    ScientificVisualReviewRequest VisualReviewRequest,
    ScientificProviderReviewResult VisualReview,
    IReadOnlyList<ArticleCandidateRepairRecord> Repairs,
    int PresentationAttempts)
{
    public bool PassedVisualReview =>
        ContractReview.Passed
        && DeterministicScientificReview.Passed
        && VisualReview.Verdict == ScientificReviewVerdict.Pass
        && VisualReview.Findings.Count == 0;
}

public sealed record ArticleScientificFigureSetRun(
    ArticleSourceFigureAudit SourceAudit,
    IReadOnlyList<string> RequestedCandidateIds,
    IReadOnlyList<ArticleScientificFigureSetItemResult> Items)
{
    public bool Complete
    {
        get
        {
            if (RequestedCandidateIds.Count == 0
                || Items.Count != RequestedCandidateIds.Count
                || RequestedCandidateIds.Distinct(StringComparer.Ordinal).Count()
                    != RequestedCandidateIds.Count
                || Items.Select(item => item.Candidate.CandidateId)
                    .Distinct(StringComparer.Ordinal).Count() != Items.Count)
            {
                return false;
            }

            var requested = RequestedCandidateIds.ToHashSet(StringComparer.Ordinal);
            return Items.All(item => requested.Contains(item.Candidate.CandidateId)
                && item.PassedVisualReview);
        }
    }
}

public sealed class ArticleScientificFigureSetService
{
    private const int MaximumPresentationAttempts = 3;
    private readonly IArticleSourceFigureExtractor _sourceFigureExtractor;
    private readonly IArticleSourceEvidenceBoardRenderer _evidenceBoardRenderer;
    private readonly IArticleScientificFigureCandidateRenderer _candidateRenderer;
    private readonly IScientificFigureExporter _exporter;
    private readonly IScientificVisualReviewProvider _visualReviewProvider;
    private readonly IScientificReviewImageCropper _cropper;
    private readonly ArticleCandidateVisualContractReviewer _contractReviewer;
    private readonly ArticleOpticalScientificReviewer _scientificReviewer;

    public ArticleScientificFigureSetService(
        IArticleSourceFigureExtractor sourceFigureExtractor,
        IArticleSourceEvidenceBoardRenderer evidenceBoardRenderer,
        IArticleScientificFigureCandidateRenderer candidateRenderer,
        IScientificFigureExporter exporter,
        IScientificVisualReviewProvider visualReviewProvider,
        IScientificReviewImageCropper cropper,
        ArticleCandidateVisualContractReviewer? contractReviewer = null,
        ArticleOpticalScientificReviewer? scientificReviewer = null)
    {
        _sourceFigureExtractor = sourceFigureExtractor;
        _evidenceBoardRenderer = evidenceBoardRenderer;
        _candidateRenderer = candidateRenderer;
        _exporter = exporter;
        _visualReviewProvider = visualReviewProvider;
        _cropper = cropper ?? throw new ArgumentNullException(nameof(cropper));
        _contractReviewer = contractReviewer ?? new ArticleCandidateVisualContractReviewer();
        _scientificReviewer = scientificReviewer ?? new ArticleOpticalScientificReviewer();
    }

    public async Task<ArticleScientificFigureSetRun> RunAsync(
        string sourcePdfPath,
        IReadOnlyList<ArticleScientificFigureCandidate> candidates,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePdfPath);
        ArgumentNullException.ThrowIfNull(candidates);
        if (candidates.Count == 0)
        {
            throw new ArgumentException("Article figure-set run requires candidates.", nameof(candidates));
        }

        var requestedCandidateIds = candidates.Select(candidate =>
        {
            ArgumentNullException.ThrowIfNull(candidate);
            if (string.IsNullOrWhiteSpace(candidate.CandidateId))
            {
                throw new ArgumentException(
                    "Article figure-set candidates require stable ids.",
                    nameof(candidates));
            }

            return candidate.CandidateId;
        }).ToArray();
        if (requestedCandidateIds.Distinct(StringComparer.Ordinal).Count()
            != requestedCandidateIds.Length)
        {
            throw new ArgumentException(
                "Article figure-set candidate ids must be unique.",
                nameof(candidates));
        }

        var audit = _sourceFigureExtractor.Extract(sourcePdfPath);
        var results = new List<ArticleScientificFigureSetItemResult>();
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(candidate.Kind == ArticleScientificFigureCandidateKind.SourceEvidenceBoard
                ? await ReviewEvidenceBoardAsync(candidate, audit, cancellationToken)
                : await RenderAndReviewCandidateAsync(candidate, audit, cancellationToken));
        }

        return new ArticleScientificFigureSetRun(
            audit,
            Array.AsReadOnly(requestedCandidateIds),
            Array.AsReadOnly(results.ToArray()));
    }

    private async Task<ArticleScientificFigureSetItemResult> RenderAndReviewCandidateAsync(
        ArticleScientificFigureCandidate candidate,
        ArticleSourceFigureAudit audit,
        CancellationToken cancellationToken)
    {
        var repairs = new List<ArticleCandidateRepairRecord>();
        ScientificSvgArtifact? lastSvg = null;
        ScientificFigureExportBundle? lastExports = null;
        ArticleCandidateVisualContractReport lastContract = new([]);
        ScientificProviderReviewResult lastVisual = FailedVisual("candidate-not-reviewed");
        ArticleOpticalScientificReviewReport lastScientific = _scientificReviewer.Review(
            candidate,
            artifact: null,
            audit,
            board: null);
        ScientificVisualReviewRequest? lastVisualRequest = null;
        var lastAttempt = 0;
        for (var attempt = 1; attempt <= MaximumPresentationAttempts; attempt++)
        {
            lastAttempt = attempt;
            lastSvg = _candidateRenderer.Render(candidate, attempt);
            lastExports = _exporter.Export(new ScientificFigureExportRequest(
                lastSvg,
                lastSvg.Sha256,
                Width: 1200,
                Height: 800));
            lastContract = _contractReviewer.Review(candidate, lastSvg, lastExports);
            lastScientific = _scientificReviewer.Review(candidate, lastSvg, audit, board: null);
            if (!lastScientific.Passed)
            {
                lastVisual = FailedVisual("deterministic-scientific-review-failed");
                break;
            }
            if (!lastContract.Passed)
            {
                if (attempt < MaximumPresentationAttempts)
                {
                    repairs.Add(new ArticleCandidateRepairRecord(
                        attempt,
                        string.Join(", ", lastContract.Findings.Select(item => item.Code)),
                        "layout-style"));
                }

                continue;
            }

            lastVisualRequest = BuildVisualRequest(candidate, lastExports);
            lastVisual = await ReviewVisualAsync(
                lastVisualRequest,
                cancellationToken);
            if (lastVisual.Verdict == ScientificReviewVerdict.Pass
                && lastVisual.Findings.Count == 0)
            {
                return new ArticleScientificFigureSetItemResult(
                    candidate,
                    lastSvg,
                    lastExports,
                    EvidenceBoard: null,
                    lastContract,
                    lastScientific,
                    lastVisualRequest,
                    lastVisual,
                    repairs,
                    attempt);
            }

            if (!CanRepairPresentation(lastVisual) || attempt == MaximumPresentationAttempts)
            {
                break;
            }

            repairs.Add(new ArticleCandidateRepairRecord(
                attempt,
                string.Join(", ", lastVisual.Findings.Select(item => item.Code)),
                "layout-style"));
        }

        return new ArticleScientificFigureSetItemResult(
            candidate,
            lastSvg,
            lastExports,
            EvidenceBoard: null,
            lastContract,
            lastScientific,
            lastVisualRequest ?? BuildVisualRequest(candidate, lastExports!),
            lastVisual,
            repairs,
            lastAttempt);
    }

    private async Task<ArticleScientificFigureSetItemResult> ReviewEvidenceBoardAsync(
        ArticleScientificFigureCandidate candidate,
        ArticleSourceFigureAudit audit,
        CancellationToken cancellationToken)
    {
        var board = _evidenceBoardRenderer.Render(audit);
        var sourceAssetIds = audit.Assets.Select(item => item.AssetId)
            .ToHashSet(StringComparer.Ordinal);
        var boardIsValid = board.PngBytes.Length > 0
            && board.PixelWidth > 0
            && board.PixelHeight > 0
            && board.SourceAssetIds.Count > 0
            && board.SourceAssetIds.Distinct(StringComparer.Ordinal).Count()
                == board.SourceAssetIds.Count
            && board.SourceAssetIds.All(sourceAssetIds.Contains)
            && string.Equals(board.Sha256, Hash(board.PngBytes), StringComparison.OrdinalIgnoreCase);
        var contract = new ArticleCandidateVisualContractReport(boardIsValid
            ? []
            : [new ArticleCandidateVisualContractFinding(
                "source-evidence-board-invalid",
                "Evidence board requires hashed source-faithful pixels and valid source asset ids.")]);
        var scientific = _scientificReviewer.Review(candidate, artifact: null, audit, board);
        var visualRequest = BuildVisualRequest(candidate, board);
        var visual = contract.Passed && scientific.Passed
            ? await ReviewVisualAsync(
                visualRequest,
                cancellationToken)
            : FailedVisual("source-evidence-board-contract-failed");
        return new ArticleScientificFigureSetItemResult(
            candidate,
            Svg: null,
            Exports: null,
            board,
            contract,
            scientific,
            visualRequest,
            visual,
            Repairs: [],
            PresentationAttempts: 1);
    }

    private ScientificVisualReviewRequest BuildVisualRequest(
        ArticleScientificFigureCandidate candidate,
        ScientificFigureExportBundle exports)
    {
        var png = exports.Artifacts.Single(item =>
            string.Equals(item.Format, "png", StringComparison.OrdinalIgnoreCase));
        return BuildVisualRequest(
            candidate,
            png.Bytes,
            png.Sha256,
            exports.Width,
            exports.Height);
    }

    private ScientificVisualReviewRequest BuildVisualRequest(
        ArticleScientificFigureCandidate candidate,
        ArticleSourceEvidenceBoard board) =>
        BuildVisualRequest(
            candidate,
            board.PngBytes,
            board.Sha256,
            board.PixelWidth,
            board.PixelHeight);

    private ScientificVisualReviewRequest BuildVisualRequest(
        ArticleScientificFigureCandidate candidate,
        byte[] pngBytes,
        string sha256,
        int width,
        int height)
    {
        var full = new ScientificFullResolutionImage(
            "png",
            "image/png",
            pngBytes,
            sha256,
            width,
            height,
            width,
            height);
        var regions = _scientificReviewer.BuildRegions(candidate);
        var crops = regions.Select(region =>
        {
            var bounded = Clamp(region.Region, width, height);
            var bytes = candidate.Kind == ArticleScientificFigureCandidateKind.SourceEvidenceBoard
                ? pngBytes
                : _cropper.CropPng(pngBytes, width, height, bounded);
            return new ScientificVisualRegionCrop(
                $"crop-{region.ExpectedCheck.ResponsibleItemId}",
                region.Kind,
                region.ExpectedCheck.ResponsibleItemId,
                bounded.X,
                bounded.Y,
                bounded.Width,
                bounded.Height,
                "image/png",
                bytes,
                region.ExpectedCheck);
        }).ToArray();
        return ScientificVisualReviewRequest.Create(full, crops);
    }

    private static ScientificPixelRegion Clamp(ScientificPixelRegion region, int width, int height)
    {
        var x = Math.Clamp(region.X, 0, width - 1);
        var y = Math.Clamp(region.Y, 0, height - 1);
        return new ScientificPixelRegion(
            x,
            y,
            Math.Clamp(region.Width, 1, width - x),
            Math.Clamp(region.Height, 1, height - y));
    }

    private static ScientificProviderReviewResult FailedVisual(string code) =>
        new(
            ScientificReviewVerdict.Fail,
            [new ScientificProviderFinding(
                code,
                ScientificProviderFindingKind.VisualDefect,
                "article-figure-set",
                "Visual review did not produce a passing result.")],
            "local-article-figure-set");

    private async Task<ScientificProviderReviewResult> ReviewVisualAsync(
        ScientificVisualReviewRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _visualReviewProvider.ReviewAsync(request, cancellationToken);
            if (result is null
                || !Enum.IsDefined(result.Verdict)
                || result.Findings is null
                || string.IsNullOrWhiteSpace(result.ProviderTraceId)
                || result.Findings.Any(finding => finding is null
                    || !Enum.IsDefined(finding.Kind)
                    || string.IsNullOrWhiteSpace(finding.Code)
                    || string.IsNullOrWhiteSpace(finding.ResponsibleItemId)
                    || string.IsNullOrWhiteSpace(finding.Evidence)))
            {
                return FailedVisual("invalid-visual-provider-output");
            }

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return FailedVisual("visual-provider-failure");
        }
    }

    private static bool CanRepairPresentation(ScientificProviderReviewResult review) =>
        review.Verdict == ScientificReviewVerdict.Fail
        && review.Findings.Count > 0
        && review.Findings.All(finding =>
            finding.Kind is ScientificProviderFindingKind.VisualDefect
                or ScientificProviderFindingKind.NonEvidentiaryAssetDefect);

    public static string Hash(byte[] bytes) =>
        $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";
}
