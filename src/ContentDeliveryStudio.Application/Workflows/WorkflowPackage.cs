using ContentDeliveryStudio.Core.Experiments;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.References;
using ContentDeliveryStudio.Core.Styles;

namespace ContentDeliveryStudio.Application.Workflows;

public sealed record WorkflowPackage(
    string SchemaVersion,
    string Name,
    DateTimeOffset ExportedAt,
    IReadOnlyList<WorkflowStyleGuide> StyleGuides,
    IReadOnlyList<WorkflowGenerationRecipe> GenerationRecipes,
    IReadOnlyList<WorkflowReferenceImageSet> ReferenceImageSets,
    IReadOnlyList<WorkflowParameterExperimentDefinition> ParameterExperiments)
{
    public const string CurrentSchemaVersion = "workflow-package.v1";

    public static WorkflowPackage Create(
        string name,
        DateTimeOffset exportedAt,
        IReadOnlyList<WorkflowStyleGuide> styleGuides,
        IReadOnlyList<WorkflowGenerationRecipe> generationRecipes,
        IReadOnlyList<WorkflowReferenceImageSet> referenceImageSets,
        IReadOnlyList<WorkflowParameterExperimentDefinition> parameterExperiments)
    {
        ArgumentNullException.ThrowIfNull(styleGuides);
        ArgumentNullException.ThrowIfNull(generationRecipes);
        ArgumentNullException.ThrowIfNull(referenceImageSets);
        ArgumentNullException.ThrowIfNull(parameterExperiments);

        return new WorkflowPackage(
            CurrentSchemaVersion,
            RequireText(name, nameof(name)),
            exportedAt,
            styleGuides.Select(WorkflowStyleGuide.Normalize).ToArray(),
            generationRecipes.Select(WorkflowGenerationRecipe.Normalize).ToArray(),
            referenceImageSets.Select(WorkflowReferenceImageSet.Normalize).ToArray(),
            parameterExperiments.Select(WorkflowParameterExperimentDefinition.Normalize).ToArray());
    }

    internal static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    internal static IReadOnlyList<string> NormalizeRequiredTextList(IReadOnlyList<string> values, string parameterName)
    {
        var normalized = NormalizeOptionalTextList(values);
        if (normalized.Count == 0)
        {
            throw new ArgumentException("At least one value is required.", parameterName);
        }

        return normalized;
    }

    internal static IReadOnlyList<string> NormalizeOptionalTextList(IReadOnlyList<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return values
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

public sealed record WorkflowStyleGuide(
    Guid Id,
    Guid SeriesId,
    string Name,
    IReadOnlyList<string> VisualPrinciples,
    IReadOnlyList<string> Palette,
    IReadOnlyList<string> Lighting,
    IReadOnlyList<string> CompositionRules,
    IReadOnlyList<string> LineOrTextureRules,
    IReadOnlyList<string> NegativeConstraints,
    IReadOnlyList<Guid> ReferenceImageSetIds,
    int Version)
{
    public static WorkflowStyleGuide FromStyleGuide(StyleGuide styleGuide)
    {
        ArgumentNullException.ThrowIfNull(styleGuide);

        return Normalize(new WorkflowStyleGuide(
            styleGuide.Id,
            styleGuide.SeriesId,
            styleGuide.Name,
            styleGuide.VisualPrinciples,
            styleGuide.Palette,
            styleGuide.Lighting,
            styleGuide.CompositionRules,
            styleGuide.LineOrTextureRules,
            styleGuide.NegativeConstraints,
            styleGuide.ReferenceImageSetIds,
            styleGuide.Version));
    }

    public static WorkflowStyleGuide Normalize(WorkflowStyleGuide value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Id == Guid.Empty)
        {
            throw new ArgumentException("Style guide id cannot be empty.", nameof(value));
        }

        if (value.SeriesId == Guid.Empty)
        {
            throw new ArgumentException("Style guide series id cannot be empty.", nameof(value));
        }

        if (value.Version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Style guide version must be greater than zero.");
        }

        return value with
        {
            Name = WorkflowPackage.RequireText(value.Name, nameof(value.Name)),
            VisualPrinciples = WorkflowPackage.NormalizeRequiredTextList(value.VisualPrinciples, nameof(value.VisualPrinciples)),
            Palette = WorkflowPackage.NormalizeOptionalTextList(value.Palette),
            Lighting = WorkflowPackage.NormalizeOptionalTextList(value.Lighting),
            CompositionRules = WorkflowPackage.NormalizeOptionalTextList(value.CompositionRules),
            LineOrTextureRules = WorkflowPackage.NormalizeOptionalTextList(value.LineOrTextureRules),
            NegativeConstraints = WorkflowPackage.NormalizeOptionalTextList(value.NegativeConstraints),
            ReferenceImageSetIds = value.ReferenceImageSetIds
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToArray(),
        };
    }
}

public sealed record WorkflowGenerationRecipe(
    Guid Id,
    Guid ProviderProfileId,
    string ModelId,
    string ImageTypePresetId,
    int Width,
    int Height,
    string Quality,
    string OutputFormat,
    ImageBackgroundMode Background,
    int? Compression,
    ImageModerationMode Moderation,
    int? Seed,
    IReadOnlyList<string> CapabilityWarnings)
{
    public static WorkflowGenerationRecipe FromGenerationRecipe(GenerationRecipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        return Normalize(new WorkflowGenerationRecipe(
            recipe.Id,
            recipe.ProviderProfileId,
            recipe.ModelId,
            recipe.ImageTypePresetId,
            recipe.Width,
            recipe.Height,
            recipe.Quality,
            recipe.OutputFormat,
            recipe.Background,
            recipe.Compression,
            recipe.Moderation,
            recipe.Seed,
            recipe.CapabilityWarnings));
    }

    public static WorkflowGenerationRecipe Normalize(WorkflowGenerationRecipe value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _ = ImageTypePresetCatalog.GetById(WorkflowPackage.RequireText(value.ImageTypePresetId, nameof(value.ImageTypePresetId)));

        if (value.Id == Guid.Empty)
        {
            throw new ArgumentException("Generation recipe id cannot be empty.", nameof(value));
        }

        if (value.ProviderProfileId == Guid.Empty)
        {
            throw new ArgumentException("Provider profile id cannot be empty.", nameof(value));
        }

        if (value.Width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Recipe width must be greater than zero.");
        }

        if (value.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Recipe height must be greater than zero.");
        }

        if (value.Compression is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Compression must be between 0 and 100.");
        }

        return value with
        {
            ModelId = WorkflowPackage.RequireText(value.ModelId, nameof(value.ModelId)),
            ImageTypePresetId = value.ImageTypePresetId.Trim(),
            Quality = WorkflowPackage.RequireText(value.Quality, nameof(value.Quality)).ToLowerInvariant(),
            OutputFormat = WorkflowPackage.RequireText(value.OutputFormat, nameof(value.OutputFormat)).ToLowerInvariant(),
            CapabilityWarnings = WorkflowPackage.NormalizeOptionalTextList(value.CapabilityWarnings),
        };
    }
}

public sealed record WorkflowReferenceImageSet(
    Guid Id,
    string Name,
    IReadOnlyList<WorkflowReferenceImage> Images)
{
    public static WorkflowReferenceImageSet FromReferenceImageSet(ReferenceImageSet referenceImageSet)
    {
        ArgumentNullException.ThrowIfNull(referenceImageSet);

        return Normalize(new WorkflowReferenceImageSet(
            referenceImageSet.Id,
            referenceImageSet.Name,
            referenceImageSet.Images
                .Select(image => new WorkflowReferenceImage(
                    image.AssetPath,
                    image.Role,
                    image.Description))
                .ToArray()));
    }

    public static WorkflowReferenceImageSet Normalize(WorkflowReferenceImageSet value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Id == Guid.Empty)
        {
            throw new ArgumentException("Reference image set id cannot be empty.", nameof(value));
        }

        return value with
        {
            Name = WorkflowPackage.RequireText(value.Name, nameof(value.Name)),
            Images = value.Images.Select(WorkflowReferenceImage.Normalize).ToArray(),
        };
    }
}

