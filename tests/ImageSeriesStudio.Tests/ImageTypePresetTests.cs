using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Styles;

namespace ImageSeriesStudio.Tests;

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
    public void Create_RejectsInvalidPresetData()
    {
        Assert.Throws<ArgumentException>(() =>
            ImageTypePreset.Create(
                " ",
                "Poster",
                "Description",
                new AspectRatio(4, 5),
                "png",
                ImageTextPolicy.Hybrid,
                ReviewRubricTemplateCatalog.GeneralImage,
                "{item-slug}"));

        Assert.Throws<ArgumentException>(() => new AspectRatio(0, 5));

        Assert.Throws<ArgumentException>(() =>
            ImageTypePreset.Create(
                "custom",
                "Custom",
                "Description",
                new AspectRatio(1, 1),
                " ",
                ImageTextPolicy.ImageModelOnly,
                ReviewRubricTemplateCatalog.GeneralImage,
                "{item-slug}"));
    }
}
