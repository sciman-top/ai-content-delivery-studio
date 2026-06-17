using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.Core.Packs;

public static class BuiltInPackCatalog
{
    public const string GenericImageSeriesWorkflowPackId = "generic-image-series";

    public const string GenericImageSeriesBlueprintPackId = "generic-image-series-blueprints";

    public const string GenericImageSeriesIndustryPackId = "generic-image-series-industry";

    public const string GenericImageSeriesRendererPackId = "generic-image-series-renderer";

    public const string GenericImageSeriesReviewRubricPackId = "generic-image-series-review-rubric";

    public const string ArticleIllustrationWorkflowPackId = "article-illustration";

    public const string ArticleIllustrationBlueprintPackId = "article-illustration-blueprints";

    public const string ArticleIllustrationIndustryPackId = "article-illustration-industry";

    public const string ArticleIllustrationRendererPackId = "article-illustration-renderer";

    public const string ArticleIllustrationReviewRubricPackId = "article-illustration-review-rubric";

    public const string DocumentReviewTranslationWorkflowPackId = "document-review-translation";

    public const string DocumentReviewTranslationBlueprintPackId = "document-review-translation-blueprints";

    public const string DocumentReviewTranslationIndustryPackId = "document-review-translation-industry";

    public const string DocumentReviewTranslationRendererPackId = "document-review-translation-renderer";

    public const string DocumentReviewTranslationReviewRubricPackId = "document-review-translation-review-rubric";

    public const string CoursewareVisualWorkflowPackId = "courseware-visual";

    public const string CoursewareVisualBlueprintPackId = "courseware-visual-blueprints";

    public const string CoursewareVisualIndustryPackId = "courseware-visual-industry";

    public const string CoursewareVisualRendererPackId = "courseware-visual-renderer";

    public const string CoursewareVisualReviewRubricPackId = "courseware-visual-review-rubric";

    public const string PosterReportDeliveryWorkflowPackId = "poster-report-delivery";

    public const string PosterReportDeliveryBlueprintPackId = "poster-report-delivery-blueprints";

    public const string PosterReportDeliveryIndustryPackId = "poster-report-delivery-industry";

    public const string PosterReportDeliveryRendererPackId = "poster-report-delivery-renderer";

    public const string PosterReportDeliveryReviewRubricPackId = "poster-report-delivery-review-rubric";

    public static PackRegistry CreateGenericImageSeriesRegistry(
        string appVersion,
        DateTimeOffset createdAt)
    {
        var compatibility = PackCompatibilityRange.Create("1.0.0", "2.0.0");
        var blueprintPack = BlueprintPack.Create(
            GenericImageSeriesBlueprintPackId,
            "Generic Image Series Blueprints",
            "1.0.0",
            compatibility,
            ["single-image", "image-series", "panel-sequence"],
            PackLifecycleState.Active,
            [],
            createdAt);
        var industryPack = IndustryPack.Create(
            GenericImageSeriesIndustryPackId,
            "Generic Image Series Audience",
            "1.0.0",
            compatibility,
            ["teacher", "creator"],
            [GenericImageSeriesWorkflowPackId],
            PackLifecycleState.Active,
            [],
            createdAt);
        var rendererPack = RendererPack.Create(
            GenericImageSeriesRendererPackId,
            "Generic Image Series Renderer",
            "1.0.0",
            compatibility,
            ["png", "webp"],
            PackLifecycleState.Active,
            [],
            createdAt);
        var reviewRubricPack = ReviewRubricPack.Create(
            GenericImageSeriesReviewRubricPackId,
            "Generic Image Series Review Rubric",
            "1.0.0",
            compatibility,
            ["general-image", "series-consistency"],
            PackLifecycleState.Active,
            [],
            createdAt);
        var workflowPack = WorkflowPack.Create(
            GenericImageSeriesWorkflowPackId,
            "Generic Image Series",
            "1.0.0",
            compatibility,
            ["Source", "Brief", "Plan", "Produce", "Review", "Repair", "Deliver"],
            [GenericImageSeriesBlueprintPackId],
            PackLifecycleState.Active,
            [],
            createdAt,
            scenarioIds: [GenericImageSeriesWorkflowPackId],
            industryPackIds: [GenericImageSeriesIndustryPackId],
            rendererPackIds: [GenericImageSeriesRendererPackId],
            reviewRubricPackIds: [GenericImageSeriesReviewRubricPackId]);

        return PackRegistry.Create(appVersion, [workflowPack, blueprintPack, industryPack, rendererPack, reviewRubricPack]);
    }

