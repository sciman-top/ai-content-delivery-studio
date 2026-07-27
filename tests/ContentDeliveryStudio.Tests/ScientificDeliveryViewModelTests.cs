using ContentDeliveryStudio.App.ViewModels;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificDeliveryViewModelTests
{
    [Fact]
    public void ReadyRequest_ExposesThreeFormatsAndCompletePreApprovalEvidence()
    {
        var fixture = ScientificDeliveryTestFixture.Create();
        var viewModel = CreateViewModel(fixture);

        Assert.True(viewModel.IsDomainEligible);
        Assert.True(viewModel.CanApproveGateTwo);
        Assert.False(viewModel.IsGateTwoApproved);
        Assert.Equal(["svg", "png", "pdf"], viewModel.Artifacts.Select(item => item.Format));
        Assert.All(viewModel.Artifacts, item => Assert.False(string.IsNullOrWhiteSpace(item.Sha256)));
        Assert.Equal(
            fixture.Workflow.Specification.Elements.Count + fixture.Workflow.Specification.Relations.Count,
            viewModel.EvidenceItems.Count);
        Assert.Equal(2, viewModel.Providers.Count);
        Assert.NotNull(viewModel.GateOneApproval);
        Assert.Empty(viewModel.UnresolvedIssues);
        Assert.False(viewModel.ExportPackageCommand.CanExecute(null));
    }

    [Fact]
    public void GateTwoApproval_RequiresHumanFieldsThenEnablesPackageExport()
    {
        var fixture = ScientificDeliveryTestFixture.Create();
        byte[]? exported = null;
        var reviewedAt = fixture.Workflow.Gate1Approval!.ReviewedAt.AddMinutes(2);
        var viewModel = CreateViewModel(fixture, bytes => exported = bytes, () => reviewedAt);

        Assert.False(viewModel.ApproveGateTwoCommand.CanExecute(null));
        viewModel.GateTwoReviewer = "delivery-reviewer";
        viewModel.GateTwoNotes = "All formats and evidence were reviewed.";
        Assert.True(viewModel.ApproveGateTwoCommand.CanExecute(null));

        viewModel.ApproveGateTwoCommand.Execute(null);

        Assert.True(viewModel.IsGateTwoApproved);
        Assert.False(viewModel.CanApproveGateTwo);
        Assert.Equal("delivery-reviewer", viewModel.GateTwoApproval!.Reviewer);
        Assert.Equal(reviewedAt, viewModel.GateTwoApproval.ReviewedAt);
        Assert.NotNull(viewModel.PackageBytes);
        Assert.True(viewModel.ExportPackageCommand.CanExecute(null));

        viewModel.ExportPackageCommand.Execute(null);
        Assert.Equal(viewModel.PackageBytes, exported);
    }

    [Fact]
    public void BlockedRequest_DisablesGateTwoAndShowsDomainReason()
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
        var request = fixture.Request with { ContractReview = failed };

        var viewModel = new ScientificDeliveryViewModel(fixture.Service, request);

        Assert.False(viewModel.IsDomainEligible);
        Assert.False(viewModel.CanApproveGateTwo);
        Assert.Contains(viewModel.UnresolvedIssues, item =>
            item.Contains("contract", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GateTwoRejection_RoutesHumanRequiredRepairWithoutPackage()
    {
        var fixture = ScientificDeliveryTestFixture.Create();
        var viewModel = CreateViewModel(fixture);
        viewModel.GateTwoReviewer = "delivery-reviewer";
        viewModel.GateTwoNotes = "Revise the scientific emphasis.";

        viewModel.RejectGateTwoCommand.Execute(null);

        Assert.True(viewModel.IsGateTwoRejected);
        Assert.False(viewModel.IsGateTwoApproved);
        Assert.Null(viewModel.PackageBytes);
        var repair = Assert.Single(viewModel.RejectionRepairActions);
        Assert.Equal(ScientificRepairLayer.FigureSpecification, repair.Layer);
        Assert.Equal(ScientificRepairExecutionMode.HumanRequired, repair.ExecutionMode);
    }

    [Fact]
    public void DeliveryView_ExposesComparisonEvidenceAndExplicitCommands()
    {
        var xaml = ReadRepoFile("ScientificDeliveryWorkspaceView.xaml");

        Assert.Contains("AutomationProperties.AutomationId=\"ScientificDeliveryFormatList\"", xaml);
        Assert.Contains("ScientificDelivery.Artifacts", xaml);
        Assert.Contains("ScientificDelivery.EvidenceItems", xaml);
        Assert.Contains("ScientificDelivery.Providers", xaml);
        Assert.Contains("ScientificDelivery.ApproveGateTwoCommand", xaml);
        Assert.Contains("ScientificDelivery.RejectGateTwoCommand", xaml);
        Assert.Contains("ScientificDelivery.ExportPackageCommand", xaml);
    }

    private static ScientificDeliveryViewModel CreateViewModel(
        ScientificDeliveryTestFixture fixture,
        Action<byte[]>? exportRequested = null,
        Func<DateTimeOffset>? clock = null)
    {
        return new ScientificDeliveryViewModel(
            fixture.Service,
            fixture.Request,
            exportRequested,
            clock);
    }

    private static string ReadRepoFile(string fileName)
    {
        return File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "ContentDeliveryStudio.App", "Views", fileName)));
    }
}
