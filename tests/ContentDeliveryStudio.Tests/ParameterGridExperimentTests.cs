using ContentDeliveryStudio.Core.Experiments;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Styles;

namespace ContentDeliveryStudio.Tests;

public sealed class ParameterGridExperimentTests
{
    [Fact]
    public void CreateVariants_BuildsStableCartesianProduct()
    {
        var settings = new GenerationSettings(1024, 1024, "standard", "png", seed: 42);
        var axes = new[]
        {
            new ParameterGridAxis("lighting", ["soft", "dramatic"]),
            new ParameterGridAxis("palette", ["warm", "cool"]),
        };

        var variants = ParameterGridExperiment.CreateVariants(
            "Scientific poster background.",
            settings,
            axes);

        Assert.Equal(4, variants.Count);
        Assert.Equal("001-lighting-soft-palette-warm", variants[0].Slug);
        Assert.Equal("004-lighting-dramatic-palette-cool", variants[3].Slug);
        Assert.Equal(
            "Scientific poster background.\n\nParameters: lighting=soft; palette=warm",
            variants[0].PromptText);
        Assert.Equal("dramatic", variants[3].ParameterValues["lighting"]);
        Assert.Same(settings, variants[0].Settings);
    }

    [Fact]
    public void CreateVariants_ReplacesPromptTemplateTokens()
    {
        var settings = new GenerationSettings(1536, 1024, "high", "png");
        var axes = new[]
        {
            new ParameterGridAxis("lighting", ["rim light"]),
            new ParameterGridAxis("palette", ["cyan and gold"]),
        };

        var variants = ParameterGridExperiment.CreateVariants(
            "A {{lighting}} scene using {{palette}}.",
            settings,
            axes);

        var variant = Assert.Single(variants);
        Assert.Equal("A rim light scene using cyan and gold.", variant.PromptText);
        Assert.Equal("rim light", variant.ParameterValues["lighting"]);
        Assert.Equal("cyan and gold", variant.ParameterValues["palette"]);
    }

    [Fact]
    public void CreateVariants_RejectsEmptyValuesAndDuplicateAxisNames()
    {
        var settings = new GenerationSettings(1024, 1024, "standard", "png");

        Assert.Throws<ArgumentException>(() =>
            ParameterGridExperiment.CreateVariants(
                "Prompt.",
                settings,
                [new ParameterGridAxis("lighting", [])]));

        Assert.Throws<ArgumentException>(() =>
            ParameterGridExperiment.CreateVariants(
                "Prompt.",
                settings,
                [
                    new ParameterGridAxis("lighting", ["soft"]),
                    new ParameterGridAxis("LIGHTING", ["dramatic"]),
                ]));
    }

    [Fact]
    public void CreateVariants_RecordsExperimentMetadataAndGenerationTaskLink()
    {
        var settings = new GenerationSettings(1024, 1024, "standard", "png", seed: 7);
        var recipe = GenerationRecipe.Create(
            Guid.NewGuid(),
            "fake-image-v1",
            ImageTypePresetCatalog.ArticleCover,
            1024,
            1024,
            "standard",
            "png",
            ImageBackgroundMode.Auto,
            compression: null,
            ImageModerationMode.Auto,
            seed: 7,
            []);

        var variant = ParameterGridExperiment.CreateVariants(
            "Base prompt with {{mood}}.",
            settings,
            [new ParameterGridAxis("mood", ["calm"])],
            recipe)
            .Single();
        var taskId = Guid.NewGuid();

        var linked = variant.WithGenerationTask(taskId);

        Assert.Equal("Base prompt with {{mood}}.", linked.BasePrompt);
        Assert.Equal(recipe.Id, linked.Recipe!.Id);
        Assert.Equal("mood", Assert.Single(linked.Axes).Name);
        Assert.Equal(taskId, linked.GenerationTaskId);
        Assert.Equal("001-mood-calm", linked.Slug);
    }
}
