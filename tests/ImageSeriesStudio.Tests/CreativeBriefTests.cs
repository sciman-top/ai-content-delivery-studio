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
