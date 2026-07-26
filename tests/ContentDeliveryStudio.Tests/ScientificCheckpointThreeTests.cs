using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificCheckpointThreeTests
{
    [Fact]
    public async Task ApprovedFakeChain_PreservesScienceAndRequiresBothHumanDecisions()
    {
        var fixture = ScientificDeliveryTestFixture.Create();
        var contract = new ScientificContractReviewer().Review(
            fixture.ScientificFixture.Request);
        var prep = new ScientificReviewPrepBuilder(
            new SkiaScientificReviewImageCropper()).Build(
                fixture.ScientificFixture.Understanding,
                fixture.ScientificFixture.Specification,
                fixture.ScientificFixture.Plan,
                fixture.ScientificFixture.Svg,
                fixture.ScientificFixture.Exports);
        var machine = await new ScientificReviewExecutionService(
            new FakeScientificSemanticReviewProvider(),
            new FakeScientificVisualReviewProvider()).ReviewAsync(
                prep.SemanticRequest,
                prep.VisualRequest,
                CancellationToken.None);
        var repairService = new ScientificRepairApplicationService();
        var repair = ScientificRepairPlan.Create(
            [repairService.RouteFinding(
                "layout-spacing",
                "element-force",
                "Increase spacing.",
                ScientificRepairLayer.LayoutStyle)]);
        var repairState = repairService.RecordAutomaticAttempt(
            repair,
            ScientificRepairLoopState.Start());

        var result = fixture.Service.DecideGateTwo(
            fixture.Request with
            {
                ContractReview = contract,
                MachineReview = machine,
                Repairs =
                [
                    new ScientificRepairRecord(
                        repair.AutomaticActions.Single(),
                        ScientificRepairRecordStatus.Resolved,
                        "Spacing adjusted without changing scientific content."),
                ],
            });

        Assert.True(contract.Passed);
        Assert.True(machine.CanProceedToGate2);
        Assert.Equal(1, repairState.CompletedAutomaticAttempts);
        Assert.Same(fixture.Workflow.Specification, result.Package!.Specification);
        Assert.NotNull(result.Package.GateOneApproval);
        Assert.NotNull(result.Package.GateTwoApproval);
        Assert.NotEmpty(result.PackageBytes!);
    }
}