    public static PackRegistry CreateStarterPackRegistry(
        string appVersion,
        DateTimeOffset createdAt)
    {
        var compatibility = PackCompatibilityRange.Create("1.0.0", "2.0.0");
        var genericBlueprint = CreateBlueprintPack(
            GenericImageSeriesBlueprintPackId,
            "Generic Image Series Blueprints",
            ["single-image", "image-series", "panel-sequence"],
            compatibility,
            createdAt);
        var genericWorkflow = CreateWorkflowPack(
            GenericImageSeriesWorkflowPackId,
            "Generic Image Series",
            [GenericImageSeriesBlueprintPackId],
            compatibility,
            createdAt);
        var genericIndustry = IndustryPack.Create(
            GenericImageSeriesIndustryPackId,
            "Generic Image Series Audience",
            "1.0.0",
            compatibility,
            ["teacher", "creator"],
            [GenericImageSeriesWorkflowPackId],
            PackLifecycleState.Active,
            [],
            createdAt);
        var genericRenderer = RendererPack.Create(
            GenericImageSeriesRendererPackId,
            "Generic Image Series Renderer",
            "1.0.0",
            compatibility,
            ["png", "webp"],
            PackLifecycleState.Active,
            [],
            createdAt);
        var genericReviewRubric = ReviewRubricPack.Create(
            GenericImageSeriesReviewRubricPackId,
            "Generic Image Series Review Rubric",
            "1.0.0",
            compatibility,
            ["general-image", "series-consistency"],
            PackLifecycleState.Active,
            [],
            createdAt);
        var articleBlueprint = CreateBlueprintPack(
            ArticleIllustrationBlueprintPackId,
            "Article Illustration Blueprints",
            ["article-inline-illustration", "article-cover", "concept-explainer"],
            compatibility,
            createdAt);
        var articleIndustry = IndustryPack.Create(
            ArticleIllustrationIndustryPackId,
            "Article Illustration Audience",
            "1.0.0",
            compatibility,
            ["editorial", "creator"],
            [ArticleIllustrationWorkflowPackId],
            PackLifecycleState.Active,
            [],
            createdAt);
        var articleRenderer = RendererPack.Create(
            ArticleIllustrationRendererPackId,
            "Article Illustration Renderer",
            "1.0.0",
            compatibility,
            ["png", "jpg", "webp"],
            PackLifecycleState.Active,
            [],
            createdAt);
        var articleReviewRubric = ReviewRubricPack.Create(
            ArticleIllustrationReviewRubricPackId,
            "Article Illustration Review Rubric",
            "1.0.0",
            compatibility,
            ["editorial-illustration"],
            PackLifecycleState.Active,
            [],
            createdAt);
        var articleWorkflow = CreateWorkflowPack(
            ArticleIllustrationWorkflowPackId,
            "Article Illustration",
            [ArticleIllustrationBlueprintPackId],
            compatibility,
            createdAt);
        var documentBlueprint = CreateBlueprintPack(
            DocumentReviewTranslationBlueprintPackId,
            "Document Review Translation Blueprints",
            ["translation-review-report", "paper-review-summary", "latex-formula-cleanup"],
            compatibility,
            createdAt);
        var documentIndustry = IndustryPack.Create(
            DocumentReviewTranslationIndustryPackId,
            "Document Review Translation Audience",
            "1.0.0",
            compatibility,
            ["research", "translation"],
            [DocumentReviewTranslationWorkflowPackId],
            PackLifecycleState.Active,
            [],
            createdAt);
        var documentRenderer = RendererPack.Create(
            DocumentReviewTranslationRendererPackId,
            "Document Review Translation Renderer",
            "1.0.0",
            compatibility,
            ["markdown", "docx", "pdf"],
            PackLifecycleState.Active,
            [],
            createdAt);
        var documentReviewRubric = ReviewRubricPack.Create(
            DocumentReviewTranslationReviewRubricPackId,
            "Document Review Translation Review Rubric",
            "1.0.0",
            compatibility,
            [ReviewRubricTemplateCatalog.ScholarlySchematic, ReviewRubricTemplateCatalog.EditorialIllustration],
            PackLifecycleState.Active,
            [],
            createdAt);
        var documentWorkflow = CreateWorkflowPack(
            DocumentReviewTranslationWorkflowPackId,
            "Document Review Translation",
            [DocumentReviewTranslationBlueprintPackId],
            compatibility,
            createdAt);
        var coursewareBlueprint = CreateBlueprintPack(
            CoursewareVisualBlueprintPackId,
            "Courseware Visual Blueprints",
            ["lesson-slide-visual", "classroom-diagram", "worksheet-visual"],
            compatibility,
            createdAt);
        var coursewareIndustry = IndustryPack.Create(
            CoursewareVisualIndustryPackId,
            "Courseware Visual Audience",
            "1.0.0",
            compatibility,
            ["teacher", "courseware"],
            [CoursewareVisualWorkflowPackId],
            PackLifecycleState.Active,
            [],
            createdAt);
        var coursewareRenderer = RendererPack.Create(
            CoursewareVisualRendererPackId,
            "Courseware Visual Renderer",
            "1.0.0",
            compatibility,
            ["png", "pptx", "pdf"],
            PackLifecycleState.Active,
            [],
            createdAt);
        var coursewareReviewRubric = ReviewRubricPack.Create(
            CoursewareVisualReviewRubricPackId,
            "Courseware Visual Review Rubric",
            "1.0.0",
            compatibility,
            [ReviewRubricTemplateCatalog.EducationalAccuracy, ReviewRubricTemplateCatalog.TextHeavyPoster],
            PackLifecycleState.Active,
            [],
            createdAt);
        var coursewareWorkflow = CreateWorkflowPack(
            CoursewareVisualWorkflowPackId,
            "Courseware Visual",
            [CoursewareVisualBlueprintPackId],
            compatibility,
            createdAt);
        var posterBlueprint = CreateBlueprintPack(
            PosterReportDeliveryBlueprintPackId,
            "Poster Report Delivery Blueprints",
            ["poster-and-report-package", "infographic-report", "review-backed-delivery"],
            compatibility,
            createdAt);
        var posterIndustry = IndustryPack.Create(
            PosterReportDeliveryIndustryPackId,
            "Poster Report Delivery Audience",
            "1.0.0",
            compatibility,
            ["creator", "delivery"],
            [PosterReportDeliveryWorkflowPackId],
            PackLifecycleState.Active,
            [],
            createdAt);
        var posterRenderer = RendererPack.Create(
            PosterReportDeliveryRendererPackId,
            "Poster Report Delivery Renderer",
            "1.0.0",
            compatibility,
            ["png", "pdf", "zip"],
            PackLifecycleState.Active,
            [],
            createdAt);
        var posterReviewRubric = ReviewRubricPack.Create(
            PosterReportDeliveryReviewRubricPackId,
            "Poster Report Delivery Review Rubric",
            "1.0.0",
            compatibility,
            [ReviewRubricTemplateCatalog.TextHeavyPoster, ReviewRubricTemplateCatalog.GeneralImage],
            PackLifecycleState.Active,
            [],
            createdAt);
        var posterWorkflow = CreateWorkflowPack(
            PosterReportDeliveryWorkflowPackId,
            "Poster Report Delivery",
            [PosterReportDeliveryBlueprintPackId],
            compatibility,
            createdAt);

        return PackRegistry.Create(
            appVersion,
            [
                genericWorkflow,
                genericBlueprint,
                genericIndustry,
                genericRenderer,
                genericReviewRubric,
                articleWorkflow,
                articleBlueprint,
                articleIndustry,
                articleRenderer,
                articleReviewRubric,
                documentWorkflow,
                documentBlueprint,
                documentIndustry,
                documentRenderer,
                documentReviewRubric,
                coursewareWorkflow,
                coursewareBlueprint,
                coursewareIndustry,
                coursewareRenderer,
                coursewareReviewRubric,
                posterWorkflow,
                posterBlueprint,
                posterIndustry,
                posterRenderer,
                posterReviewRubric,
            ]);
    }

