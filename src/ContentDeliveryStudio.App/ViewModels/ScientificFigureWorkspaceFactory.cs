using ContentDeliveryStudio.Application.Localization;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.App.ViewModels;

public sealed class ScientificFigureWorkspaceFactory
{
    private static readonly DateTimeOffset FixtureTime =
        DateTimeOffset.Parse("2026-07-27T00:00:00Z");

    private readonly LocalizationService _localizationService;
    private readonly Action<byte[]>? _packageExportRequested;

    public ScientificFigureWorkspaceFactory(
        LocalizationService localizationService,
        Action<byte[]>? packageExportRequested = null)
    {
        _localizationService = localizationService
            ?? throw new ArgumentNullException(nameof(localizationService));
        _packageExportRequested = packageExportRequested;
    }

    public WorkbenchTabViewModel Create()
    {
        var extraction = CreateExtraction();
        var understanding = CreateUnderstanding(extraction);
        var specification = CreateSpecification(understanding);
        var workflow = ScientificFigureWorkflow.Create(specification)
            .ApproveGate1(
                "fake-human-gate-one",
                "Fake-first scientific content and evidence approved.",
                FixtureTime.AddMinutes(1))
            .RecordDownstreamApproval(
                ScientificDownstreamApprovalStage.RenderPlan,
                "deterministic-svg-compiler",
                FixtureTime.AddMinutes(2))
            .RecordDownstreamApproval(
                ScientificDownstreamApprovalStage.ScientificReview,
                "fake-independent-review",
                FixtureTime.AddMinutes(3));
        var plan = new ScientificFigureSpecCompiler().Compile(workflow);
        var svg = new DeterministicSvgRenderer().Render(plan);
        var exports = new ScientificFigureExporter().Export(
            new ScientificFigureExportRequest(svg, svg.Sha256, Width: 1200, Height: 800));
        var contractReview = ScientificContractReviewReport.Create(1, []);
        var machineReview = new ScientificMachineReviewDecision([]);
        var repairPlan = ScientificRepairPlan.Create([]);
        var providers = new[]
        {
            new ScientificDeliveryProviderMetadata(
                ScientificReviewLayer.Semantic,
                "fake-scientific-semantic",
                "deterministic-fake-v1",
                "fake-semantic-trace"),
            new ScientificDeliveryProviderMetadata(
                ScientificReviewLayer.Visual,
                "fake-scientific-visual",
                "deterministic-fake-v1",
                "fake-visual-trace"),
        };
        var deliveryService = new ScientificFigureDeliveryService(
            new ScientificFigurePackageWriter());
        var deliveryRequest = new ScientificFigureDeliveryRequest(
            workflow,
            svg,
            exports,
            contractReview,
            machineReview,
            [],
            providers,
            new ScientificGateTwoDecision(
                Approved: true,
                "fake-readiness-probe",
                "Fake-first readiness probe.",
                FixtureTime.AddMinutes(4)));

        return new WorkbenchTabViewModel(
            WorkbenchTabKind.ScientificFigure,
            Text(LocalizationKey.ScientificFigures),
            Text(LocalizationKey.ScientificFiguresEmptyState),
            new ScientificFigureWorkflowCoordinator(_localizationService).Build(workflow),
            new ScientificSourceUnderstandingViewModel(
                extraction,
                understanding,
                () => FixtureTime,
                _localizationService),
            new ScientificFigureSpecViewModel(
                workflow,
                [],
                () => FixtureTime,
                _localizationService),
            new ScientificRenderReviewViewModel(
                understanding,
                specification,
                plan,
                svg,
                contractReview,
                machineReview,
                repairPlan,
                [],
                automaticRepairRequested: null,
                clock: () => FixtureTime,
                localizationService: _localizationService),
            new ScientificDeliveryViewModel(
                deliveryService,
                deliveryRequest,
                _packageExportRequested,
                () => FixtureTime.AddMinutes(4),
                _localizationService));
    }

