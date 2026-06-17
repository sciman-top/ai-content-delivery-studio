using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.Core.Styles;

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
        string deliveryNamingPolicy,
        string catalogVersion,
        string deliveryFamily,
        IReadOnlyList<AspectRatio> supportedAspectRatios,
        ImageBackgroundMode defaultBackgroundMode,
        string defaultQualityBand,
        IReadOnlyList<string> workflowModes,
        IReadOnlyList<string> styleDimensionHints,
        IReadOnlyList<string> requiredBriefFields,
        IReadOnlyList<string> commonFailureModes,
        IReadOnlyList<string> capabilityRequirements,
        bool isDeprecated)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
        DefaultAspectRatio = defaultAspectRatio;
        DefaultOutputFormat = defaultOutputFormat;
        TextPolicy = textPolicy;
        ReviewRubricTemplateId = reviewRubricTemplateId;
        DeliveryNamingPolicy = deliveryNamingPolicy;
        CatalogVersion = catalogVersion;
        DeliveryFamily = deliveryFamily;
        SupportedAspectRatios = supportedAspectRatios;
        DefaultBackgroundMode = defaultBackgroundMode;
        DefaultQualityBand = defaultQualityBand;
        WorkflowModes = workflowModes;
        StyleDimensionHints = styleDimensionHints;
        RequiredBriefFields = requiredBriefFields;
        CommonFailureModes = commonFailureModes;
        CapabilityRequirements = capabilityRequirements;
        IsDeprecated = isDeprecated;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public AspectRatio DefaultAspectRatio { get; }

    public string DefaultOutputFormat { get; }

    public ImageTextPolicy TextPolicy { get; }

    public string ReviewRubricTemplateId { get; }

    public string DeliveryNamingPolicy { get; }

    public string CatalogVersion { get; }

    public string DeliveryFamily { get; }

    public IReadOnlyList<AspectRatio> SupportedAspectRatios { get; }

    public ImageBackgroundMode DefaultBackgroundMode { get; }

    public string DefaultQualityBand { get; }

    public IReadOnlyList<string> WorkflowModes { get; }

    public IReadOnlyList<string> StyleDimensionHints { get; }

    public IReadOnlyList<string> RequiredBriefFields { get; }

    public IReadOnlyList<string> CommonFailureModes { get; }

    public IReadOnlyList<string> CapabilityRequirements { get; }

    public bool IsDeprecated { get; }

    public static ImageTypePreset Create(
        string id,
        string displayName,
        string description,
        AspectRatio defaultAspectRatio,
        string defaultOutputFormat,
        ImageTextPolicy textPolicy,
        string reviewRubricTemplateId,
        string deliveryNamingPolicy,
        string catalogVersion,
        string deliveryFamily,
        IReadOnlyList<AspectRatio> supportedAspectRatios,
        ImageBackgroundMode defaultBackgroundMode,
        string defaultQualityBand,
        IReadOnlyList<string> workflowModes,
        IReadOnlyList<string> styleDimensionHints,
        IReadOnlyList<string> requiredBriefFields,
        IReadOnlyList<string> commonFailureModes,
        IReadOnlyList<string> capabilityRequirements,
        bool isDeprecated = false)
    {
        ArgumentNullException.ThrowIfNull(defaultAspectRatio);
        ArgumentNullException.ThrowIfNull(supportedAspectRatios);

        return new ImageTypePreset(
            RequireText(id, nameof(id)),
            RequireText(displayName, nameof(displayName)),
            description.Trim(),
            defaultAspectRatio,
            RequireText(defaultOutputFormat, nameof(defaultOutputFormat)).ToLowerInvariant(),
            textPolicy,
            RequireText(reviewRubricTemplateId, nameof(reviewRubricTemplateId)),
            RequireText(deliveryNamingPolicy, nameof(deliveryNamingPolicy)),
            RequireText(catalogVersion, nameof(catalogVersion)),
            RequireText(deliveryFamily, nameof(deliveryFamily)),
            NormalizeAspectRatios(supportedAspectRatios, defaultAspectRatio),
            defaultBackgroundMode,
            RequireText(defaultQualityBand, nameof(defaultQualityBand)).ToLowerInvariant(),
            NormalizeTextList(workflowModes, nameof(workflowModes)),
            NormalizeTextList(styleDimensionHints, nameof(styleDimensionHints)),
            NormalizeTextList(requiredBriefFields, nameof(requiredBriefFields)),
            NormalizeTextList(commonFailureModes, nameof(commonFailureModes)),
            NormalizeTextList(capabilityRequirements, nameof(capabilityRequirements)),
            isDeprecated);
    }

    private static IReadOnlyList<AspectRatio> NormalizeAspectRatios(
        IReadOnlyList<AspectRatio> supportedAspectRatios,
        AspectRatio defaultAspectRatio)
    {
        return supportedAspectRatios
            .Append(defaultAspectRatio)
            .Distinct()
            .ToArray();
    }

    private static IReadOnlyList<string> NormalizeTextList(
        IReadOnlyList<string> values,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values);

        var normalized = values
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one value is required.", parameterName);
        }

        return normalized;
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
    public const string CatalogVersion = "2026-06-02";

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
            "{series}/{item-number}-{item-slug}",
            CatalogVersion,
            "education",
            [new AspectRatio(4, 5), new AspectRatio(16, 9)],
            ImageBackgroundMode.Opaque,
            "draft",
            ["text-to-image", "background-plate"],
            ["educational", "diagram", "poster"],
            ["goal", "audience", "must_include", "text_policy"],
            ["unreadable small text", "crowded layout", "formula hallucination"],
            ["deterministic text composition", "provider size support"]),
        ImageTypePreset.Create(
            ArticleCover,
            "Article cover",
            "Editorial cover image for an article, essay, or newsletter.",
            new AspectRatio(16, 9),
            "png",
            ImageTextPolicy.Hybrid,
            ReviewRubricTemplateCatalog.GeneralImage,
            "{series}/cover-{item-slug}",
            CatalogVersion,
            "editorial",
            [new AspectRatio(16, 9), new AspectRatio(4, 5), new AspectRatio(1, 1)],
            ImageBackgroundMode.Auto,
            "draft",
            ["text-to-image"],
            ["editorial", "cover", "hero"],
            ["goal", "audience", "delivery_context"],
            ["weak first impression", "unsupported headline text"],
            ["provider size support", "hybrid text review"]),
        ImageTypePreset.Create(
            ArticleInlineIllustration,
            "Article inline illustration",
            "Inline article illustration tied to a specific section or claim.",
            new AspectRatio(16, 9),
            "png",
            ImageTextPolicy.Hybrid,
            ReviewRubricTemplateCatalog.EditorialIllustration,
            "{series}/inline-{item-number}-{item-slug}",
            CatalogVersion,
            "editorial",
            [new AspectRatio(16, 9), new AspectRatio(4, 3), new AspectRatio(1, 1)],
            ImageBackgroundMode.Auto,
            "draft",
            ["text-to-image", "reference-image"],
            ["editorial", "inline", "explanatory"],
            ["goal", "audience", "source_evidence"],
            ["adds unsupported claims", "poor section fit"],
            ["provider size support", "source evidence review"]),
        ImageTypePreset.Create(
            ConceptDiagram,
            "Concept diagram",
            "Educational concept diagram with clear structure and deterministic label support.",
            new AspectRatio(16, 9),
            "png",
            ImageTextPolicy.DeterministicPostRender,
            ReviewRubricTemplateCatalog.EducationalAccuracy,
            "{series}/concept-{item-number}-{item-slug}",
            CatalogVersion,
            "education",
            [new AspectRatio(16, 9), new AspectRatio(4, 3)],
            ImageBackgroundMode.Opaque,
            "draft",
            ["text-to-image", "background-plate"],
            ["conceptual", "diagram", "teaching"],
            ["goal", "audience", "must_include", "must_avoid"],
            ["incorrect concept relationship", "unreadable labels"],
            ["deterministic text composition", "concept accuracy review"]),
        ImageTypePreset.Create(
            GraphicalAbstract,
            "Graphical abstract",
            "Schematic graphical abstract for a scholarly or educational draft.",
            new AspectRatio(16, 9),
            "png",
            ImageTextPolicy.DeterministicPostRender,
            ReviewRubricTemplateCatalog.ScholarlySchematic,
            "{series}/graphical-abstract-{item-slug}",
            CatalogVersion,
            "scholarly",
            [new AspectRatio(16, 9), new AspectRatio(4, 3)],
            ImageBackgroundMode.Opaque,
            "draft",
            ["text-to-image", "background-plate"],
            ["schematic", "summary", "scholarly"],
            ["goal", "source_evidence", "must_avoid"],
            ["implies fake evidence", "overly decorative layout"],
            ["deterministic text composition", "scholarly evidence review"]),
        ImageTypePreset.Create(
            ScholarlySchematic,
            "Scholarly schematic",
            "Concept-level scholarly schematic that avoids fabricated evidence imagery.",
            new AspectRatio(16, 9),
            "png",
            ImageTextPolicy.DeterministicPostRender,
            ReviewRubricTemplateCatalog.ScholarlySchematic,
            "{series}/schematic-{item-number}-{item-slug}",
            CatalogVersion,
            "scholarly",
            [new AspectRatio(16, 9), new AspectRatio(4, 3)],
            ImageBackgroundMode.Opaque,
            "draft",
            ["text-to-image", "background-plate"],
            ["schematic", "concept-level", "evidence-safe"],
            ["goal", "source_evidence", "known_constraints"],
            ["fabricated experimental evidence", "ambiguous causal relationship"],
            ["deterministic text composition", "scholarly evidence review"]),
        ImageTypePreset.Create(
            SocialSquare,
            "Social media square",
            "Square image suitable for social feeds and compact previews.",
            new AspectRatio(1, 1),
            "png",
            ImageTextPolicy.Hybrid,
            ReviewRubricTemplateCatalog.GeneralImage,
            "{series}/social-{item-slug}",
            CatalogVersion,
            "social",
            [new AspectRatio(1, 1), new AspectRatio(4, 5), new AspectRatio(16, 9)],
            ImageBackgroundMode.Auto,
            "draft",
            ["text-to-image"],
            ["social", "compact", "high-contrast"],
            ["goal", "audience", "delivery_context"],
            ["poor thumbnail readability", "too much small text"],
            ["provider size support", "hybrid text review"]),
        ImageTypePreset.Create(
            BackgroundPlate,
            "Background plate",
            "Clean visual background intended for deterministic text, labels, or layout overlays.",
            new AspectRatio(16, 9),
            "png",
            ImageTextPolicy.DeterministicPostRender,
            ReviewRubricTemplateCatalog.TextHeavyPoster,
            "{series}/plate-{item-slug}",
            CatalogVersion,
            "layout",
            [new AspectRatio(16, 9), new AspectRatio(4, 5), new AspectRatio(1, 1)],
            ImageBackgroundMode.Opaque,
            "draft",
            ["background-plate", "text-to-image"],
            ["background", "clean-space", "layout-safe"],
            ["goal", "text_policy", "layout_constraints"],
            ["insufficient text space", "visual clutter"],
            ["deterministic text composition", "layout review"]),
    ];

    public static IReadOnlyList<ImageTypePreset> Defaults => Presets;

    public static ImageTypePreset GetById(string id)
    {
        return Presets.SingleOrDefault(preset => preset.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Image type preset not found: {id}");
    }
}