    private static WorkflowPack CreateWorkflowPack(
        string id,
        string displayName,
        IReadOnlyList<string> blueprintPackIds,
        PackCompatibilityRange compatibility,
        DateTimeOffset createdAt)
    {
        return WorkflowPack.Create(
            id,
            displayName,
            "1.0.0",
            compatibility,
            ["Source", "Brief", "Plan", "Produce", "Review", "Repair", "Deliver"],
            blueprintPackIds,
            PackLifecycleState.Active,
            [],
            createdAt,
            scenarioIds: [id],
            industryPackIds: id switch
            {
                GenericImageSeriesWorkflowPackId => [GenericImageSeriesIndustryPackId],
                ArticleIllustrationWorkflowPackId => [ArticleIllustrationIndustryPackId],
                DocumentReviewTranslationWorkflowPackId => [DocumentReviewTranslationIndustryPackId],
                CoursewareVisualWorkflowPackId => [CoursewareVisualIndustryPackId],
                PosterReportDeliveryWorkflowPackId => [PosterReportDeliveryIndustryPackId],
                _ => [],
            },
            rendererPackIds: id switch
            {
                GenericImageSeriesWorkflowPackId => [GenericImageSeriesRendererPackId],
                ArticleIllustrationWorkflowPackId => [ArticleIllustrationRendererPackId],
                DocumentReviewTranslationWorkflowPackId => [DocumentReviewTranslationRendererPackId],
                CoursewareVisualWorkflowPackId => [CoursewareVisualRendererPackId],
                PosterReportDeliveryWorkflowPackId => [PosterReportDeliveryRendererPackId],
                _ => [],
            },
            reviewRubricPackIds: id switch
            {
                GenericImageSeriesWorkflowPackId => [GenericImageSeriesReviewRubricPackId],
                ArticleIllustrationWorkflowPackId => [ArticleIllustrationReviewRubricPackId],
                DocumentReviewTranslationWorkflowPackId => [DocumentReviewTranslationReviewRubricPackId],
                CoursewareVisualWorkflowPackId => [CoursewareVisualReviewRubricPackId],
                PosterReportDeliveryWorkflowPackId => [PosterReportDeliveryReviewRubricPackId],
                _ => [],
            });
    }

    private static BlueprintPack CreateBlueprintPack(
        string id,
        string displayName,
        IReadOnlyList<string> blueprintIds,
        PackCompatibilityRange compatibility,
        DateTimeOffset createdAt)
    {
        return BlueprintPack.Create(
            id,
            displayName,
            "1.0.0",
            compatibility,
            blueprintIds,
            PackLifecycleState.Active,
            [],
            createdAt);
    }
}
