using ImageSeriesStudio.Core.Packs;

namespace ImageSeriesStudio.Tests;

public sealed class WorkflowStageDefinitionTests
{
    [Fact]
    public void WorkflowPack_CanUseStageDefinitionsWithCompletionCriteria()
    {
        var createdAt = DateTimeOffset.Parse("2026-06-03T13:30:00Z");
        var compatibility = PackCompatibilityRange.Create("1.0.0", "2.0.0");
        var source = WorkflowStageDefinition.Create(
            " Source ",
            "Source",
            ["At least one source asset is attached."],
            required: true);
        var review = WorkflowStageDefinition.Create(
            "Review",
            "Review",
            ["Every candidate has structured review output."],
            required: true);

        var workflow = WorkflowPack.CreateWithStages(
            "review-workflow",
            "Review Workflow",
            "1.0.0",
            compatibility,
            [source, review],
            [],
            PackLifecycleState.Active,
            [],
            createdAt);

        Assert.Equal(["Source", "Review"], workflow.StageIds);
        Assert.Equal("At least one source asset is attached.", workflow.StageDefinitions[0].CompletionCriteria[0]);
        Assert.True(workflow.StageDefinitions[0].Required);
        Assert.Equal("Review", workflow.StageDefinitions[1].DisplayName);
    }

    [Fact]
    public void WorkflowPack_RejectsDuplicateStageDefinitionsAndMissingCompletionCriteria()
    {
        var createdAt = DateTimeOffset.Parse("2026-06-03T13:30:00Z");
        var compatibility = PackCompatibilityRange.Create("1.0.0", "2.0.0");
        var source = WorkflowStageDefinition.Create("Source", "Source", ["source done"], required: true);
        var duplicate = WorkflowStageDefinition.Create("source", "Source duplicate", ["source done"], required: true);

        Assert.Throws<ArgumentException>(() =>
            WorkflowStageDefinition.Create("Review", "Review", [], required: true));
        Assert.Throws<ArgumentException>(() =>
            WorkflowPack.CreateWithStages(
                "bad-workflow",
                "Bad Workflow",
                "1.0.0",
                compatibility,
                [source, duplicate],
                [],
                PackLifecycleState.Active,
                [],
                createdAt));
    }
}
