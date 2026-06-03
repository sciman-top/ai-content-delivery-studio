using ImageSeriesStudio.Core.Packs;

namespace ImageSeriesStudio.Tests;

public sealed class BuiltInPackCatalogTests
{
    [Fact]
    public void BuiltInPackCatalog_CreatesGenericImageSeriesRegistry()
    {
        var createdAt = DateTimeOffset.Parse("2026-06-03T13:00:00Z");

        var registry = BuiltInPackCatalog.CreateGenericImageSeriesRegistry("1.5.0", createdAt);
        var workflow = registry.GetRequired<WorkflowPack>(BuiltInPackCatalog.GenericImageSeriesWorkflowPackId);
        var blueprint = registry.GetRequired<BlueprintPack>(BuiltInPackCatalog.GenericImageSeriesBlueprintPackId);

        Assert.Equal("generic-image-series", workflow.Metadata.Id);
        Assert.Equal(PackLifecycleState.Active, workflow.Metadata.LifecycleState);
        Assert.Equal(["Source", "Brief", "Plan", "Produce", "Review", "Repair", "Deliver"], workflow.StageIds);
        Assert.Equal([BuiltInPackCatalog.GenericImageSeriesBlueprintPackId], workflow.BlueprintPackIds);
        Assert.Contains("single-image", blueprint.BlueprintIds);
        Assert.Contains("image-series", blueprint.BlueprintIds);
        Assert.Contains("panel-sequence", blueprint.BlueprintIds);
    }

    [Fact]
    public void BuiltInPackCatalog_CreatesStarterPackRegistry()
    {
        var createdAt = DateTimeOffset.Parse("2026-06-03T13:00:00Z");

        var registry = BuiltInPackCatalog.CreateStarterPackRegistry("1.5.0", createdAt);

        Assert.NotNull(registry.GetRequired<WorkflowPack>(BuiltInPackCatalog.GenericImageSeriesWorkflowPackId));
        Assert.NotNull(registry.GetRequired<WorkflowPack>(BuiltInPackCatalog.ArticleIllustrationWorkflowPackId));
        Assert.NotNull(registry.GetRequired<WorkflowPack>(BuiltInPackCatalog.DocumentReviewTranslationWorkflowPackId));
        Assert.NotNull(registry.GetRequired<WorkflowPack>(BuiltInPackCatalog.CoursewareVisualWorkflowPackId));
        Assert.NotNull(registry.GetRequired<WorkflowPack>(BuiltInPackCatalog.PosterReportDeliveryWorkflowPackId));

        var article = registry.GetRequired<BlueprintPack>(BuiltInPackCatalog.ArticleIllustrationBlueprintPackId);
        var document = registry.GetRequired<BlueprintPack>(BuiltInPackCatalog.DocumentReviewTranslationBlueprintPackId);
        var courseware = registry.GetRequired<BlueprintPack>(BuiltInPackCatalog.CoursewareVisualBlueprintPackId);
        var poster = registry.GetRequired<BlueprintPack>(BuiltInPackCatalog.PosterReportDeliveryBlueprintPackId);

        Assert.Contains("article-inline-illustration", article.BlueprintIds);
        Assert.Contains("translation-review-report", document.BlueprintIds);
        Assert.Contains("lesson-slide-visual", courseware.BlueprintIds);
        Assert.Contains("poster-and-report-package", poster.BlueprintIds);
        Assert.Equal(10, registry.Packs.Count);
    }

    [Fact]
    public void BuiltInPackCatalog_UsesCanonicalIdsCompatibleRangesAndMigrationPolicy()
    {
        var createdAt = DateTimeOffset.Parse("2026-06-03T13:00:00Z");

        var registry = BuiltInPackCatalog.CreateStarterPackRegistry("1.5.0", createdAt);
        var packIds = registry.Packs.Select(pack => pack.Metadata.Id).ToArray();

        Assert.Equal(packIds.Length, packIds.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(registry.Packs, pack =>
        {
            Assert.True(IsCanonicalPackId(pack.Metadata.Id), $"Pack id is not canonical: {pack.Metadata.Id}");
            Assert.Equal("1.0.0", pack.Metadata.Compatibility.MinimumAppVersion.ToString());
            Assert.Equal("2.0.0", pack.Metadata.Compatibility.MaximumAppVersion?.ToString());

            if (pack.Metadata.LifecycleState is PackLifecycleState.Active)
            {
                Assert.Empty(pack.Metadata.MigrationNotes);
            }
            else
            {
                Assert.NotEmpty(pack.Metadata.MigrationNotes);
            }
        });
    }

    [Fact]
    public void BuiltInPackCatalog_WorkflowsUseStableStagesAndViewSlots()
    {
        var createdAt = DateTimeOffset.Parse("2026-06-03T13:00:00Z");
        var allowedStageIds = new HashSet<string>(
            ["Source", "Brief", "Plan", "Produce", "Review", "Repair", "Deliver"],
            StringComparer.OrdinalIgnoreCase);

        var registry = BuiltInPackCatalog.CreateStarterPackRegistry("1.5.0", createdAt);

        Assert.All(registry.Packs.OfType<WorkflowPack>(), workflow =>
        {
            Assert.Subset(allowedStageIds, workflow.StageIds.ToHashSet(StringComparer.OrdinalIgnoreCase));
            Assert.Contains(workflow.UiDefaults.DefaultStageId, workflow.StageIds, StringComparer.OrdinalIgnoreCase);
            Assert.All(workflow.UiDefaults.ViewSlots, slot =>
            {
                Assert.Contains(slot.SlotId, WorkflowViewSlotIds.AllowedIds, StringComparer.OrdinalIgnoreCase);
                Assert.DoesNotContain("Tab", slot.SlotId, StringComparison.OrdinalIgnoreCase);
                Assert.Contains(slot.StageId, workflow.StageIds, StringComparer.OrdinalIgnoreCase);
            });
        });
    }

    private static bool IsCanonicalPackId(string packId)
    {
        return packId.Length > 0
            && packId.All(character =>
                char.IsAsciiLetterLower(character)
                || char.IsDigit(character)
                || character is '-' or '_' or '.');
    }
}
