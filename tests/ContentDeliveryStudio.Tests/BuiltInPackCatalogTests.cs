using ContentDeliveryStudio.Core.Packs;
using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.Tests;

public sealed class BuiltInPackCatalogTests
{
    [Fact]
    public void BuiltInPackCatalog_CreatesGenericImageSeriesRegistry()
    {
        var createdAt = DateTimeOffset.Parse("2026-06-03T13:00:00Z");

        var registry = BuiltInPackCatalog.CreateGenericImageSeriesRegistry("1.5.0", createdAt);
        var workflow = registry.GetRequired<WorkflowPack>(BuiltInPackCatalog.GenericImageSeriesWorkflowPackId);
        var blueprint = registry.GetRequired<BlueprintPack>(BuiltInPackCatalog.GenericImageSeriesBlueprintPackId);
        var industry = registry.GetRequired<IndustryPack>(BuiltInPackCatalog.GenericImageSeriesIndustryPackId);
        var renderer = registry.GetRequired<RendererPack>(BuiltInPackCatalog.GenericImageSeriesRendererPackId);
        var rubric = registry.GetRequired<ReviewRubricPack>(BuiltInPackCatalog.GenericImageSeriesReviewRubricPackId);

        Assert.Equal("generic-image-series", workflow.Metadata.Id);
        Assert.Equal(PackLifecycleState.Active, workflow.Metadata.LifecycleState);
        Assert.Equal(["generic-image-series"], workflow.ScenarioIds);
        Assert.Equal([BuiltInPackCatalog.GenericImageSeriesIndustryPackId], workflow.IndustryPackIds);
        Assert.Equal([BuiltInPackCatalog.GenericImageSeriesRendererPackId], workflow.RendererPackIds);
        Assert.Equal([BuiltInPackCatalog.GenericImageSeriesReviewRubricPackId], workflow.ReviewRubricPackIds);
        Assert.Equal(["Source", "Brief", "Plan", "Produce", "Review", "Repair", "Deliver"], workflow.StageIds);
        Assert.Equal([BuiltInPackCatalog.GenericImageSeriesBlueprintPackId], workflow.BlueprintPackIds);
        Assert.Contains("single-image", blueprint.BlueprintIds);
        Assert.Contains("image-series", blueprint.BlueprintIds);
        Assert.Contains("panel-sequence", blueprint.BlueprintIds);
        Assert.Contains("teacher", industry.AudienceTags);
        Assert.Contains("png", renderer.OutputFormats);
        Assert.Contains("general-image", rubric.RubricTemplateIds);
        Assert.Equal(5, registry.Packs.Count);
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
        Assert.NotNull(registry.GetRequired<IndustryPack>(BuiltInPackCatalog.GenericImageSeriesIndustryPackId));
        Assert.NotNull(registry.GetRequired<RendererPack>(BuiltInPackCatalog.GenericImageSeriesRendererPackId));
        Assert.NotNull(registry.GetRequired<ReviewRubricPack>(BuiltInPackCatalog.GenericImageSeriesReviewRubricPackId));
        Assert.NotNull(registry.GetRequired<IndustryPack>(BuiltInPackCatalog.ArticleIllustrationIndustryPackId));
        Assert.NotNull(registry.GetRequired<RendererPack>(BuiltInPackCatalog.ArticleIllustrationRendererPackId));
        Assert.NotNull(registry.GetRequired<ReviewRubricPack>(BuiltInPackCatalog.ArticleIllustrationReviewRubricPackId));
        Assert.NotNull(registry.GetRequired<IndustryPack>(BuiltInPackCatalog.DocumentReviewTranslationIndustryPackId));
        Assert.NotNull(registry.GetRequired<RendererPack>(BuiltInPackCatalog.DocumentReviewTranslationRendererPackId));
        Assert.NotNull(registry.GetRequired<ReviewRubricPack>(BuiltInPackCatalog.DocumentReviewTranslationReviewRubricPackId));
        Assert.NotNull(registry.GetRequired<IndustryPack>(BuiltInPackCatalog.CoursewareVisualIndustryPackId));
        Assert.NotNull(registry.GetRequired<RendererPack>(BuiltInPackCatalog.CoursewareVisualRendererPackId));
        Assert.NotNull(registry.GetRequired<ReviewRubricPack>(BuiltInPackCatalog.CoursewareVisualReviewRubricPackId));
        Assert.NotNull(registry.GetRequired<IndustryPack>(BuiltInPackCatalog.PosterReportDeliveryIndustryPackId));
        Assert.NotNull(registry.GetRequired<RendererPack>(BuiltInPackCatalog.PosterReportDeliveryRendererPackId));
        Assert.NotNull(registry.GetRequired<ReviewRubricPack>(BuiltInPackCatalog.PosterReportDeliveryReviewRubricPackId));

        var article = registry.GetRequired<BlueprintPack>(BuiltInPackCatalog.ArticleIllustrationBlueprintPackId);
        var articleWorkflow = registry.GetRequired<WorkflowPack>(BuiltInPackCatalog.ArticleIllustrationWorkflowPackId);
        var document = registry.GetRequired<BlueprintPack>(BuiltInPackCatalog.DocumentReviewTranslationBlueprintPackId);
        var documentWorkflow = registry.GetRequired<WorkflowPack>(BuiltInPackCatalog.DocumentReviewTranslationWorkflowPackId);
        var documentRenderer = registry.GetRequired<RendererPack>(BuiltInPackCatalog.DocumentReviewTranslationRendererPackId);
        var documentRubric = registry.GetRequired<ReviewRubricPack>(BuiltInPackCatalog.DocumentReviewTranslationReviewRubricPackId);
        var courseware = registry.GetRequired<BlueprintPack>(BuiltInPackCatalog.CoursewareVisualBlueprintPackId);
        var coursewareWorkflow = registry.GetRequired<WorkflowPack>(BuiltInPackCatalog.CoursewareVisualWorkflowPackId);
        var coursewareRenderer = registry.GetRequired<RendererPack>(BuiltInPackCatalog.CoursewareVisualRendererPackId);
        var coursewareRubric = registry.GetRequired<ReviewRubricPack>(BuiltInPackCatalog.CoursewareVisualReviewRubricPackId);
        var poster = registry.GetRequired<BlueprintPack>(BuiltInPackCatalog.PosterReportDeliveryBlueprintPackId);
        var posterWorkflow = registry.GetRequired<WorkflowPack>(BuiltInPackCatalog.PosterReportDeliveryWorkflowPackId);
        var posterRenderer = registry.GetRequired<RendererPack>(BuiltInPackCatalog.PosterReportDeliveryRendererPackId);
        var posterRubric = registry.GetRequired<ReviewRubricPack>(BuiltInPackCatalog.PosterReportDeliveryReviewRubricPackId);

        Assert.Contains("article-inline-illustration", article.BlueprintIds);
        Assert.Equal(["article-illustration"], articleWorkflow.ScenarioIds);
        Assert.Equal([BuiltInPackCatalog.ArticleIllustrationIndustryPackId], articleWorkflow.IndustryPackIds);
        Assert.Equal([BuiltInPackCatalog.ArticleIllustrationRendererPackId], articleWorkflow.RendererPackIds);
        Assert.Equal([BuiltInPackCatalog.ArticleIllustrationReviewRubricPackId], articleWorkflow.ReviewRubricPackIds);
        Assert.Contains("translation-review-report", document.BlueprintIds);
        Assert.Equal(["document-review-translation"], documentWorkflow.ScenarioIds);
        Assert.Equal([BuiltInPackCatalog.DocumentReviewTranslationIndustryPackId], documentWorkflow.IndustryPackIds);
        Assert.Equal([BuiltInPackCatalog.DocumentReviewTranslationRendererPackId], documentWorkflow.RendererPackIds);
        Assert.Equal([BuiltInPackCatalog.DocumentReviewTranslationReviewRubricPackId], documentWorkflow.ReviewRubricPackIds);
        Assert.Contains("markdown", documentRenderer.OutputFormats);
        Assert.Contains(ReviewRubricTemplateCatalog.ScholarlySchematic, documentRubric.RubricTemplateIds);
        Assert.Contains("lesson-slide-visual", courseware.BlueprintIds);
        Assert.Equal(["courseware-visual"], coursewareWorkflow.ScenarioIds);
        Assert.Equal([BuiltInPackCatalog.CoursewareVisualIndustryPackId], coursewareWorkflow.IndustryPackIds);
        Assert.Equal([BuiltInPackCatalog.CoursewareVisualRendererPackId], coursewareWorkflow.RendererPackIds);
        Assert.Equal([BuiltInPackCatalog.CoursewareVisualReviewRubricPackId], coursewareWorkflow.ReviewRubricPackIds);
        Assert.Contains("pptx", coursewareRenderer.OutputFormats);
        Assert.Contains(ReviewRubricTemplateCatalog.EducationalAccuracy, coursewareRubric.RubricTemplateIds);
        Assert.Contains("poster-and-report-package", poster.BlueprintIds);
        Assert.Equal(["poster-report-delivery"], posterWorkflow.ScenarioIds);
        Assert.Equal([BuiltInPackCatalog.PosterReportDeliveryIndustryPackId], posterWorkflow.IndustryPackIds);
        Assert.Equal([BuiltInPackCatalog.PosterReportDeliveryRendererPackId], posterWorkflow.RendererPackIds);
        Assert.Equal([BuiltInPackCatalog.PosterReportDeliveryReviewRubricPackId], posterWorkflow.ReviewRubricPackIds);
        Assert.Contains("pdf", posterRenderer.OutputFormats);
        Assert.Contains(ReviewRubricTemplateCatalog.TextHeavyPoster, posterRubric.RubricTemplateIds);
        Assert.Equal(25, registry.Packs.Count);
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

    [Fact]
    public void BuiltInPackCatalog_StarterWorkflowsCarryScenarioAndPolicyLinks()
    {
        var createdAt = DateTimeOffset.Parse("2026-06-03T13:00:00Z");
        var registry = BuiltInPackCatalog.CreateStarterPackRegistry("1.5.0", createdAt);
        var starterWorkflowIds = new HashSet<string>(
            [
                BuiltInPackCatalog.GenericImageSeriesWorkflowPackId,
                BuiltInPackCatalog.ArticleIllustrationWorkflowPackId,
                BuiltInPackCatalog.DocumentReviewTranslationWorkflowPackId,
                BuiltInPackCatalog.CoursewareVisualWorkflowPackId,
                BuiltInPackCatalog.PosterReportDeliveryWorkflowPackId,
            ],
            StringComparer.OrdinalIgnoreCase);

        Assert.All(
            registry.Packs
                .OfType<WorkflowPack>()
                .Where(workflow => starterWorkflowIds.Contains(workflow.Metadata.Id)),
            workflow =>
            {
                Assert.Single(workflow.ScenarioIds);
                Assert.Equal(workflow.Metadata.Id, workflow.ScenarioIds[0], ignoreCase: true);
                Assert.Single(workflow.IndustryPackIds);
                Assert.Single(workflow.RendererPackIds);
                Assert.Single(workflow.ReviewRubricPackIds);

                Assert.NotNull(registry.GetRequired<IndustryPack>(workflow.IndustryPackIds[0]));
                Assert.NotNull(registry.GetRequired<RendererPack>(workflow.RendererPackIds[0]));
                Assert.NotNull(registry.GetRequired<ReviewRubricPack>(workflow.ReviewRubricPackIds[0]));
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
