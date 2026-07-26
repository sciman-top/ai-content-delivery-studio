using System.Text.Json;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Infrastructure.Persistence;

internal static class ScientificFigureWorkflowJsonCodec
{
    public const string CurrentSchemaVersion = "scientific-figure-workflow.v1";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Serialize(ScientificFigureWorkflowAggregate aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        return JsonSerializer.Serialize(ToPayload(aggregate), JsonOptions);
    }

    public static ScientificFigureWorkflowAggregate Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Scientific figure workflow payload is empty.");
        }

        Payload payload;
        try
        {
            payload = JsonSerializer.Deserialize<Payload>(json, JsonOptions)
                ?? throw new InvalidOperationException("Scientific figure workflow payload is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "Scientific figure workflow payload is invalid JSON.",
                exception);
        }

        if (!string.Equals(payload.SchemaVersion, CurrentSchemaVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Unsupported scientific figure workflow schema: {payload.SchemaVersion}");
        }

        try
        {
            return Restore(payload);
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or InvalidOperationException
            or KeyNotFoundException)
        {
            throw new InvalidOperationException(
                $"Scientific figure workflow payload failed domain validation: {payload.Id}",
                exception);
        }
    }

    private static Payload ToPayload(ScientificFigureWorkflowAggregate aggregate)
    {
        var extraction = aggregate.Extraction;
        var understanding = aggregate.Understanding;
        var specification = aggregate.Workflow.Specification;
        return new Payload(
            CurrentSchemaVersion,
            aggregate.Id,
            aggregate.ProjectId,
            aggregate.CreatedAt,
            aggregate.UpdatedAt,
            new ExtractionDto(
                extraction.SourceAssetId,
                extraction.SourceSha256,
                extraction.Extractor.ProviderId,
                extraction.Extractor.Version,
                extraction.Quality.IsScanned,
                extraction.Quality.OcrApplied,
                extraction.Quality.ReadingOrder,
                extraction.Quality.RequiredContent,
                extraction.Blocks.Select(ToBlockDto).ToArray(),
                extraction.Diagnostics
                    .Select(item => new DiagnosticDto(item.Code, item.Severity, item.Message))
                    .ToArray()),
            new UnderstandingDto(
                understanding.UnderstandingId,
                understanding.Objective,
                understanding.Version,
                understanding.Terminology
                    .Select(item => new TermDto(
                        item.TermId,
                        item.CanonicalTerm,
                        item.Definition,
                        item.Aliases.ToArray()))
                    .ToArray(),
                understanding.Claims
                    .Select(item => new ClaimDto(
                        item.ClaimId,
                        item.Category,
                        item.NormalizedStatement,
                        item.SourceWording,
                        item.Confidence,
                        item.Status,
                        item.EvidenceLinks.Select(ToEvidenceDto).ToArray()))
                    .ToArray(),
                understanding.Conflicts
                    .Select(item => new ConflictDto(
                        item.ConflictId,
                        item.FirstClaimId,
                        item.SecondClaimId,
                        item.Description,
                        item.Status,
                        item.Resolution))
                    .ToArray(),
                understanding.Coverage
                    .Select(item => new CoverageDto(
                        item.RequirementId,
                        item.Description,
                        item.IsRequired,
                        item.Status,
                        item.ClaimIds.ToArray()))
                    .ToArray()),
            new SpecificationDto(
                specification.SpecificationId,
                specification.Version,
                specification.Purpose,
                specification.CentralMessage,
                specification.Audience,
                specification.IsSchematic,
                specification.RiskLevel,
                specification.Elements.Select(ToElementDto).ToArray(),
                specification.Relations.Select(ToRelationDto).ToArray(),
                specification.Issues
                    .Select(item => new IssueDto(
                        item.IssueId,
                        item.Kind,
                        item.Description,
                        item.Status,
                        item.Resolution))
                    .ToArray()),
            new WorkflowDto(
                aggregate.Workflow.State,
                aggregate.Workflow.Gate1Approval is null
                    ? null
                    : new Gate1Dto(
                        aggregate.Workflow.Gate1Approval.Reviewer,
                        aggregate.Workflow.Gate1Approval.Notes,
                        aggregate.Workflow.Gate1Approval.ReviewedAt),
                aggregate.Workflow.DownstreamApprovals
                    .Select(item => new DownstreamDto(
                        item.Stage,
                        item.Reviewer,
                        item.ReviewedAt))
                    .ToArray()));
    }

    private static ScientificFigureWorkflowAggregate Restore(Payload payload)
    {
        var extraction = RestoreExtraction(payload.Extraction);
        var understanding = RestoreUnderstanding(payload.Understanding, extraction);
        var specification = RestoreSpecification(payload.Specification, understanding);
        var workflow = ScientificFigureWorkflow.Create(specification);
        if (payload.Workflow.Gate1Approval is not null)
        {
            workflow = workflow.ApproveGate1(
                payload.Workflow.Gate1Approval.Reviewer,
                payload.Workflow.Gate1Approval.Notes,
                payload.Workflow.Gate1Approval.ReviewedAt);
        }

        foreach (var approval in payload.Workflow.DownstreamApprovals)
        {
            workflow = workflow.RecordDownstreamApproval(
                approval.Stage,
                approval.Reviewer,
                approval.ReviewedAt);
        }

        if (workflow.State != payload.Workflow.State)
        {
            throw new InvalidOperationException(
                $"Workflow state '{payload.Workflow.State}' is inconsistent with its approvals.");
        }

        return ScientificFigureWorkflowAggregate.Create(
            payload.Id,
            payload.ProjectId,
            extraction,
            understanding,
            workflow,
            payload.CreatedAt,
            payload.UpdatedAt);
    }

    private static ScientificDocumentExtraction RestoreExtraction(ExtractionDto dto)
    {
        var blocks = dto.Blocks.Select(item =>
            ScientificSourceBlock.Create(
                item.BlockId,
                item.Kind,
                RestoreLocation(item.Location),
                item.OriginalText,
                item.IsRequired,
                item.RecoveryStatus)).ToArray();
        var diagnostics = dto.Diagnostics.Select(item =>
            ScientificExtractionDiagnostic.Create(
                item.Code,
                item.Severity,
                item.Message)).ToArray();
        return ScientificDocumentExtraction.Create(
            dto.SourceAssetId,
            dto.SourceSha256,
            ScientificExtractorIdentity.Create(dto.ExtractorProviderId, dto.ExtractorVersion),
            ScientificExtractionQuality.Create(
                dto.IsScanned,
                dto.OcrApplied,
                dto.ReadingOrder,
                dto.RequiredContent),
            blocks,
            diagnostics);
    }

    private static ScientificDocumentUnderstanding RestoreUnderstanding(
        UnderstandingDto dto,
        ScientificDocumentExtraction extraction)
    {
        var blockById = extraction.Blocks.ToDictionary(item => item.BlockId, StringComparer.Ordinal);
        var terms = dto.Terminology.Select(item =>
            ScientificTermDefinition.Create(
                item.TermId,
                item.CanonicalTerm,
                item.Definition,
                item.Aliases)).ToArray();
        var claims = dto.Claims.Select(item =>
            ScientificClaim.Create(
                item.ClaimId,
                item.Category,
                item.NormalizedStatement,
                item.SourceWording,
                item.Confidence,
                item.Status,
                item.EvidenceLinks.Select(link =>
                    RestoreEvidence(link, extraction, blockById)).ToArray())).ToArray();
        var conflicts = dto.Conflicts.Select(item =>
            ScientificClaimConflict.Create(
                item.ConflictId,
                item.FirstClaimId,
                item.SecondClaimId,
                item.Description,
                item.Status,
                item.Resolution)).ToArray();
        var coverage = dto.Coverage.Select(item =>
            ScientificCoverageRequirement.Create(
                item.RequirementId,
                item.Description,
                item.IsRequired,
                item.Status,
                item.ClaimIds)).ToArray();
        return ScientificDocumentUnderstanding.Create(
            dto.UnderstandingId,
            extraction,
            dto.Objective,
            dto.Version,
            terms,
            claims,
            conflicts,
            coverage);
    }

    private static ScientificFigureSpec RestoreSpecification(
        SpecificationDto dto,
        ScientificDocumentUnderstanding understanding)
    {
        var claims = understanding.Claims.ToDictionary(item => item.ClaimId, StringComparer.Ordinal);
        ScientificFigureProvenance? RestoreProvenance(ProvenanceDto? item)
        {
            if (item is null)
            {
                return null;
            }

            if (item.Kind == ScientificProvenanceKind.ScientificConvention)
            {
                return ScientificFigureProvenance.FromConvention(
                    item.ConventionId!,
                    item.ConventionStatement!);
            }

            var claim = claims[item.ClaimId!];
            var evidence = claim.EvidenceLinks.Single(link =>
                string.Equals(link.SourceBlockId, item.Evidence!.SourceBlockId, StringComparison.Ordinal)
                && string.Equals(link.QuotedText, item.Evidence.QuotedText, StringComparison.Ordinal)
                && link.Role == item.Evidence.Role
                && link.ValidationState == item.Evidence.ValidationState);
            return ScientificFigureProvenance.FromEvidence(claim, evidence);
        }

        var elements = dto.Elements.Select(item =>
            FigureElementSpec.Create(
                item.ElementId,
                item.ScientificMeaning,
                item.Kind,
                item.LabelOrFormula,
                item.RenderStrategy,
                item.Requirement,
                item.IsCritical,
                RestoreProvenance(item.Provenance))).ToArray();
        var relations = dto.Relations.Select(item =>
            FigureRelationSpec.Create(
                item.RelationId,
                item.SourceElementId,
                item.TargetElementId,
                item.Kind,
                item.Direction,
                item.Label,
                item.ScientificMeaning,
                item.RepresentationConstraint,
                item.Requirement,
                item.IsCritical,
                RestoreProvenance(item.Provenance)!)).ToArray();
        var issues = dto.Issues.Select(item =>
            ScientificFigureIssue.Create(
                item.IssueId,
                item.Kind,
                item.Description,
                item.Status,
                item.Resolution)).ToArray();
        var specification = ScientificFigureSpec.Create(
            dto.SpecificationId,
            understanding,
            dto.Purpose,
            dto.CentralMessage,
            dto.Audience,
            dto.IsSchematic,
            dto.RiskLevel,
            elements,
            relations,
            issues);
        for (var version = 1; version < dto.Version; version++)
        {
            specification = specification.ReviseScientificContent(
                understanding,
                dto.CentralMessage,
                elements,
                relations,
                issues);
        }

        return specification;
    }

    private static ScientificSourceBlockDto ToBlockDto(ScientificSourceBlock block)
    {
        return new ScientificSourceBlockDto(
            block.BlockId,
            block.Kind,
            ToLocationDto(block.Location),
            block.OriginalText,
            block.IsRequired,
            block.RecoveryStatus);
    }

    private static LocationDto ToLocationDto(ScientificSourceLocation location)
    {
        return new LocationDto(
            location.PageNumber,
            location.Section,
            location.BoundingRegion is null
                ? null
                : new BoundsDto(
                    location.BoundingRegion.X,
                    location.BoundingRegion.Y,
                    location.BoundingRegion.Width,
                    location.BoundingRegion.Height),
            location.CharacterRange is null
                ? null
                : new RangeDto(
                    location.CharacterRange.StartOffset,
                    location.CharacterRange.EndOffset));
    }

    private static ScientificSourceLocation RestoreLocation(LocationDto location)
    {
        return ScientificSourceLocation.Create(
            location.PageNumber,
            location.Section,
            location.Bounds is null
                ? null
                : ScientificBoundingRegion.Create(
                    location.Bounds.X,
                    location.Bounds.Y,
                    location.Bounds.Width,
                    location.Bounds.Height),
            location.Range is null
                ? null
                : ScientificCharacterRange.Create(
                    location.Range.StartOffset,
                    location.Range.EndOffset));
    }

    private static EvidenceDto ToEvidenceDto(ClaimEvidenceLink evidence)
    {
        return new EvidenceDto(
            evidence.SourceBlockId,
            evidence.QuotedText,
            evidence.Role,
            evidence.Confidence,
            evidence.ValidationState);
    }

    private static ClaimEvidenceLink RestoreEvidence(
        EvidenceDto evidence,
        ScientificDocumentExtraction extraction,
        IReadOnlyDictionary<string, ScientificSourceBlock> blocks)
    {
        return ClaimEvidenceLink.Create(
            extraction,
            blocks[evidence.SourceBlockId],
            evidence.QuotedText,
            evidence.Role,
            evidence.Confidence,
            evidence.ValidationState);
    }

    private static FigureElementDto ToElementDto(FigureElementSpec element)
    {
        return new FigureElementDto(
            element.ElementId,
            element.ScientificMeaning,
            element.Kind,
            element.LabelOrFormula,
            element.RenderStrategy,
            element.Requirement,
            element.IsCritical,
            ToProvenanceDto(element.Provenance));
    }

    private static FigureRelationDto ToRelationDto(FigureRelationSpec relation)
    {
        return new FigureRelationDto(
            relation.RelationId,
            relation.SourceElementId,
            relation.TargetElementId,
            relation.Kind,
            relation.Direction,
            relation.Label,
            relation.ScientificMeaning,
            relation.RepresentationConstraint,
            relation.Requirement,
            relation.IsCritical,
            ToProvenanceDto(relation.Provenance)!);
    }

    private static ProvenanceDto? ToProvenanceDto(ScientificFigureProvenance? provenance)
    {
        return provenance is null
            ? null
            : new ProvenanceDto(
                provenance.Kind,
                provenance.ClaimId,
                provenance.Evidence is null ? null : ToEvidenceDto(provenance.Evidence),
                provenance.ConventionId,
                provenance.ConventionStatement);
    }

    private sealed record Payload(
        string SchemaVersion,
        Guid Id,
        Guid ProjectId,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        ExtractionDto Extraction,
        UnderstandingDto Understanding,
        SpecificationDto Specification,
        WorkflowDto Workflow);

    private sealed record ExtractionDto(
        Guid SourceAssetId,
        string SourceSha256,
        string ExtractorProviderId,
        string ExtractorVersion,
        bool IsScanned,
        bool OcrApplied,
        ScientificReadingOrderStatus ReadingOrder,
        ScientificRequiredContentStatus RequiredContent,
        ScientificSourceBlockDto[] Blocks,
        DiagnosticDto[] Diagnostics);

    private sealed record ScientificSourceBlockDto(
        string BlockId,
        ScientificSourceBlockKind Kind,
        LocationDto Location,
        string? OriginalText,
        bool IsRequired,
        ScientificRecoveryStatus RecoveryStatus);

    private sealed record LocationDto(
        int PageNumber,
        string Section,
        BoundsDto? Bounds,
        RangeDto? Range);

    private sealed record BoundsDto(double X, double Y, double Width, double Height);

    private sealed record RangeDto(int StartOffset, int EndOffset);

    private sealed record DiagnosticDto(
        string Code,
        ScientificDiagnosticSeverity Severity,
        string Message);

    private sealed record UnderstandingDto(
        Guid UnderstandingId,
        string Objective,
        int Version,
        TermDto[] Terminology,
        ClaimDto[] Claims,
        ConflictDto[] Conflicts,
        CoverageDto[] Coverage);

    private sealed record TermDto(
        string TermId,
        string CanonicalTerm,
        string Definition,
        string[] Aliases);

    private sealed record ClaimDto(
        string ClaimId,
        ScientificClaimCategory Category,
        string NormalizedStatement,
        string SourceWording,
        double Confidence,
        ScientificClaimStatus Status,
        EvidenceDto[] EvidenceLinks);

    private sealed record EvidenceDto(
        string SourceBlockId,
        string QuotedText,
        ClaimEvidenceRole Role,
        double Confidence,
        EvidenceValidationState ValidationState);

    private sealed record ConflictDto(
        string ConflictId,
        string FirstClaimId,
        string SecondClaimId,
        string Description,
        ScientificConflictStatus Status,
        string? Resolution);

    private sealed record CoverageDto(
        string RequirementId,
        string Description,
        bool IsRequired,
        ScientificCoverageStatus Status,
        string[] ClaimIds);

    private sealed record SpecificationDto(
        Guid SpecificationId,
        int Version,
        string Purpose,
        string CentralMessage,
        string Audience,
        bool IsSchematic,
        ScientificFigureRiskLevel RiskLevel,
        FigureElementDto[] Elements,
        FigureRelationDto[] Relations,
        IssueDto[] Issues);

    private sealed record FigureElementDto(
        string ElementId,
        string ScientificMeaning,
        FigureElementKind Kind,
        string? LabelOrFormula,
        string RenderStrategy,
        FigureContentRequirement Requirement,
        bool IsCritical,
        ProvenanceDto? Provenance);

    private sealed record FigureRelationDto(
        string RelationId,
        string SourceElementId,
        string TargetElementId,
        FigureRelationKind Kind,
        FigureRelationDirection Direction,
        string? Label,
        string ScientificMeaning,
        string RepresentationConstraint,
        FigureContentRequirement Requirement,
        bool IsCritical,
        ProvenanceDto Provenance);

    private sealed record ProvenanceDto(
        ScientificProvenanceKind Kind,
        string? ClaimId,
        EvidenceDto? Evidence,
        string? ConventionId,
        string? ConventionStatement);

    private sealed record IssueDto(
        string IssueId,
        ScientificFigureIssueKind Kind,
        string Description,
        ScientificFigureIssueStatus Status,
        string? Resolution);

    private sealed record WorkflowDto(
        ScientificFigureWorkflowState State,
        Gate1Dto? Gate1Approval,
        DownstreamDto[] DownstreamApprovals);

    private sealed record Gate1Dto(
        string Reviewer,
        string Notes,
        DateTimeOffset ReviewedAt);

    private sealed record DownstreamDto(
        ScientificDownstreamApprovalStage Stage,
        string Reviewer,
        DateTimeOffset ReviewedAt);
}
