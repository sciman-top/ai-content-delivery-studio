using ContentDeliveryStudio.Core.Styles;

namespace ContentDeliveryStudio.Tests;

public sealed class GenerationRecipeTests
{
    [Fact]
    public void Create_NormalizesProviderNeutralSettingsAndWarnings()
    {
        var providerProfileId = Guid.NewGuid();

        var recipe = GenerationRecipe.Create(
            providerProfileId,
            " gpt-image-2 ",
            ImageTypePresetCatalog.EducationalPoster,
            1536,
            2048,
            " HIGH ",
            " PNG ",
            ImageBackgroundMode.Opaque,
            compression: 90,
            ImageModerationMode.Auto,
            seed: 42,
            [" transparent background unsupported ", " "]);

        Assert.NotEqual(Guid.Empty, recipe.Id);
        Assert.Equal(providerProfileId, recipe.ProviderProfileId);
        Assert.Equal("gpt-image-2", recipe.ModelId);
        Assert.Equal(ImageTypePresetCatalog.EducationalPoster, recipe.ImageTypePresetId);
        Assert.Equal(1536, recipe.Width);
        Assert.Equal(2048, recipe.Height);
        Assert.Equal("high", recipe.Quality);
        Assert.Equal("png", recipe.OutputFormat);
        Assert.Equal(ImageBackgroundMode.Opaque, recipe.Background);
        Assert.Equal(90, recipe.Compression);
        Assert.Equal(ImageModerationMode.Auto, recipe.Moderation);
        Assert.Equal(42, recipe.Seed);
        Assert.Equal(["transparent background unsupported"], recipe.CapabilityWarnings);
    }

    [Fact]
    public void Create_AllowsOptionalCompressionSeedAndWarnings()
    {
        var recipe = GenerationRecipe.Create(
            Guid.NewGuid(),
            "fake-image",
            ImageTypePresetCatalog.ArticleCover,
            1024,
            1024,
            "standard",
            "webp",
            ImageBackgroundMode.Auto,
            compression: null,
            ImageModerationMode.Low,
            seed: null,
            []);

        Assert.Null(recipe.Compression);
        Assert.Null(recipe.Seed);
        Assert.Empty(recipe.CapabilityWarnings);
    }

    [Fact]
    public void Create_RejectsInvalidRequiredSettings()
    {
        Assert.Throws<ArgumentException>(() =>
            GenerationRecipe.Create(
                Guid.Empty,
                "model",
                ImageTypePresetCatalog.ArticleCover,
                1024,
                1024,
                "standard",
                "png",
                ImageBackgroundMode.Auto,
                null,
                ImageModerationMode.Auto,
                null,
                []));

        Assert.Throws<ArgumentException>(() =>
            GenerationRecipe.Create(
                Guid.NewGuid(),
                "",
                ImageTypePresetCatalog.ArticleCover,
                1024,
                1024,
                "standard",
                "png",
                ImageBackgroundMode.Auto,
                null,
                ImageModerationMode.Auto,
                null,
                []));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            GenerationRecipe.Create(
                Guid.NewGuid(),
                "model",
                ImageTypePresetCatalog.ArticleCover,
                0,
                1024,
                "standard",
                "png",
                ImageBackgroundMode.Auto,
                null,
                ImageModerationMode.Auto,
                null,
                []));
    }

    [Fact]
    public void Create_RejectsCompressionOutsideImageRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            GenerationRecipe.Create(
                Guid.NewGuid(),
                "model",
                ImageTypePresetCatalog.ArticleCover,
                1024,
                1024,
                "standard",
                "png",
                ImageBackgroundMode.Auto,
                compression: 101,
                ImageModerationMode.Auto,
                null,
                []));
    }
}
