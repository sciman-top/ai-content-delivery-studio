using System.Globalization;
using ContentDeliveryStudio.App.ViewModels;
using ContentDeliveryStudio.Application.Localization;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificCheckpointFourTests
{
    [Fact]
    public void FakeWorkspace_ProjectsCompleteFiveStageAuthorityChain()
    {
        var localization = new LocalizationService(() => new CultureInfo("en-US"));
        var tab = new ScientificFigureWorkspaceFactory(localization).Create();

        Assert.Equal(WorkbenchTabKind.ScientificFigure, tab.Kind);
        Assert.Equal(ScientificFigureWorkflowState.ReviewPassed, tab.ScientificWorkspace!.AuthoritativeState);
        Assert.Equal(5, tab.ScientificWorkspace.Workspaces.Count);
        Assert.True(tab.ScientificSourceUnderstanding!.CanProceed);
        Assert.True(tab.ScientificFigureSpec!.IsGateOneApproved);
        Assert.NotNull(tab.ScientificFigureSpec.FrozenUnderstandingVersion);
        Assert.NotNull(tab.ScientificFigureSpec.FrozenSpecificationVersion);
        Assert.True(tab.ScientificRenderReview!.CanProceedToGateTwo);
        Assert.True(tab.ScientificDelivery!.IsDomainEligible);
        Assert.Equal(["svg", "png", "pdf"], tab.ScientificDelivery.Artifacts.Select(item => item.Format));
    }

    [Fact]
    public void FakeWorkspace_UserCanExplicitlyApproveAndExportGateTwoPackage()
    {
        byte[]? exported = null;
        var localization = new LocalizationService(() => new CultureInfo("en-US"));
        var tab = new ScientificFigureWorkspaceFactory(
            localization,
            bytes => exported = bytes).Create();
        var delivery = tab.ScientificDelivery!;
        delivery.GateTwoReviewer = "checkpoint-four-reviewer";
        delivery.GateTwoNotes = "Compared all formats and reviewed the complete evidence chain.";

        delivery.ApproveGateTwoCommand.Execute(null);
        delivery.ExportPackageCommand.Execute(null);

        Assert.True(delivery.IsGateTwoApproved);
        Assert.NotNull(delivery.GateTwoApproval);
        Assert.NotNull(delivery.PackageBytes);
        Assert.NotEmpty(exported!);
        Assert.Equal(delivery.PackageBytes, exported);
    }

    [Fact]
    public void ScientificModule_IsVisibleOnlyAtCompletedFakeFirstCheckpoint()
    {
        Assert.True(ScientificFigureModule.IsUserVisible);
        var payload = new MainWindowLocalizationCoordinator(
            new LocalizationService(() => new CultureInfo("en-US")))
            .BuildPayload();

        Assert.Equal(9, payload.WorkbenchTabs.Count);
        var scientific = Assert.Single(payload.WorkbenchTabs, tab =>
            tab.Kind == WorkbenchTabKind.ScientificFigure);
        Assert.NotNull(scientific.ScientificSourceUnderstanding);
        Assert.NotNull(scientific.ScientificFigureSpec);
        Assert.NotNull(scientific.ScientificRenderReview);
        Assert.NotNull(scientific.ScientificDelivery);
    }
}
