using ContentDeliveryStudio.Core.Packs;

namespace ContentDeliveryStudio.Tests;

public sealed class PackUiDefaultsTests
{
    [Fact]
    public void WorkflowPack_CanDeclareUiDefaultsUsingStableViewSlots()
    {
        var createdAt = DateTimeOffset.Parse("2026-06-03T14:30:00Z");
        var compatibility = PackCompatibilityRange.Create("1.0.0", "2.0.0");
        var uiDefaults = WorkflowPackUiDefaults.Create(
            "Brief",
            [
                WorkflowViewSlotDefault.Create("SourceList", "Source", visibleByDefault: true, order: 0),
                WorkflowViewSlotDefault.Create("StageWorkspace", "Brief", visibleByDefault: true, order: 10),
                WorkflowViewSlotDefault.Create("Inspector", "Review", visibleByDefault: false, order: 20),
            ]);

        var workflow = WorkflowPack.CreateWithStagesAndUiDefaults(
            "article-workflow",
            "Article Workflow",
            "1.0.0",
            compatibility,
            [
                WorkflowStageDefinition.Create("Source", "Source", ["Source assets are attached."], required: true),
                WorkflowStageDefinition.Create("Brief", "Brief", ["Creative brief is approved."], required: true),
                WorkflowStageDefinition.Create("Review", "Review", ["Review findings are resolved."], required: true),
            ],
            ["article-blueprints"],
            uiDefaults,
            PackLifecycleState.Active,
            [],
            createdAt);

        Assert.Equal("Brief", workflow.UiDefaults.DefaultStageId);
        Assert.Equal(["SourceList", "StageWorkspace", "Inspector"], workflow.UiDefaults.ViewSlots.Select(slot => slot.SlotId));
        Assert.Equal("Review", workflow.UiDefaults.ViewSlots[2].StageId);
    }

    [Fact]
    public void WorkflowViewSlotDefault_RejectsPackSpecificGlobalTabVocabulary()
    {
        Assert.Throws<ArgumentException>(() =>
            WorkflowViewSlotDefault.Create("ArticleReviewTab", "Review", visibleByDefault: true, order: 0));
        Assert.Throws<ArgumentException>(() =>
            WorkflowViewSlotDefault.Create("GlobalTab", "Review", visibleByDefault: true, order: 0));
    }

    [Fact]
    public void PackRegistry_RejectsUiDefaultsThatReferenceMissingWorkflowStages()
    {
        var createdAt = DateTimeOffset.Parse("2026-06-03T14:30:00Z");
        var compatibility = PackCompatibilityRange.Create("1.0.0", "2.0.0");
        var blueprint = BlueprintPack.Create(
            "article-blueprints",
            "Article Blueprints",
            "1.0.0",
            compatibility,
            ["article-illustration"],
            PackLifecycleState.Active,
            [],
            createdAt);
        var workflow = WorkflowPack.CreateWithStagesAndUiDefaults(
            "article-workflow",
            "Article Workflow",
            "1.0.0",
            compatibility,
            [WorkflowStageDefinition.Create("Source", "Source", ["Source assets are attached."], required: true)],
            ["article-blueprints"],
            WorkflowPackUiDefaults.Create(
                "Review",
                [WorkflowViewSlotDefault.Create("StageWorkspace", "Review", visibleByDefault: true, order: 0)]),
            PackLifecycleState.Active,
            [],
            createdAt);

        Assert.Throws<InvalidOperationException>(() => PackRegistry.Create("1.5.0", [workflow, blueprint]));
    }
}
