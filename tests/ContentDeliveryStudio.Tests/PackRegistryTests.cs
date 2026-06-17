using ContentDeliveryStudio.Core.Packs;

namespace ContentDeliveryStudio.Tests;

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

    [Fact]
    public void PackRegistry_RejectsMissingPolicyReferencesAndDuplicateScenarioIds()
    {
        var createdAt = DateTimeOffset.Parse("2026-06-03T12:30:00Z");
        var compatibility = PackCompatibilityRange.Create("1.0.0", "2.0.0");
        var blueprint = BlueprintPack.Create(
            "generic-image-series-blueprints",
            "Generic Image Series Blueprints",
            "1.0.0",
            compatibility,
            ["single-image"],
            PackLifecycleState.Active,
            [],
            createdAt);
        var industry = IndustryPack.Create(
            "generic-image-series-industry",
            "Generic Image Series Audience",
            "1.0.0",
            compatibility,
            ["teacher"],
            ["generic-image-series"],
            PackLifecycleState.Active,
            [],
            createdAt);
        var renderer = RendererPack.Create(
            "generic-image-series-renderer",
            "Generic Image Series Renderer",
            "1.0.0",
            compatibility,
            ["png"],
            PackLifecycleState.Active,
            [],
            createdAt);
        var rubric = ReviewRubricPack.Create(
            "generic-image-series-review-rubric",
            "Generic Image Series Review Rubric",
            "1.0.0",
            compatibility,
            ["general-image"],
            PackLifecycleState.Active,
            [],
            createdAt);
        var missingPolicyWorkflow = WorkflowPack.Create(
            "generic-image-series",
            "Generic Image Series",
            "1.0.0",
            compatibility,
            ["Source", "Brief", "Plan", "Produce", "Review", "Repair", "Deliver"],
            ["generic-image-series-blueprints"],
            PackLifecycleState.Active,
            [],
            createdAt,
            scenarioIds: ["generic-image-series"],
            industryPackIds: ["missing-industry"],
            rendererPackIds: ["missing-renderer"],
            reviewRubricPackIds: ["missing-review-rubric"]);
        var validWorkflow = WorkflowPack.Create(
            "generic-image-series",
            "Generic Image Series",
            "1.0.0",
            compatibility,
            ["Source", "Brief", "Plan", "Produce", "Review", "Repair", "Deliver"],
            ["generic-image-series-blueprints"],
            PackLifecycleState.Active,
            [],
            createdAt,
            scenarioIds: ["generic-image-series"],
            industryPackIds: ["generic-image-series-industry"],
            rendererPackIds: ["generic-image-series-renderer"],
            reviewRubricPackIds: ["generic-image-series-review-rubric"]);
        var duplicateScenarioWorkflow = WorkflowPack.Create(
            "duplicate-scenario",
            "Duplicate Scenario",
            "1.0.0",
            compatibility,
            ["Source", "Brief", "Plan", "Produce", "Review", "Deliver"],
            ["generic-image-series-blueprints"],
            PackLifecycleState.Active,
            [],
            createdAt,
            scenarioIds: ["generic-image-series"]);

        Assert.Throws<InvalidOperationException>(() =>
            PackRegistry.Create("1.5.0", [missingPolicyWorkflow, blueprint, industry, renderer, rubric]));
        Assert.Throws<ArgumentException>(() =>
            PackRegistry.Create("1.5.0", [validWorkflow, duplicateScenarioWorkflow, blueprint, industry, renderer, rubric]));
    }
}
