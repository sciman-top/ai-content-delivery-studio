using System.Globalization;
using ContentDeliveryStudio.App.ViewModels;
using ContentDeliveryStudio.Application.Localization;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificFigureWorkspaceCoordinatorTests
{
    [Fact]
    public void Build_ProjectsFiveAuthoritativeStagesWithoutPromptEditingSurface()
    {
        var localization = new LocalizationService(() => new CultureInfo("en-US"));
        var coordinator = new ScientificFigureWorkflowCoordinator(localization);
        var workflow = ScientificFigureWorkflow.Create(
            ScientificContractReviewFixture.Create().Specification);

        var projection = coordinator.Build(workflow);

        Assert.Equal(workflow.Specification.SpecificationId, projection.SpecificationId);
        Assert.Equal(workflow.Specification.Version, projection.SpecificationVersion);
        Assert.Equal(workflow.State, projection.AuthoritativeState);
        Assert.Equal(
            [
                ScientificWorkspaceStage.Source,
                ScientificWorkspaceStage.Understanding,
                ScientificWorkspaceStage.FigureSpec,
                ScientificWorkspaceStage.RenderAndReview,
                ScientificWorkspaceStage.Delivery,
            ],
            projection.Workspaces.Select(item => item.Stage));
        Assert.Equal(
            ["Source", "Understanding", "Figure Spec", "Render & Review", "Delivery"],
            projection.Workspaces.Select(item => item.Title));
        Assert.Equal(ScientificWorkspaceStatus.Complete, projection.Workspaces[0].Status);
        Assert.Equal(ScientificWorkspaceStatus.Complete, projection.Workspaces[1].Status);
        Assert.Equal(ScientificWorkspaceStatus.NeedsApproval, projection.Workspaces[2].Status);
        Assert.DoesNotContain(
            typeof(ScientificFigureWorkspaceProjection).GetProperties(),
            property => property.Name.Contains("Prompt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Build_LocalizesWorkspaceTitlesAndAdvancesOnlyFromWorkflowState()
    {
        var localization = new LocalizationService(() => new CultureInfo("zh-CN"));
        localization.SetLanguage(LanguagePreference.Chinese);
        var coordinator = new ScientificFigureWorkflowCoordinator(localization);
        var workflow = ScientificFigureWorkflow.Create(
                ScientificContractReviewFixture.Create().Specification)
            .ApproveGate1("reviewer", "approved", DateTimeOffset.UtcNow)
            .RecordDownstreamApproval(
                ScientificDownstreamApprovalStage.RenderPlan,
                "renderer",
                DateTimeOffset.UtcNow);

        var projection = coordinator.Build(workflow);

        Assert.Equal(["来源", "理解", "图稿规格", "渲染与评审", "交付"], projection.Workspaces.Select(item => item.Title));
        Assert.Equal(ScientificWorkspaceStatus.Complete, projection.Workspaces[2].Status);
        Assert.Equal(ScientificWorkspaceStatus.InProgress, projection.Workspaces[3].Status);
        Assert.Equal("进行中", projection.Workspaces[3].StatusText);
        Assert.Equal(ScientificWorkspaceStatus.Pending, projection.Workspaces[4].Status);
    }
}
