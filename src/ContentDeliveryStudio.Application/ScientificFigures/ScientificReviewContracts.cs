using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Application.ScientificFigures;

public interface IScientificSemanticReviewProvider
{
    Task<ScientificProviderReviewResult> ReviewAsync(
        ScientificSemanticReviewRequest request,
        CancellationToken cancellationToken);
}

public interface IScientificVisualReviewProvider
{
    Task<ScientificProviderReviewResult> ReviewAsync(
        ScientificVisualReviewRequest request,
        CancellationToken cancellationToken);
}

public sealed record ScientificReviewEvidence(
    string SourceBlockId,
    ScientificSourceLocation Location,
    string QuotedText,
    ClaimEvidenceRole Role);

public sealed record ScientificReviewClaim(
    string ClaimId,
    ScientificClaimCategory Category,
    string NormalizedStatement,
    IReadOnlyList<ScientificReviewEvidence> Evidence);

public sealed record ScientificRenderElementSummary(
    string SpecificationItemId,
    FigureElementKind Kind,
    string ScientificMeaning,
    string? ExactContent,
    bool IsCritical);

public sealed record ScientificRenderRelationSummary(
    string SpecificationItemId,
    string SourceSpecificationItemId,
    string TargetSpecificationItemId,
    FigureRelationKind Kind,
    FigureRelationDirection Direction,
    string? Label,
    bool IsCritical);

public sealed record ScientificRenderSummary(
    string PlanId,
    Guid SpecificationId,
    int SpecificationVersion,
    IReadOnlyList<ScientificRenderElementSummary> Elements,
    IReadOnlyList<ScientificRenderRelationSummary> Relations);

public sealed record ScientificSemanticReviewRequest
{
    private ScientificSemanticReviewRequest(
        IReadOnlyList<ScientificReviewClaim> approvedClaims,
        ScientificFigureSpec specification,
        ScientificRenderSummary renderSummary)
    {
        ApprovedClaims = approvedClaims;
        Specification = specification;
        RenderSummary = renderSummary;
    }

    public IReadOnlyList<ScientificReviewClaim> ApprovedClaims { get; }

    public ScientificFigureSpec Specification { get; }

    public ScientificRenderSummary RenderSummary { get; }

    public static ScientificSemanticReviewRequest Create(
        ScientificDocumentUnderstanding understanding,
        ScientificFigureSpec specification,
        SvgRenderPlan renderPlan)
    {
        ArgumentNullException.ThrowIfNull(understanding);
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(renderPlan);
        if (understanding.UnderstandingId != specification.UnderstandingId
            || understanding.Version != specification.UnderstandingVersion
            || renderPlan.SpecificationId != specification.SpecificationId
            || renderPlan.SpecificationVersion != specification.Version)
        {
            throw new ArgumentException(
                "Semantic review inputs must share the same understanding and specification authority.");
        }

        var evidenceByClaim = specification.Elements
            .Where(item => item.Requirement != FigureContentRequirement.Forbidden)
            .Select(item => item.Provenance)
            .Concat(specification.Relations
                .Where(item => item.Requirement != FigureContentRequirement.Forbidden)
                .Select(item => (ScientificFigureProvenance?)item.Provenance))
            .Where(item => item?.Kind == ScientificProvenanceKind.ClaimEvidence)
            .GroupBy(item => item!.ClaimId!, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item!.Evidence!)
                    .DistinctBy(EvidenceIdentity)
                    .ToArray(),
                StringComparer.Ordinal);
        var claimsById = understanding.Claims.ToDictionary(
            claim => claim.ClaimId,
            StringComparer.Ordinal);
        var approvedClaims = new List<ScientificReviewClaim>();
        foreach (var pair in evidenceByClaim.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            if (!claimsById.TryGetValue(pair.Key, out var claim)
                || claim.Status != ScientificClaimStatus.Accepted)
            {
                throw new InvalidOperationException(
                    $"Figure specification references a claim that is not approved: {pair.Key}.");
            }

            var evidence = pair.Value.Select(link => new ScientificReviewEvidence(
                link.SourceBlockId,
                link.Location,
                link.QuotedText,
                link.Role)).ToArray();
            approvedClaims.Add(new ScientificReviewClaim(
                claim.ClaimId,
                claim.Category,
                claim.NormalizedStatement,
                Array.AsReadOnly(evidence)));
        }

