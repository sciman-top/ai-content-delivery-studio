using System.Security.Cryptography;
using System.Text;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Application.ScientificFigures;

public interface IScientificFigurePackageWriter
{
    byte[] Write(ScientificFigureDeliveryPackage package);
}

public sealed record ScientificGateTwoDecision(
    bool Approved,
    string Reviewer,
    string Notes,
    DateTimeOffset ReviewedAt);

public sealed record ScientificGateTwoApproval(
    Guid SpecificationId,
    int SpecificationVersion,
    string Reviewer,
    string Notes,
    DateTimeOffset ReviewedAt);

public enum ScientificRepairRecordStatus
{
    Resolved = 0,
    Unresolved = 1,
}

public sealed record ScientificRepairRecord(
    ScientificRepairAction Action,
    ScientificRepairRecordStatus Status,
    string Resolution);

public sealed record ScientificDeliveryProviderMetadata(
    ScientificReviewLayer Layer,
    string ProviderId,
    string ModelId,
    string TraceId);

public sealed record ScientificClaimEvidenceItemMap(
    string SpecificationItemId,
    string ItemKind,
    string? ClaimId,
    string? SourceBlockId,
    string? QuotedText,
    string? ConventionId);

public sealed record ScientificFigureDeliveryPackage(
    Guid SpecificationId,
    int SpecificationVersion,
    ScientificSvgArtifact Svg,
    ScientificFigureExportBundle Exports,
    ScientificFigureSpec Specification,
    IReadOnlyList<ScientificClaimEvidenceItemMap> ClaimEvidenceItemMap,
    ScientificContractReviewReport ContractReview,
    ScientificMachineReviewDecision MachineReview,
    IReadOnlyList<ScientificRepairRecord> Repairs,
    IReadOnlyList<ScientificDeliveryProviderMetadata> Providers,
    ScientificGate1Approval GateOneApproval,
    ScientificGateTwoApproval GateTwoApproval);

public sealed record ScientificFigureDeliveryRequest(
    ScientificFigureWorkflow Workflow,
    ScientificSvgArtifact Svg,
    ScientificFigureExportBundle Exports,
    ScientificContractReviewReport ContractReview,
    ScientificMachineReviewDecision MachineReview,
    IReadOnlyList<ScientificRepairRecord> Repairs,
    IReadOnlyList<ScientificDeliveryProviderMetadata> Providers,
    ScientificGateTwoDecision HumanDecision);

public sealed record ScientificFigureDeliveryResult(
    bool Approved,
    ScientificGateTwoApproval? GateTwoApproval,
    ScientificFigureDeliveryPackage? Package,
    byte[]? PackageBytes,
    ScientificRepairPlan? RejectionRepairPlan);

public sealed class ScientificFigureDeliveryService
{
    private readonly IScientificFigurePackageWriter _packageWriter;
    private readonly ScientificRepairApplicationService _repairService;

    public ScientificFigureDeliveryService(
        IScientificFigurePackageWriter packageWriter,
        ScientificRepairApplicationService? repairService = null)
    {
        _packageWriter = packageWriter
            ?? throw new ArgumentNullException(nameof(packageWriter));
        _repairService = repairService ?? new ScientificRepairApplicationService();
    }

    public ScientificFigureDeliveryResult DecideGateTwo(
        ScientificFigureDeliveryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.HumanDecision);
        if (!request.HumanDecision.Approved)
        {
            var action = _repairService.RouteFinding(
                "gate-two-human-rejection",
                request.Workflow.Specification.SpecificationId.ToString("D"),
                RequireText(request.HumanDecision.Notes, nameof(request.HumanDecision.Notes)),
                ScientificRepairLayer.FigureSpecification);
            return new ScientificFigureDeliveryResult(
                Approved: false,
                GateTwoApproval: null,
                Package: null,
                PackageBytes: null,
                ScientificRepairPlan.Create([action]));
        }

        ValidateGateTwoReadiness(request);
        var specification = request.Workflow.Specification;
        var gateTwo = new ScientificGateTwoApproval(
            specification.SpecificationId,
            specification.Version,
            RequireText(request.HumanDecision.Reviewer, nameof(request.HumanDecision.Reviewer)),
            RequireText(request.HumanDecision.Notes, nameof(request.HumanDecision.Notes)),
            request.HumanDecision.ReviewedAt);
        var package = new ScientificFigureDeliveryPackage(
            specification.SpecificationId,
            specification.Version,
            request.Svg,
            request.Exports,
            specification,
            BuildClaimEvidenceMap(specification),
            request.ContractReview,
            request.MachineReview,
            Array.AsReadOnly(request.Repairs.ToArray()),
            Array.AsReadOnly(request.Providers.ToArray()),
            request.Workflow.Gate1Approval!,
            gateTwo);
        var bytes = _packageWriter.Write(package);
        if (bytes is null || bytes.Length == 0)
        {
            throw new InvalidOperationException(
                "Scientific delivery package writer returned no bytes.");
        }