public sealed record WorkflowReferenceImage(
    string AssetPath,
    ReferenceImageRole Role,
    string Description)
{
    public static WorkflowReferenceImage Normalize(WorkflowReferenceImage value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value with
        {
            AssetPath = NormalizeWorkspaceRelativePath(value.AssetPath),
            Description = value.Description.Trim(),
        };
    }

    private static string NormalizeWorkspaceRelativePath(string assetPath)
    {
        var trimmed = WorkflowPackage.RequireText(assetPath, nameof(assetPath));
        if (Path.IsPathRooted(trimmed) || Uri.TryCreate(trimmed, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Reference image paths must be workspace-relative.", nameof(assetPath));
        }

        var segments = trimmed
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
        {
            throw new ArgumentException("Reference image paths cannot escape the workspace.", nameof(assetPath));
        }

        return string.Join("/", segments);
    }
}

public sealed record WorkflowParameterExperimentDefinition(
    string Name,
    string BasePrompt,
    GenerationSettings Settings,
    IReadOnlyList<ParameterGridAxis> Axes,
    Guid? GenerationRecipeId)
{
    public static WorkflowParameterExperimentDefinition Create(
        string name,
        string basePrompt,
        GenerationSettings settings,
        IReadOnlyList<ParameterGridAxis> axes,
        Guid? generationRecipeId)
    {
        return Normalize(new WorkflowParameterExperimentDefinition(
            name,
            basePrompt,
            settings,
            axes,
            generationRecipeId));
    }

    public static WorkflowParameterExperimentDefinition Normalize(WorkflowParameterExperimentDefinition value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(value.Settings);
        ArgumentNullException.ThrowIfNull(value.Axes);

        if (value.GenerationRecipeId == Guid.Empty)
        {
            throw new ArgumentException("Generation recipe id cannot be empty.", nameof(value));
        }

        var normalizedVariants = ParameterGridExperiment.CreateVariants(
            value.BasePrompt,
            value.Settings,
            value.Axes);

        return value with
        {
            Name = WorkflowPackage.RequireText(value.Name, nameof(value.Name)),
            BasePrompt = WorkflowPackage.RequireText(value.BasePrompt, nameof(value.BasePrompt)),
            Axes = normalizedVariants.First().Axes,
        };
    }
}
