using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.Tests;

public sealed class RepairPlanTests
{
    [Fact]
    public void RepairPlan_FromReviewCreatesOrderedRepairSteps()
    {
        var createdAt = DateTimeOffset.Parse("2026-06-03T16:00:00Z");
        var candidateId = Guid.NewGuid();
        var review = new StructuredReviewOutput(
            candidateId,
            ReviewDecision.Fail,
            [new StructuredReviewScore("settings", "Output settings must match the delivery format.", 2, 2)],
            [
                "Missing requirement from the creative brief.",
                "Character consistency drift across the panel sequence.",
            ],
            "Needs a structured repair plan.",
            SuggestedFix: "Revise prompt wording and regenerate.");

        var plan = RepairPlan.FromReview(review, createdAt);

        Assert.True(plan.HasRepair);
        Assert.False(plan.RequiresOperator);
        Assert.Equal(candidateId, plan.CandidateImageId);
        Assert.Equal(RepairSeverity.Regenerate, plan.Severity);
        Assert.Equal(createdAt, plan.CreatedAt);
        Assert.Equal([1, 2, 3, 4], plan.Steps.Select(step => step.Order));
        Assert.Equal(ReviewOutcomeTargetLayer.Brief, plan.Steps[0].TargetLayer);
        Assert.Contains(plan.Steps, step => step.TargetLayer is ReviewOutcomeTargetLayer.Blueprint);
        Assert.Contains(plan.Steps, step => step.TargetLayer is ReviewOutcomeTargetLayer.Settings);
        Assert.Contains(plan.Steps, step => step.TargetLayer is ReviewOutcomeTargetLayer.Prompt);
    }

    [Fact]
    public void RepairPlan_FromCleanReviewCreatesNoRepairSteps()
    {
        var review = new StructuredReviewOutput(
            Guid.NewGuid(),
            ReviewDecision.Pass,
            [new StructuredReviewScore("match", "Matches the brief.", 3, 5)],
            [],
            "Looks good.",
            SuggestedFix: null);

        var plan = RepairPlan.FromReview(review, DateTimeOffset.Parse("2026-06-03T16:00:00Z"));

        Assert.False(plan.HasRepair);
        Assert.False(plan.RequiresOperator);
        Assert.Equal(RepairSeverity.None, plan.Severity);
        Assert.Empty(plan.Steps);
    }

    [Fact]
    public void RepairPlanStep_RequiresActionsAndEvidenceForRepairSteps()
    {
        Assert.Throws<ArgumentException>(() =>
            RepairPlanStep.Create(
                order: 1,
                ReviewOutcomeTargetLayer.Prompt,
                RepairSeverity.Minor,
                [],
                ["Revise prompt wording."],
                requiresOperator: false));
        Assert.Throws<ArgumentException>(() =>
            RepairPlanStep.Create(
                order: 1,
                ReviewOutcomeTargetLayer.Prompt,
                RepairSeverity.Minor,
                ["Low prompt-match score."],
                [],
                requiresOperator: false));
    }
}
