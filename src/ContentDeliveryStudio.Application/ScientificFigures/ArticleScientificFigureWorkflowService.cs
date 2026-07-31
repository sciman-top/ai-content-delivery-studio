using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Application.ScientificFigures;

/// <summary>
/// Moves a human-approved article candidate into the same persisted scientific-figure
/// authority and review path used by the rest of the product.  It intentionally accepts
/// only the bounded mechanism candidate; broader claims in the source remain outside this
/// service until a reviewer approves them in a separate Gate 1 decision.
/// </summary>
public sealed class ArticleScientificFigureWorkflowService
{
    private readonly IScientificFigureWorkflowRepository _repository;
    private readonly IScientificFigureRenderer _renderer;
    private readonly IScientificFigureExporter _exporter;
    private readonly IScientificReviewImageCropper _cropper;
    private readonly IScientificSemanticReviewProvider _semanticReviewProvider;
    private readonly IScientificVisualReviewProvider _visualReviewProvider;

    public ArticleScientificFigureWorkflowService(
        IScientificFigureWorkflowRepository repository,
        IScientificFigureRenderer renderer,
        IScientificFigureExporter exporter,
        IScientificReviewImageCropper cropper,
        IScientificSemanticReviewProvider semanticReviewProvider,
        IScientificVisualReviewProvider visualReviewProvider)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
        _cropper = cropper ?? throw new ArgumentNullException(nameof(cropper));
        _semanticReviewProvider = semanticReviewProvider
            ?? throw new ArgumentNullException(nameof(semanticReviewProvider));
        _visualReviewProvider = visualReviewProvider
            ?? throw new ArgumentNullException(nameof(visualReviewProvider));
    }

    public async Task<ScientificFigureWorkflowAggregate> CreateApprovedMechanismAsync(
        Guid projectId,
        ScientificDocumentExtraction extraction,
        ArticleScientificFigureCandidate candidate,
        ScientificGateOneDecision decision,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(extraction);
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(decision);
        if (!decision.Approved)
        {
            throw new InvalidOperationException("Article candidate Gate 1 requires explicit approval.");
        }

        if (extraction.Status != ScientificExtractionStatus.Ready
            || candidate.Kind != ArticleScientificFigureCandidateKind.Mechanism
            || candidate.GateOneStatus != ArticleScientificFigureGateStatus.PendingHumanApproval)
        {
            throw new InvalidOperationException(
                "Only a ready, pending mechanism candidate can enter the approved article workflow.");
        }

        var evidenceBlockId = candidate.Evidence
            .Select(item => item.SourceBlockId)
            .FirstOrDefault(id => extraction.Blocks.Any(block => block.BlockId == id))
            ?? throw new InvalidOperationException("The article candidate has no extractable source evidence.");
        var sourceBlock = extraction.Blocks.Single(block => block.BlockId == evidenceBlockId);
        var quotedText = sourceBlock.OriginalText
            ?? throw new InvalidOperationException("Article candidate evidence has no source text.");
        var evidence = ClaimEvidenceLink.Create(
            extraction,
            sourceBlock,
            quotedText,
            ClaimEvidenceRole.Support,
            confidence: 0.9,
            EvidenceValidationState.Validated);
        var claim = ScientificClaim.Create(
            "claim-secondary-lens-mechanism",
            ScientificClaimCategory.Mechanism,
            "Within the approved schematic scope, intermediate image S is treated as the object for an eye modeled as a second convex lens.",
            quotedText,
            confidence: 0.9,
            ScientificClaimStatus.Accepted,
            [evidence]);
        var understanding = ScientificDocumentUnderstanding.Create(
            Guid.NewGuid(),
            extraction,
            candidate.Objective,
            version: 1,
            terminology: [],
            claims: [claim],
            conflicts: [],
            coverage:
            [
                ScientificCoverageRequirement.Create(
                    "coverage-approved-optical-schematic",
                    "The approved secondary-lens mechanism is represented without unapproved quantitative or physiological claims.",
                    isRequired: true,
                    ScientificCoverageStatus.Complete,
                    [claim.ClaimId]),
            ]);
        var evidenceProvenance = ScientificFigureProvenance.FromEvidence(claim, evidence);
        var scopeConvention = ScientificFigureProvenance.FromConvention(
            "scientific_convention:article-gate-one-optics-schematic",
            "Gate 1 approves a non-proportional schematic only: primary lens forms S; S is the object for the eye lens; rays proceed toward the retina. No focal length, clarity, perceived orientation, or medical claim is represented.");
        var primaryLens = Element(
            "element-primary-lens",
            "Primary convex lens in the approved schematic.",
            "主凸透镜 L1",
            scopeConvention);
        var intermediateImage = Element(
            "element-intermediate-image",
            "Intermediate image S in the approved schematic.",
            "中间像 S",
            evidenceProvenance);
        var eyeLens = Element(
            "element-eye-lens",
            "Eye lens represented as the second convex lens.",
            "眼睛晶状体 L2",
            evidenceProvenance);
        var retina = Element(
            "element-retina",
            "Retina direction surface in the approved schematic.",
            "视网膜",
            scopeConvention);
        var specification = ScientificFigureSpec.Create(
            Guid.NewGuid(),
            understanding,
            candidate.Objective,
            candidate.CentralMessage,
            candidate.Audience,
            isSchematic: true,
            candidate.RiskLevel,
            [primaryLens, intermediateImage, eyeLens, retina],
            [
                Relation(
                    "relation-primary-lens-forms-s",
                    primaryLens,
                    intermediateImage,
                    "forms S",
                    scopeConvention),
                Relation(
                    "relation-s-is-object-for-eye-lens",
                    intermediateImage,
                    eyeLens,
                    "S is object for L2",
                    evidenceProvenance),
                Relation(
                    "relation-eye-lens-toward-retina",
                    eyeLens,
                    retina,
                    "toward retina",
                    scopeConvention),
            ],
            issues: []);
        var workflow = ScientificFigureWorkflow.Create(specification).ApproveGate1(
            decision.Reviewer,
            decision.Notes,
            decision.ReviewedAt);
        var aggregate = ScientificFigureWorkflowAggregate.Create(
            Guid.NewGuid(),
            projectId,
            extraction,
            understanding,
            workflow,
            decision.ReviewedAt,
            decision.ReviewedAt);
        await _repository.SaveAsync(aggregate, cancellationToken);
        return aggregate;
    }

    public async Task<ArticleScientificFigureMachineReviewResult> RunMachineReviewAsync(
        ScientificFigureWorkflowAggregate aggregate,
        DateTimeOffset reviewedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        if (aggregate.Workflow.Gate1Approval is null)
        {
            throw new InvalidOperationException("Machine review requires a current Gate 1 approval.");
        }

        var plan = new ScientificFigureSpecCompiler().Compile(aggregate.Workflow);
        var svg = _renderer.Render(plan);
        var exports = _exporter.Export(new ScientificFigureExportRequest(
            svg,
            svg.Sha256,
            Width: 1200,
            Height: 800));
        var contract = new ScientificContractReviewer().Review(
            new ScientificContractReviewRequest(aggregate.Workflow.Specification, plan, svg, exports));
        var prep = new ScientificReviewPrepBuilder(_cropper).Build(
            aggregate.Understanding,
            aggregate.Workflow.Specification,
            plan,
            svg,
            exports);
        var machine = await new ScientificReviewExecutionService(
            _semanticReviewProvider,
            _visualReviewProvider).ReviewAsync(
            prep.SemanticRequest,
            prep.VisualRequest,
            cancellationToken);
        var reviewedAggregate = aggregate;
        if (contract.Passed && machine.CanProceedToGate2)
        {
            var reviewedWorkflow = aggregate.Workflow
                .RecordDownstreamApproval(
                    ScientificDownstreamApprovalStage.RenderPlan,
                    "system-deterministic-contract-review",
                    reviewedAt)
                .RecordDownstreamApproval(
                    ScientificDownstreamApprovalStage.ScientificReview,
                    "system-semantic-and-visual-review",
                    reviewedAt);
            reviewedAggregate = ScientificFigureWorkflowAggregate.Create(
                aggregate.Id,
                aggregate.ProjectId,
                aggregate.Extraction,
                aggregate.Understanding,
                reviewedWorkflow,
                aggregate.CreatedAt,
                reviewedAt);
            await _repository.SaveAsync(reviewedAggregate, cancellationToken);
        }

        return new ArticleScientificFigureMachineReviewResult(
            reviewedAggregate,
            plan,
            svg,
            exports,
            contract,
            prep,
            machine);
    }

    private static FigureElementSpec Element(
        string elementId,
        string meaning,
        string label,
        ScientificFigureProvenance provenance) =>
        FigureElementSpec.Create(
            elementId,
            meaning,
            FigureElementKind.Entity,
            label,
            "deterministic-node",
            FigureContentRequirement.Required,
            isCritical: true,
            provenance);

    private static FigureRelationSpec Relation(
        string relationId,
        FigureElementSpec source,
        FigureElementSpec target,
        string label,
        ScientificFigureProvenance provenance) =>
        FigureRelationSpec.Create(
            relationId,
            source.ElementId,
            target.ElementId,
            FigureRelationKind.TransformsInto,
            FigureRelationDirection.Directed,
            label,
            label,
            "single directed arrow",
            FigureContentRequirement.Required,
            isCritical: true,
            provenance);
}

public sealed record ArticleScientificFigureMachineReviewResult(
    ScientificFigureWorkflowAggregate Aggregate,
    SvgRenderPlan RenderPlan,
    ScientificSvgArtifact Svg,
    ScientificFigureExportBundle Exports,
    ScientificContractReviewReport ContractReview,
    ScientificReviewPrepBundle ReviewPreparation,
    ScientificMachineReviewDecision MachineReview);