        return new ScientificSemanticReviewRequest(
            Array.AsReadOnly(approvedClaims.ToArray()),
            specification,
            BuildRenderSummary(renderPlan));
    }

    private static ScientificRenderSummary BuildRenderSummary(SvgRenderPlan plan)
    {
        var elementsByRenderId = plan.Elements.ToDictionary(
            item => item.RenderElementId,
            StringComparer.Ordinal);
        return new ScientificRenderSummary(
            plan.PlanId,
            plan.SpecificationId,
            plan.SpecificationVersion,
            plan.Elements.Select(item => new ScientificRenderElementSummary(
                item.SourceSpecificationItemId,
                item.Kind,
                item.ScientificMeaning,
                item.ExactContent,
                item.IsCritical)).ToArray(),
            plan.Connections.Select(item => new ScientificRenderRelationSummary(
                item.SourceSpecificationItemId,
                elementsByRenderId[item.SourceRenderElementId].SourceSpecificationItemId,
                elementsByRenderId[item.TargetRenderElementId].SourceSpecificationItemId,
                item.Kind,
                item.Direction,
                item.Label,
                item.IsCritical)).ToArray());
    }

    private static string EvidenceIdentity(ClaimEvidenceLink evidence)
    {
        return $"{evidence.SourceBlockId}\n{evidence.Role}\n{evidence.QuotedText}";
    }
}

public enum ScientificVisualRegionKind
{
    Element = 0,
    Relation = 1,
    Formula = 2,
    Legend = 3,
}

public sealed record ScientificFullResolutionImage(
    string Format,
    string MimeType,
    byte[] Bytes,
    string Sha256,
    int PixelWidth,
    int PixelHeight,
    int SourcePixelWidth,
    int SourcePixelHeight);

public sealed record ScientificVisualRegionCrop(
    string CropId,
    ScientificVisualRegionKind Kind,
    string ResponsibleItemId,
    int X,
    int Y,
    int Width,
    int Height,
    string MimeType,
    byte[] Bytes);

public sealed record ScientificVisualReviewRequest
{
    private ScientificVisualReviewRequest(
        ScientificFullResolutionImage fullResolutionOutput,
        IReadOnlyList<ScientificVisualRegionCrop> regionCrops)
    {
        FullResolutionOutput = fullResolutionOutput;
        RegionCrops = regionCrops;
    }

    public ScientificFullResolutionImage FullResolutionOutput { get; }

    public IReadOnlyList<ScientificVisualRegionCrop> RegionCrops { get; }

    public static ScientificVisualReviewRequest Create(
        ScientificFullResolutionImage fullResolutionOutput,
        IReadOnlyList<ScientificVisualRegionCrop> regionCrops)
    {
        ArgumentNullException.ThrowIfNull(fullResolutionOutput);
        ArgumentNullException.ThrowIfNull(regionCrops);
        if (fullResolutionOutput.Bytes is null || fullResolutionOutput.Bytes.Length == 0)
        {
            throw new ArgumentException("Full-resolution review output cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(fullResolutionOutput.Format)
            || string.IsNullOrWhiteSpace(fullResolutionOutput.MimeType)
            || string.IsNullOrWhiteSpace(fullResolutionOutput.Sha256))
        {
            throw new ArgumentException(
                "Full-resolution review output requires format, MIME type, and hash metadata.");
        }

        if (fullResolutionOutput.PixelWidth <= 0
            || fullResolutionOutput.PixelHeight <= 0
            || fullResolutionOutput.PixelWidth != fullResolutionOutput.SourcePixelWidth
            || fullResolutionOutput.PixelHeight != fullResolutionOutput.SourcePixelHeight)
        {
            throw new ArgumentException(
                "Visual review requires the original full-resolution output.");
        }

        if (regionCrops.Any(crop => crop is null
                || crop.Bytes is null
                || crop.Bytes.Length == 0
                || !Enum.IsDefined(crop.Kind)
                || string.IsNullOrWhiteSpace(crop.CropId)
                || string.IsNullOrWhiteSpace(crop.MimeType)
                || crop.Width <= 0
                || crop.Height <= 0
                || crop.X < 0
                || crop.Y < 0
                || crop.X + crop.Width > fullResolutionOutput.PixelWidth
                || crop.Y + crop.Height > fullResolutionOutput.PixelHeight
                || string.IsNullOrWhiteSpace(crop.ResponsibleItemId)))
        {
            throw new ArgumentException(
                "Visual review crops must be non-empty, typed, bounded, and item-addressable.",
                nameof(regionCrops));
        }

        var snapshot = regionCrops.ToArray();
        if (snapshot.Select(crop => crop.CropId).Distinct(StringComparer.Ordinal).Count()
            != snapshot.Length)
        {
            throw new ArgumentException("Visual review crop ids must be unique.", nameof(regionCrops));
        }

        return new ScientificVisualReviewRequest(
            fullResolutionOutput with { Bytes = fullResolutionOutput.Bytes.ToArray() },
            Array.AsReadOnly(snapshot.Select(crop => crop with { Bytes = crop.Bytes.ToArray() }).ToArray()));
    }
}

public enum ScientificReviewVerdict
{
    Pass = 0,
    Fail = 1,
    Uncertain = 2,
}

public enum ScientificProviderFindingKind
{
    ScientificMismatch = 0,
    MissingElement = 1,
    VisualDefect = 2,
}

public sealed record ScientificProviderFinding(
    string Code,
    ScientificProviderFindingKind Kind,
    string ResponsibleItemId,
    string Evidence);

public sealed record ScientificProviderReviewResult(
    ScientificReviewVerdict Verdict,
    IReadOnlyList<ScientificProviderFinding> Findings,
    string ProviderTraceId);

public enum ScientificReviewLayer
{
    Semantic = 0,
    Visual = 1,
}

public sealed record ScientificReviewBlocker(
    ScientificReviewLayer Layer,
    string Code,
    string ResponsibleItemId,
    string Evidence);

public sealed record ScientificMachineReviewDecision(
    IReadOnlyList<ScientificReviewBlocker> Blockers)
{
    public bool CanProceedToGate2 => Blockers.Count == 0;
}

public sealed class ScientificReviewExecutionService
{
    private readonly IScientificSemanticReviewProvider _semanticProvider;
    private readonly IScientificVisualReviewProvider _visualProvider;

