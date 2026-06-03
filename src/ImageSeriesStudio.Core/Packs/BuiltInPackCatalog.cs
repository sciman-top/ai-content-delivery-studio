namespace ImageSeriesStudio.Core.Packs;

public static class BuiltInPackCatalog
{
    public const string GenericImageSeriesWorkflowPackId = "generic-image-series";

    public const string GenericImageSeriesBlueprintPackId = "generic-image-series-blueprints";

    public const string ArticleIllustrationWorkflowPackId = "article-illustration";

    public const string ArticleIllustrationBlueprintPackId = "article-illustration-blueprints";

    public const string DocumentReviewTranslationWorkflowPackId = "document-review-translation";

    public const string DocumentReviewTranslationBlueprintPackId = "document-review-translation-blueprints";

    public const string CoursewareVisualWorkflowPackId = "courseware-visual";

    public const string CoursewareVisualBlueprintPackId = "courseware-visual-blueprints";

    public const string PosterReportDeliveryWorkflowPackId = "poster-report-delivery";

    public const string PosterReportDeliveryBlueprintPackId = "poster-report-delivery-blueprints";

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
        var workflowPack = WorkflowPack.Create(
            GenericImageSeriesWorkflowPackId,
            "Generic Image Series",
            "1.0.0",
            compatibility,
            ["Source", "Brief", "Plan", "Produce", "Review", "Repair", "Deliver"],
            [GenericImageSeriesBlueprintPackId],
            PackLifecycleState.Active,
            [],
            createdAt);

        return PackRegistry.Create(appVersion, [workflowPack, blueprintPack]);
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
        var articleBlueprint = CreateBlueprintPack(
            ArticleIllustrationBlueprintPackId,
            "Article Illustration Blueprints",
            ["article-inline-illustration", "article-cover", "concept-explainer"],
            compatibility,
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
                articleWorkflow,
                articleBlueprint,
                documentWorkflow,
                documentBlueprint,
                coursewareWorkflow,
                coursewareBlueprint,
                posterWorkflow,
                posterBlueprint,
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
            createdAt);
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