        return new ScientificFigureDeliveryResult(
            Approved: true,
            gateTwo,
            package,
            bytes.ToArray(),
            RejectionRepairPlan: null);
    }

    public static void ValidateGateTwoReadiness(
        ScientificFigureDeliveryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.HumanDecision);
        ArgumentNullException.ThrowIfNull(request.Workflow);
        ArgumentNullException.ThrowIfNull(request.Svg);
        ArgumentNullException.ThrowIfNull(request.Exports);
        ArgumentNullException.ThrowIfNull(request.ContractReview);
        ArgumentNullException.ThrowIfNull(request.MachineReview);
        ArgumentNullException.ThrowIfNull(request.Repairs);
        ArgumentNullException.ThrowIfNull(request.Providers);
        var specification = request.Workflow.Specification;
        var gateOne = request.Workflow.Gate1Approval;
        if (gateOne is null
            || gateOne.SpecificationId != specification.SpecificationId
            || gateOne.ApprovedSpecVersion != specification.Version)
        {
            throw new InvalidOperationException(
                "Gate 2 requires a current Gate 1 human approval.");
        }

        if (!request.ContractReview.Passed)
        {
            throw new InvalidOperationException(
                "Gate 2 is blocked by deterministic contract failures.");
        }

        if (!request.MachineReview.CanProceedToGate2)
        {
            throw new InvalidOperationException(
                "Gate 2 is blocked by semantic or visual review findings.");
        }

        if (specification.Issues.Any(item =>
                item.Status == ScientificFigureIssueStatus.Unresolved))
        {
            throw new InvalidOperationException(
                "Gate 2 is blocked by unresolved specification issues.");
        }

        if (request.Repairs.Any(item =>
                item is null
                || item.Status == ScientificRepairRecordStatus.Unresolved
                || string.IsNullOrWhiteSpace(item.Resolution)))
        {
            throw new InvalidOperationException(
                "Gate 2 is blocked by unresolved repair records.");
        }

        if (request.Providers.Any(item => !IsValidProviderMetadata(item)))
        {
            throw new InvalidOperationException(
                "Gate 2 provider metadata contains an invalid record.");
        }

        var providerLayers = request.Providers
            .Select(item => item.Layer)
            .ToHashSet();
        if (!providerLayers.SetEquals(
                [ScientificReviewLayer.Semantic, ScientificReviewLayer.Visual])
            || request.Providers.GroupBy(item => item.Layer).Any(group => group.Count() != 1))
        {
            throw new InvalidOperationException(
                "Gate 2 requires valid semantic and visual provider metadata.");
        }

        if (request.Svg.SpecificationId != specification.SpecificationId
            || request.Svg.SpecificationVersion != specification.Version
            || !HashEquals(
                Hash(Encoding.UTF8.GetBytes(request.Svg.Svg)),
                request.Svg.Sha256)
            || !string.Equals(
                request.Exports.SourceSvgSha256,
                request.Svg.Sha256,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Gate 2 artifacts do not match the approved specification version.");
        }

        var formats = request.Exports.Artifacts
            .GroupBy(item => item.Format, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        if (formats.Count != 2
            || !formats.TryGetValue("png", out var pngCount)
            || pngCount != 1
            || !formats.TryGetValue("pdf", out var pdfCount)
            || pdfCount != 1)
        {
            throw new InvalidOperationException(
                "Gate 2 delivery requires exactly one PNG and one PDF export.");
        }

        if (request.Exports.Artifacts.Any(item =>
                !HashEquals(Hash(item.Bytes), item.Sha256)
                || !HashEquals(item.SourceSvgSha256, request.Svg.Sha256)
                || !HashEquals(item.SemanticSha256, request.Exports.SemanticSha256)))
        {
            throw new InvalidOperationException(
                "Gate 2 export artifact hash bindings are invalid.");
        }

        if (request.HumanDecision.ReviewedAt < gateOne.ReviewedAt)
        {
            throw new InvalidOperationException(
                "Gate 2 human approval cannot precede Gate 1 approval.");
        }
    }

    private static bool IsValidProviderMetadata(ScientificDeliveryProviderMetadata item)
    {
        return item is not null
            && Enum.IsDefined(item.Layer)
            && !string.IsNullOrWhiteSpace(item.ProviderId)
            && !string.IsNullOrWhiteSpace(item.ModelId)
            && !string.IsNullOrWhiteSpace(item.TraceId);
    }

    private static IReadOnlyList<ScientificClaimEvidenceItemMap> BuildClaimEvidenceMap(
        ScientificFigureSpec specification)
    {
        return specification.Elements
            .Where(item => item.Requirement != FigureContentRequirement.Forbidden)
            .Select(item => Map(item.ElementId, item.Kind.ToString(), item.Provenance))
            .Concat(specification.Relations
                .Where(item => item.Requirement != FigureContentRequirement.Forbidden)
                .Select(item => Map(item.RelationId, "Relation", item.Provenance)))
            .ToArray();
    }

    private static ScientificClaimEvidenceItemMap Map(
        string itemId,
        string itemKind,
        ScientificFigureProvenance? provenance)
    {
        return new ScientificClaimEvidenceItemMap(
            itemId,
            itemKind,
            provenance?.ClaimId,
            provenance?.Evidence?.SourceBlockId,
            provenance?.Evidence?.QuotedText,
            provenance?.ConventionId);
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    private static string Hash(byte[] bytes)
    {
        return $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";
    }

    private static bool HashEquals(string first, string second)
    {
        return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
    }
}
