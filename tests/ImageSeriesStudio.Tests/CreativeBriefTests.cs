using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Styles;

namespace ImageSeriesStudio.Tests;

public sealed class CreativeBriefTests
{
    [Fact]
    public void Create_TrimsFieldsAndStoresPromptDirections()
    {
        var timestamp = new DateTimeOffset(2026, 6, 2, 9, 0, 0, TimeSpan.Zero);
        var seriesId = Guid.NewGuid();

        var brief = CreativeBrief.Create(
            seriesId,
            " Article illustration set ",
            " teachers ",
            ImageTextPolicy.DeterministicPostRender,
            "clean educational style",
            ["formula labels"],
            ["fake historical scene"],
            timestamp);

        var direction = PromptDirection.Create(
            "conservative",
            "Conservative faithful",
            "Safest route for classroom accuracy.",
            "Create a clean visual background.",
            "No fake labels.",
            "Strong factual match.",
            "May feel restrained.",
            timestamp.AddMinutes(1));

        brief.ReplaceDirections([direction], timestamp.AddMinutes(2));

        Assert.Equal(seriesId, brief.SeriesId);
        Assert.Equal("Article illustration set", brief.Goal);
        Assert.Equal("teachers", brief.Audience);
        Assert.Equal(ImageTextPolicy.DeterministicPostRender, brief.TextPolicy);
        Assert.Equal("clean educational style", brief.StyleIntent);
        Assert.Equal("formula labels", Assert.Single(brief.MustInclude));
        Assert.Equal("fake historical scene", Assert.Single(brief.MustAvoid));
        Assert.Equal("conservative", Assert.Single(brief.PromptDirections).Key);
        Assert.Equal(timestamp.AddMinutes(2), brief.UpdatedAt);
    }

    [Fact]
    public void Create_RejectsBlankRequiredFields()
    {
        var timestamp = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            CreativeBrief.Create(
                Guid.NewGuid(),
                " ",
                "teachers",
                ImageTextPolicy.Hybrid,
                "style",
                [],
                [],
                timestamp));

        Assert.Throws<ArgumentException>(() =>
            PromptDirection.Create(
                " ",
                "Name",
                "Use",
                "Prompt",
                "Negative",
                "Strength",
                "Risk",
                timestamp));
    }

    [Fact]
    public void PromptDirection_StoresStructuredRecommendation()
    {
        var timestamp = new DateTimeOffset(2026, 6, 2, 11, 0, 0, TimeSpan.Zero);
        var recommendation = PromptDirectionRecommendation.Create(
            ImageTypePresetCatalog.EducationalPoster,
            ImageTextPolicy.DeterministicPostRender,
            "clean classroom poster",
            new AspectRatio(4, 5),
            1024,
            1280,
            "draft",
            "png",
            ImageBackgroundMode.Opaque,
            ReviewRubricTemplateCatalog.TextHeavyPoster,
            draftCount: 2,
            finalCount: 1,
            "Text-heavy educational output needs deterministic labels.",
            confidence: 0.9,
            ["model text can be unreliable"],
            ["reserve a formula area"]);

        var direction = PromptDirection.Create(
            "conservative",
            "Conservative faithful",
            "Use for accurate classroom delivery.",
            "Create a clean educational poster background.",
            "No unreadable formula text.",
            "Accurate and easy to review.",
            "Less dramatic than a cover image.",
            timestamp,
            recommendation);

        Assert.NotNull(direction.Recommendation);
        Assert.Equal(ImageTypePresetCatalog.EducationalPoster, direction.Recommendation.ImageTypePresetId);
        Assert.Equal(ImageTextPolicy.DeterministicPostRender, direction.Recommendation.TextPolicy);
        Assert.Equal(new AspectRatio(4, 5), direction.Recommendation.AspectRatio);
        Assert.Equal("draft", direction.Recommendation.QualityBand);
        Assert.Equal("png", direction.Recommendation.OutputFormat);
        Assert.Equal(0.9, direction.Recommendation.Confidence);
        Assert.Equal("model text can be unreliable", Assert.Single(direction.Recommendation.CapabilityWarnings));
        Assert.Equal("reserve a formula area", Assert.Single(direction.Recommendation.NonExecutableSuggestions));
    }

    [Fact]
    public void PromptDirectionRecommendation_RejectsInvalidExecutableValues()
    {
        Assert.Throws<InvalidOperationException>(() =>
            PromptDirectionRecommendation.Create(
                "missing-preset",
                ImageTextPolicy.Hybrid,
                "style",
                new AspectRatio(1, 1),
                1024,
                1024,
                "draft",
                "png",
                ImageBackgroundMode.Auto,
                ReviewRubricTemplateCatalog.GeneralImage,
                draftCount: 1,
                finalCount: 1,
                "reason",
                confidence: 0.7,
                [],
                []));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PromptDirectionRecommendation.Create(
                ImageTypePresetCatalog.ArticleCover,
                ImageTextPolicy.Hybrid,
                "style",
                new AspectRatio(16, 9),
                0,
                1024,
                "draft",
                "png",
                ImageBackgroundMode.Auto,
                ReviewRubricTemplateCatalog.GeneralImage,
                draftCount: 1,
                finalCount: 1,
                "reason",
                confidence: 0.7,
                [],
                []));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PromptDirectionRecommendation.Create(
                ImageTypePresetCatalog.ArticleCover,
                ImageTextPolicy.Hybrid,
                "style",
                new AspectRatio(16, 9),
                1536,
                1024,
                "draft",
                "png",
                ImageBackgroundMode.Auto,
                ReviewRubricTemplateCatalog.GeneralImage,
                draftCount: 1,
                finalCount: 1,
                "reason",
                confidence: 1.2,
                [],
                []));
    }

    [Fact]
    public void ImageSeries_AddCreativeBrief_AttachesBriefToSeries()
    {
        var timestamp = new DateTimeOffset(2026, 6, 2, 9, 0, 0, TimeSpan.Zero);
        var series = ImageSeries.Create(Guid.NewGuid(), "Series", "Description", timestamp);

        var brief = series.AddCreativeBrief(
            "course poster",
            "middle school teachers",
            ImageTextPolicy.DeterministicPostRender,
            "editorial science style",
            ["accurate diagram"],
            ["unreadable text"],
            timestamp.AddMinutes(1));

        Assert.Equal(series.Id, brief.SeriesId);
        Assert.Equal("course poster", brief.Goal);
        Assert.Single(series.CreativeBriefs);
    }
}