    public ScientificReviewExecutionService(
        IScientificSemanticReviewProvider semanticProvider,
        IScientificVisualReviewProvider visualProvider)
    {
        _semanticProvider = semanticProvider
            ?? throw new ArgumentNullException(nameof(semanticProvider));
        _visualProvider = visualProvider
            ?? throw new ArgumentNullException(nameof(visualProvider));
    }

    public async Task<ScientificMachineReviewDecision> ReviewAsync(
        ScientificSemanticReviewRequest semanticRequest,
        ScientificVisualReviewRequest visualRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(semanticRequest);
        ArgumentNullException.ThrowIfNull(visualRequest);
        var semantic = await ExecuteAsync(
            ScientificReviewLayer.Semantic,
            token => _semanticProvider.ReviewAsync(semanticRequest, token),
            cancellationToken);
        var visual = await ExecuteAsync(
            ScientificReviewLayer.Visual,
            token => _visualProvider.ReviewAsync(visualRequest, token),
            cancellationToken);
        return new ScientificMachineReviewDecision(
            Array.AsReadOnly([.. semantic, .. visual]));
    }

    private static async Task<IReadOnlyList<ScientificReviewBlocker>> ExecuteAsync(
        ScientificReviewLayer layer,
        Func<CancellationToken, Task<ScientificProviderReviewResult>> execute,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await execute(cancellationToken);
            return Map(layer, result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return [new ScientificReviewBlocker(
                layer,
                "provider-failure",
                layer.ToString(),
                $"Provider raised {exception.GetType().Name}.")];
        }
    }

    private static IReadOnlyList<ScientificReviewBlocker> Map(
        ScientificReviewLayer layer,
        ScientificProviderReviewResult? result)
    {
        if (!IsValid(result))
        {
            return [new ScientificReviewBlocker(
                layer,
                "invalid-provider-output",
                layer.ToString(),
                "Provider output is null, incomplete, or contains an undefined enum value.")];
        }

        var blockers = result!.Findings.Select(finding => new ScientificReviewBlocker(
            layer,
            finding.Code,
            finding.ResponsibleItemId,
            finding.Evidence)).ToList();
        if (result.Verdict != ScientificReviewVerdict.Pass)
        {
            blockers.Add(new ScientificReviewBlocker(
                layer,
                result.Verdict == ScientificReviewVerdict.Uncertain
                    ? "provider-uncertain"
                    : "provider-failed-review",
                layer.ToString(),
                $"Provider verdict was {result.Verdict}."));
        }

        return Array.AsReadOnly(blockers.ToArray());
    }

    private static bool IsValid(ScientificProviderReviewResult? result)
    {
        return result is not null
            && Enum.IsDefined(result.Verdict)
            && !string.IsNullOrWhiteSpace(result.ProviderTraceId)
            && result.Findings is not null
            && result.Findings.All(finding => finding is not null
                && Enum.IsDefined(finding.Kind)
                && !string.IsNullOrWhiteSpace(finding.Code)
                && !string.IsNullOrWhiteSpace(finding.ResponsibleItemId)
                && !string.IsNullOrWhiteSpace(finding.Evidence));
    }
}
