using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificGateTwoWorkflowTests
{
    [Fact]
    public void DecideGateTwo_ApprovesOnlyCurrentFullyPassingWorkflow()
    {
        var fixture = ScientificDeliveryTestFixture.Create();

        var result = fixture.Service.DecideGateTwo(fixture.Request);

        Assert.True(result.Approved);
        Assert.NotNull(result.GateTwoApproval);
        Assert.NotNull(result.Package);
        Assert.NotEmpty(result.PackageBytes!);
        Assert.Null(result.RejectionRepairPlan);
        Assert.Equal(
            fixture.Workflow.Gate1Approval!.Reviewer,
            result.Package!.GateOneApproval.Reviewer);
        Assert.Equal("final-reviewer", result.Package.GateTwoApproval.Reviewer);
    }

    [Fact]
    public void DecideGateTwo_BlocksContractFailure()
    {
        var fixture = ScientificDeliveryTestFixture.Create();
        var failed = ScientificContractReviewReport.Create(
            1,
            [new ScientificContractFinding(
                "formula-drift",
                ScientificContractInvariant.ExactScientificContent,
                "element-formula",
                "Formula differs.",
                ScientificContractRepairLayer.FigureSpecification)]);

        var error = Assert.Throws<InvalidOperationException>(() =>
            fixture.Service.DecideGateTwo(
                fixture.Request with { ContractReview = failed }));

        Assert.Contains("contract", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DecideGateTwo_BlocksUncertaintyAndUnresolvedRepair()
    {
        var fixture = ScientificDeliveryTestFixture.Create();
        var uncertain = new ScientificMachineReviewDecision(
            [new ScientificReviewBlocker(
                ScientificReviewLayer.Semantic,
                "provider-uncertain",
                "Semantic",
                "Provider verdict was uncertain.")]);
        Assert.Throws<InvalidOperationException>(() =>
            fixture.Service.DecideGateTwo(
                fixture.Request with { MachineReview = uncertain }));

        var unresolved = new ScientificRepairRecord(
            new ScientificRepairApplicationService().RouteFinding(
                "layout-overlap",
                "element-force",
                "Overlap remains.",
                ScientificRepairLayer.LayoutStyle),
            ScientificRepairRecordStatus.Unresolved,
            string.Empty);
        Assert.Throws<InvalidOperationException>(() =>
            fixture.Service.DecideGateTwo(
                fixture.Request with { Repairs = [unresolved] }));
    }

    [Fact]
    public void DecideGateTwo_BlocksInvalidatedSpecificationVersion()
    {
        var fixture = ScientificDeliveryTestFixture.Create();
        var revised = new ScientificRepairApplicationService().ApplyScientificRevision(
            fixture.Workflow,
            fixture.ScientificFixture.Understanding,
            "Revised message.",
            fixture.Workflow.Specification.Elements,
            fixture.Workflow.Specification.Relations,
            fixture.Workflow.Specification.Issues);

        var error = Assert.Throws<InvalidOperationException>(() =>
            fixture.Service.DecideGateTwo(
                fixture.Request with { Workflow = revised }));

        Assert.Contains("Gate 1", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DecideGateTwo_BlocksTamperedExportAndInvalidProviderMetadata()
    {
        var fixture = ScientificDeliveryTestFixture.Create();
        var artifacts = fixture.Request.Exports.Artifacts.Select(item =>
            item.Format == "png"
                ? item with { Bytes = [.. item.Bytes, 0] }
                : item).ToArray();
        Assert.Throws<InvalidOperationException>(() =>
            fixture.Service.DecideGateTwo(
                fixture.Request with
                {
                    Exports = fixture.Request.Exports with { Artifacts = artifacts },
                }));

        var providers = fixture.Request.Providers.ToArray();
        providers[0] = providers[0] with { TraceId = string.Empty };
        Assert.Throws<InvalidOperationException>(() =>
            fixture.Service.DecideGateTwo(
                fixture.Request with { Providers = providers }));
    }

    [Fact]
    public void DecideGateTwo_RequiresExactlyOnePngAndOnePdf()
    {
        var fixture = ScientificDeliveryTestFixture.Create();
        var duplicatePng = fixture.Request.Exports.Artifacts
            .Append(fixture.Request.Exports.Artifacts.Single(item => item.Format == "png"))
            .ToArray();

        var error = Assert.Throws<InvalidOperationException>(() =>
            fixture.Service.DecideGateTwo(fixture.Request with
            {
                Exports = fixture.Request.Exports with { Artifacts = duplicatePng },
            }));

        Assert.Contains("exactly one PNG", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DecideGateTwo_HumanRejectionRoutesRepairAndNeverBuildsPackage()
    {
        var fixture = ScientificDeliveryTestFixture.Create();
        var rejected = fixture.Request with
        {
            HumanDecision = new ScientificGateTwoDecision(
                Approved: false,
                "final-reviewer",
                "Revise the scientific emphasis.",
                DateTimeOffset.UtcNow),
        };

        var result = fixture.Service.DecideGateTwo(rejected);

        Assert.False(result.Approved);
        Assert.Null(result.Package);
        Assert.Null(result.PackageBytes);
        var action = Assert.Single(result.RejectionRepairPlan!.Actions);
        Assert.Equal(ScientificRepairLayer.FigureSpecification, action.Layer);
        Assert.Equal(ScientificRepairExecutionMode.HumanRequired, action.ExecutionMode);
    }
}

internal sealed record ScientificDeliveryTestFixture(
    ScientificContractReviewFixture ScientificFixture,
    ScientificFigureWorkflow Workflow,
    ScientificFigureDeliveryService Service,
    ScientificFigureDeliveryRequest Request)
{
    public static ScientificDeliveryTestFixture Create()
    {
        var scientific = ScientificContractReviewFixture.Create();
        var workflow = ScientificFigureWorkflow.Create(scientific.Specification)
            .ApproveGate1(
                "gate-one-reviewer",
                "Scientific content approved.",
                DateTimeOffset.UtcNow.AddMinutes(-5));
        var service = new ScientificFigureDeliveryService(
            new ScientificFigurePackageWriter());
        var request = new ScientificFigureDeliveryRequest(
            workflow,
            scientific.Svg,
            scientific.Exports,
            ScientificContractReviewReport.Create(1, []),
            new ScientificMachineReviewDecision([]),
            [],
            [
                new ScientificDeliveryProviderMetadata(
                    ScientificReviewLayer.Semantic,
                    "fake-scientific-semantic",
                    "deterministic-fake-v1",
                    "semantic-trace"),
                new ScientificDeliveryProviderMetadata(
                    ScientificReviewLayer.Visual,
                    "fake-scientific-visual",
                    "deterministic-fake-v1",
                    "visual-trace"),
            ],
            new ScientificGateTwoDecision(
                Approved: true,
                "final-reviewer",
                "Final scientific delivery approved.",
                DateTimeOffset.UtcNow));
        return new ScientificDeliveryTestFixture(
            scientific,
            workflow,
            service,
            request);
    }
}
