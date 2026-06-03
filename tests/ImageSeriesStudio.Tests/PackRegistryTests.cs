using ImageSeriesStudio.Core.Packs;

namespace ImageSeriesStudio.Tests;

public sealed class PackRegistryTests
{
    [Fact]
    public void PackRegistry_AcceptsCompatiblePacksAndResolvesById()
    {
        var createdAt = DateTimeOffset.Parse("2026-06-03T12:30:00Z");
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
        var workflow = WorkflowPack.Create(
            "article-workflow",
            "Article Workflow",
            "1.0.0",
            compatibility,
            ["Source", "Brief", "Plan", "Produce", "Review", "Deliver"],
            ["article-blueprints"],
            PackLifecycleState.Active,
            [],
            createdAt);

        var registry = PackRegistry.Create("1.5.0", [workflow, blueprint]);

        Assert.Equal(2, registry.Packs.Count);
        Assert.Same(workflow, registry.GetRequired<WorkflowPack>("article-workflow"));
        Assert.Same(blueprint, registry.GetRequired<BlueprintPack>("article-blueprints"));
    }

    [Fact]
    public void PackRegistry_RejectsDuplicateIdsIncompatiblePacksAndMissingBlueprintReferences()
    {
        var createdAt = DateTimeOffset.Parse("2026-06-03T12:30:00Z");
        var compatibility = PackCompatibilityRange.Create("1.0.0", "2.0.0");
        var workflow = WorkflowPack.Create(
            "article-workflow",
            "Article Workflow",
            "1.0.0",
            compatibility,
            ["Source", "Brief", "Plan"],
            ["missing-blueprints"],
            PackLifecycleState.Active,
            [],
            createdAt);
        var duplicate = BlueprintPack.Create(
            "article-workflow",
            "Duplicate Id",
            "1.0.0",
            compatibility,
            ["article-illustration"],
            PackLifecycleState.Active,
            [],
            createdAt);
        var incompatible = BlueprintPack.Create(
            "future-blueprints",
            "Future Blueprints",
            "1.0.0",
            PackCompatibilityRange.Create("3.0.0", null),
            ["future"],
            PackLifecycleState.Active,
            [],
            createdAt);

        Assert.Throws<ArgumentException>(() => PackRegistry.Create("1.5.0", [workflow, duplicate]));
        Assert.Throws<InvalidOperationException>(() => PackRegistry.Create("1.5.0", [incompatible]));
        Assert.Throws<InvalidOperationException>(() => PackRegistry.Create("1.5.0", [workflow]));
    }
}