    private static ScientificDocumentExtraction CreateExtraction()
    {
        var block = ScientificSourceBlock.Create(
            "block-dynamics",
            ScientificSourceBlockKind.Paragraph,
            ScientificSourceLocation.Create(
                pageNumber: 4,
                section: "2.1 Dynamics",
                ScientificBoundingRegion.Create(72, 144, 320, 48),
                ScientificCharacterRange.Create(20, 112)),
            "Net force causes acceleration for constant mass.",
            isRequired: true,
            ScientificRecoveryStatus.NotRequired);
        return ScientificDocumentExtraction.Create(
            Guid.Parse("3da91bb1-73e2-42b3-9919-c9386af0a011"),
            "sha256:a3f75487411ef13d0270c65fc801ceff2b28e6b339afed9b407fe477f7e8453e",
            ScientificExtractorIdentity.Create("fake-fixture-extractor", "1.0"),
            ScientificExtractionQuality.Create(
                isScanned: false,
                ocrApplied: false,
                ScientificReadingOrderStatus.Reliable,
                ScientificRequiredContentStatus.Complete),
            [block],
            []);
    }

    private static ScientificDocumentUnderstanding CreateUnderstanding(
        ScientificDocumentExtraction extraction)
    {
        var block = extraction.Blocks.Single();
        var evidence = ClaimEvidenceLink.Create(
            extraction,
            block,
            block.OriginalText!,
            ClaimEvidenceRole.Support,
            confidence: 0.98,
            EvidenceValidationState.Validated);
        var claim = ScientificClaim.Create(
            "claim-newton-second-law",
            ScientificClaimCategory.CausalRelation,
            "Net force causes acceleration for constant mass.",
            block.OriginalText!,
            confidence: 0.96,
            ScientificClaimStatus.Accepted,
            [evidence]);
        var coverage = ScientificCoverageRequirement.Create(
            "coverage-central-mechanism",
            "The central causal mechanism is represented.",
            isRequired: true,
            ScientificCoverageStatus.Complete,
            [claim.ClaimId]);
        return ScientificDocumentUnderstanding.Create(
            Guid.Parse("6a8d768c-a6f1-4f73-99c7-21d52f3e72aa"),
            extraction,
            "Explain how net force changes motion.",
            version: 1,
            [],
            [claim],
            [],
            [coverage]);
    }

    private static ScientificFigureSpec CreateSpecification(
        ScientificDocumentUnderstanding understanding)
    {
        var claim = understanding.Claims.Single();
        var evidence = claim.SupportingEvidence.Single();
        var provenance = ScientificFigureProvenance.FromEvidence(claim, evidence);
        var force = FigureElementSpec.Create(
            "element-force",
            "Net force acting on the object.",
            FigureElementKind.Entity,
            "Net force",
            "deterministic-node",
            FigureContentRequirement.Required,
            isCritical: true,
            provenance);
        var acceleration = FigureElementSpec.Create(
            "element-acceleration",
            "Acceleration of the object.",
            FigureElementKind.Entity,
            "Acceleration",
            "deterministic-node",
            FigureContentRequirement.Required,
            isCritical: true,
            provenance);
        var relation = FigureRelationSpec.Create(
            "relation-force-acceleration",
            force.ElementId,
            acceleration.ElementId,
            FigureRelationKind.Causes,
            FigureRelationDirection.Directed,
            "causes",
            "Net force causes acceleration for constant mass.",
            "A single directed arrow points from force to acceleration.",
            FigureContentRequirement.Required,
            isCritical: true,
            provenance);
        return ScientificFigureSpec.Create(
            Guid.Parse("889da024-c98c-4daa-b2ba-1c19e8aa1fa6"),
            understanding,
            "Explain a bounded scientific mechanism.",
            "Net force causes acceleration.",
            "Secondary physics learners",
            isSchematic: true,
            ScientificFigureRiskLevel.Medium,
            [force, acceleration],
            [relation],
            []);
    }

    private string Text(LocalizationKey key)
    {
        return _localizationService.GetText(key);
    }
}
