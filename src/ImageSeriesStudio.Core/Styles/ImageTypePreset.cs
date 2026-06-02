using ImageSeriesStudio.Core.Projects;

namespace ImageSeriesStudio.Core.Styles;

public sealed record AspectRatio
{
    public AspectRatio(int widthUnits, int heightUnits)
    {
        if (widthUnits <= 0)
        {
            throw new ArgumentException("Width units must be greater than zero.", nameof(widthUnits));
        }

        if (heightUnits <= 0)
        {
            throw new ArgumentException("Height units must be greater than zero.", nameof(heightUnits));
        }

        WidthUnits = widthUnits;
        HeightUnits = heightUnits;
    }

    public int WidthUnits { get; }

    public int HeightUnits { get; }

    public override string ToString()
    {
        return $"{WidthUnits}:{HeightUnits}";
    }
}

public enum ImageTextPolicy
{
    ImageModelOnly = 0,
    DeterministicPostRender = 1,
    Hybrid = 2,
}

public sealed record ImageTypePreset
{
    private ImageTypePreset(
        string id,
        string displayName,
        string description,
        AspectRatio defaultAspectRatio,
        string defaultOutputFormat,
        ImageTextPolicy textPolicy,
        string reviewRubricTemplateId,
        string deliveryNamingPolicy)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
        DefaultAspectRatio = defaultAspectRatio;
        DefaultOutputFormat = defaultOutputFormat;
        TextPolicy = textPolicy;
        ReviewRubricTemplateId = reviewRubricTemplateId;
        DeliveryNamingPolicy = deliveryNamingPolicy;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public AspectRatio DefaultAspectRatio { get; }

    public string DefaultOutputFormat { get; }

    public ImageTextPolicy TextPolicy { get; }

    public string ReviewRubricTemplateId { get; }

    public string DeliveryNamingPolicy { get; }

    public static ImageTypePreset Create(
        string id,
        string displayName,
        string description,
        AspectRatio defaultAspectRatio,
        string defaultOutputFormat,
        ImageTextPolicy textPolicy,
        string reviewRubricTemplateId,
        string deliveryNamingPolicy)
    {
        ArgumentNullException.ThrowIfNull(defaultAspectRatio);

        return new ImageTypePreset(
            RequireText(id, nameof(id)),
            RequireText(displayName, nameof(displayName)),
            description.Trim(),
            defaultAspectRatio,
            RequireText(defaultOutputFormat, nameof(defaultOutputFormat)).ToLowerInvariant(),
            textPolicy,
            RequireText(reviewRubricTemplateId, nameof(reviewRubricTemplateId)),
            RequireText(deliveryNamingPolicy, nameof(deliveryNamingPolicy)));
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }
}

public static class ImageTypePresetCatalog
{
    public const string EducationalPoster = "educational-poster";

    public const string ArticleCover = "article-cover";

    public const string ArticleInlineIllustration = "article-inline-illustration";

    public const string ConceptDiagram = "concept-diagram";

    public const string GraphicalAbstract = "graphical-abstract";

    public const string ScholarlySchematic = "scholarly-schematic";

    public const string SocialSquare = "social-square";

    public const string BackgroundPlate = "background-plate";

    private static readonly IReadOnlyList<ImageTypePreset> Presets =
    [
        ImageTypePreset.Create(
            EducationalPoster,
            "Educational poster",
            "Text-heavy educational poster or infographic with deterministic final text composition.",
            new AspectRatio(4, 5),
            "png",
            ImageTextPolicy.DeterministicPostRender,
            ReviewRubricTemplateCatalog.TextHeavyPoster,
            "{series}/{item-number}-{item-slug}"),
        ImageTypePreset.Create(
            ArticleCover,
            "Article cover",
            "Editorial cover image for an article, essay, or newsletter.",
            new AspectRatio(16, 9),
            "png",
            ImageTextPolicy.Hybrid,
            ReviewRubricTemplateCatalog.GeneralImage,
            "{series}/cover-{item-slug}"),
        ImageTypePreset.Create(
            ArticleInlineIllustration,
            "Article inline illustration",
            "Inline document illustration that clarifies a section, argument, or example within an article.",
            new AspectRatio(16, 9),
            "png",
            ImageTextPolicy.Hybrid,
            ReviewRubricTemplateCatalog.EditorialIllustration,
            "{series}/inline-{item-number}-{item-slug}"),
        ImageTypePreset.Create(
            ConceptDiagram,
            "Concept diagram",
            "Educational concept diagram with deterministic final labels and explanatory text.",
            new AspectRatio(16, 9),
            "png",
            ImageTextPolicy.DeterministicPostRender,
            ReviewRubricTemplateCatalog.EducationalAccuracy,
            "{series}/concept-{item-number}-{item-slug}"),
        ImageTypePreset.Create(
            GraphicalAbstract,
            "Graphical abstract",
            "Research-style graphical abstract summarizing evidence-backed ideas for a document or paper.",
            new AspectRatio(16, 9),
            "png",
            ImageTextPolicy.DeterministicPostRender,
            ReviewRubricTemplateCatalog.ScholarlySchematic,
            "{series}/graphical-abstract-{item-slug}"),
        ImageTypePreset.Create(
            ScholarlySchematic,
            "Scholarly schematic",
            "Evidence-grounded scholarly schematic with deterministic labels, flows, and callouts.",
            new AspectRatio(16, 9),
            "png",
            ImageTextPolicy.DeterministicPostRender,
            ReviewRubricTemplateCatalog.ScholarlySchematic,
            "{series}/schematic-{item-number}-{item-slug}"),
        ImageTypePreset.Create(
            SocialSquare,
            "Social media square",
            "Square image suitable for social feeds and compact previews.",
            new AspectRatio(1, 1),
            "png",
            ImageTextPolicy.Hybrid,
            ReviewRubricTemplateCatalog.GeneralImage,
            "{series}/social-{item-slug}"),
        ImageTypePreset.Create(
            BackgroundPlate,
            "Background plate",
            "Clean visual background intended for deterministic text, labels, or layout overlays.",
            new AspectRatio(16, 9),
            "png",
            ImageTextPolicy.DeterministicPostRender,
            ReviewRubricTemplateCatalog.TextHeavyPoster,
            "{series}/plate-{item-slug}"),
    ];

    public static IReadOnlyList<ImageTypePreset> Defaults => Presets;

    public static ImageTypePreset GetById(string id)
    {
        return Presets.SingleOrDefault(preset => preset.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Image type preset not found: {id}");
    }
}
