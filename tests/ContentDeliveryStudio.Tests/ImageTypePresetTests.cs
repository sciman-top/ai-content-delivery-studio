using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Styles;

namespace ContentDeliveryStudio.Tests;

public sealed class ImageTypePresetTests
{
    [Fact]
    public void Catalog_IncludesEducationalPosterWithDeterministicTextPolicy()
    {
        var preset = ImageTypePresetCatalog.GetById(ImageTypePresetCatalog.EducationalPoster);

        Assert.Equal("educational-poster", preset.Id);
        Assert.Equal("Educational poster", preset.DisplayName);
        Assert.Equal(new AspectRatio(4, 5), preset.DefaultAspectRatio);
        Assert.Equal("png", preset.DefaultOutputFormat);
        Assert.Equal(ImageTextPolicy.DeterministicPostRender, preset.TextPolicy);
        Assert.Equal(ReviewRubricTemplateCatalog.TextHeavyPoster, preset.ReviewRubricTemplateId);
        Assert.Equal("{series}/{item-number}-{item-slug}", preset.DeliveryNamingPolicy);
    }

    [Fact]
    public void Catalog_ReturnsPresetsInStableDisplayOrder()
    {
        var presets = ImageTypePresetCatalog.Defaults;

        Assert.Collection(
            presets,
            preset => Assert.Equal(ImageTypePresetCatalog.EducationalPoster, preset.Id),
            preset => Assert.Equal(ImageTypePresetCatalog.ArticleCover, preset.Id),
            preset => Assert.Equal(ImageTypePresetCatalog.ArticleInlineIllustration, preset.Id),
            preset => Assert.Equal(ImageTypePresetCatalog.ConceptDiagram, preset.Id),
            preset => Assert.Equal(ImageTypePresetCatalog.GraphicalAbstract, preset.Id),
            preset => Assert.Equal(ImageTypePresetCatalog.ScholarlySchematic, preset.Id),
            preset => Assert.Equal(ImageTypePresetCatalog.SocialSquare, preset.Id),
            preset => Assert.Equal(ImageTypePresetCatalog.BackgroundPlate, preset.Id));
    }

    [Fact]
    public void Catalog_IncludesDocumentIllustrationPresets()
    {
        var presets = ImageTypePresetCatalog.Defaults.Select(preset => preset.Id).ToArray();

        Assert.Contains(ImageTypePresetCatalog.ArticleInlineIllustration, presets);
        Assert.Contains(ImageTypePresetCatalog.ConceptDiagram, presets);
        Assert.Contains(ImageTypePresetCatalog.GraphicalAbstract, presets);
        Assert.Contains(ImageTypePresetCatalog.ScholarlySchematic, presets);

        var scholarly = ImageTypePresetCatalog.GetById(ImageTypePresetCatalog.ScholarlySchematic);
        Assert.Equal(ImageTextPolicy.DeterministicPostRender, scholarly.TextPolicy);
        Assert.Equal(ReviewRubricTemplateCatalog.ScholarlySchematic, scholarly.ReviewRubricTemplateId);
    }

    [Fact]
    public void Catalog_ContainsGovernanceMetadataForEveryPreset()
    {
        foreach (var preset in ImageTypePresetCatalog.Defaults)
        {
            Assert.Equal(ImageTypePresetCatalog.CatalogVersion, preset.CatalogVersion);
            Assert.False(string.IsNullOrWhiteSpace(preset.DeliveryFamily));
            Assert.Contains(preset.DefaultAspectRatio, preset.SupportedAspectRatios);
            Assert.False(string.IsNullOrWhiteSpace(preset.DefaultQualityBand));
            Assert.NotEmpty(preset.WorkflowModes);
            Assert.NotEmpty(preset.StyleDimensionHints);
            Assert.NotEmpty(preset.RequiredBriefFields);
            Assert.NotEmpty(preset.CommonFailureModes);
            Assert.NotEmpty(preset.CapabilityRequirements);
            Assert.All(preset.WorkflowModes, value => Assert.False(string.IsNullOrWhiteSpace(value)));
            Assert.All(preset.StyleDimensionHints, value => Assert.False(string.IsNullOrWhiteSpace(value)));
            Assert.All(preset.RequiredBriefFields, value => Assert.False(string.IsNullOrWhiteSpace(value)));
            Assert.All(preset.CommonFailureModes, value => Assert.False(string.IsNullOrWhiteSpace(value)));
            Assert.All(preset.CapabilityRequirements, value => Assert.False(string.IsNullOrWhiteSpace(value)));
            _ = ReviewRubricTemplateCatalog.GetById(preset.ReviewRubricTemplateId);
        }
    }

    [Fact]
    public void Catalog_TextHeavyPresetsUseDeterministicTextPolicy()
    {
        var textHeavyPresetIds = new[]
        {
            ImageTypePresetCatalog.EducationalPoster,
            ImageTypePresetCatalog.ConceptDiagram,
            ImageTypePresetCatalog.GraphicalAbstract,
            ImageTypePresetCatalog.ScholarlySchematic,
            ImageTypePresetCatalog.BackgroundPlate,
        };

        foreach (var presetId in textHeavyPresetIds)
        {
            var preset = ImageTypePresetCatalog.GetById(presetId);

            Assert.Equal(ImageTextPolicy.DeterministicPostRender, preset.TextPolicy);
            Assert.Contains(preset.CapabilityRequirements, value =>
                value.Contains("deterministic", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Create_RejectsInvalidPresetData()
    {
        Assert.Throws<ArgumentException>(() =>
            CreatePresetForValidation(
                " ",
                "Poster",
                "Description",
                new AspectRatio(4, 5)));

        Assert.Throws<ArgumentException>(() => new AspectRatio(0, 5));

        Assert.Throws<ArgumentException>(() =>
            CreatePresetForValidation(
                "custom",
                "Custom",
                "Description",
                new AspectRatio(1, 1),
                outputFormat: " "));
    }

    private static ImageTypePreset CreatePresetForValidation(
        string id,
        string displayName,
        string description,
        AspectRatio defaultAspectRatio,
        string outputFormat = "png")
    {
        return ImageTypePreset.Create(
            id,
            displayName,
            description,
            defaultAspectRatio,
            outputFormat,
            ImageTextPolicy.Hybrid,
            ReviewRubricTemplateCatalog.GeneralImage,
            "{item-slug}",
            ImageTypePresetCatalog.CatalogVersion,
            "validation",
            [defaultAspectRatio],
            ImageBackgroundMode.Auto,
            "draft",
            ["text-to-image"],
            ["validation"],
            ["goal"],
            ["bad validation data"],
            ["provider size support"]);
    }
}
