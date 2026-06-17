using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.Tests;

public sealed class ReviewOutcomeRoutingTests
{
    [Fact]
    public void FromReview_ReturnsNoRepairRouteForCleanPass()
    {
        var review = new StructuredReviewOutput(
            Guid.NewGuid(),
            ReviewDecision.Pass,
            [new StructuredReviewScore("match", "Matches the prompt.", 3, 5)],
            [],
            "Looks good.",
            SuggestedFix: null);

        var plan = ReviewOutcomeRoutingPlan.FromReview(review);

        Assert.False(plan.RequiresRepair);
        Assert.Equal(ReviewOutcomeTargetLayer.None, plan.PrimaryRoute.TargetLayer);
        Assert.Equal(RepairSeverity.None, plan.PrimaryRoute.Severity);
    }

    [Fact]
    public void FromReview_RoutesEvidenceToBriefBlueprintPromptAndSettingsLayers()
    {
        var review = new StructuredReviewOutput(
            Guid.NewGuid(),
            ReviewDecision.Fail,
            [
                new StructuredReviewScore("prompt-match", "Prompt content should be followed.", 3, 2),
                new StructuredReviewScore("quality", "Final quality should be high.", 2, 4),
            ],
            [
                "Missing requirement from the creative brief.",
                "Character consistency drift across the panel sequence.",
                "Wrong aspect ratio and output size.",
            ],
            "Multiple repair layers are involved.",
            SuggestedFix: "Improve the prompt wording before retry.");

        var plan = ReviewOutcomeRoutingPlan.FromReview(review);

        Assert.True(plan.RequiresRepair);
        Assert.Equal(ReviewOutcomeTargetLayer.Brief, plan.PrimaryRoute.TargetLayer);
        Assert.All(plan.Routes, route => Assert.Equal(RepairSeverity.Regenerate, route.Severity));
        Assert.Contains(plan.Routes, route => route.TargetLayer is ReviewOutcomeTargetLayer.Brief);
        Assert.Contains(plan.Routes, route => route.TargetLayer is ReviewOutcomeTargetLayer.Blueprint);
        Assert.Contains(plan.Routes, route => route.TargetLayer is ReviewOutcomeTargetLayer.Settings);
        Assert.Contains(plan.Routes, route => route.TargetLayer is ReviewOutcomeTargetLayer.Prompt);
        Assert.Contains(
            plan.Routes.Single(route => route.TargetLayer is ReviewOutcomeTargetLayer.Blueprint).Actions,
            action => action.Contains("blueprint", StringComparison.OrdinalIgnoreCase));
    }
}
