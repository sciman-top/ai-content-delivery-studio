using ContentDeliveryStudio.Core.Styles;

namespace ContentDeliveryStudio.Tests;

public sealed class StyleGuideTests
{
    [Fact]
    public void Create_NormalizesRulesAndReferenceSetLinks()
    {
        var seriesId = Guid.NewGuid();
        var firstReferenceSetId = Guid.NewGuid();
        var secondReferenceSetId = Guid.NewGuid();
        var timestamp = new DateTimeOffset(2026, 6, 1, 11, 0, 0, TimeSpan.Zero);

        var styleGuide = StyleGuide.Create(
            seriesId,
            " Physics chalk style ",
            [" cinematic diagrams ", " ", "clear visual hierarchy"],
            [" cyan", "gold "],
            ["soft rim light"],
            ["main subject centered", "leave text-safe margin"],
            ["chalk texture", "thin diagram strokes"],
            ["no illegible model-rendered text"],
            [firstReferenceSetId, firstReferenceSetId, secondReferenceSetId],
            timestamp);

        Assert.Equal(seriesId, styleGuide.SeriesId);
        Assert.Equal("Physics chalk style", styleGuide.Name);
        Assert.Equal(1, styleGuide.Version);
        Assert.Equal(["cinematic diagrams", "clear visual hierarchy"], styleGuide.VisualPrinciples);
        Assert.Equal(["cyan", "gold"], styleGuide.Palette);
        Assert.Equal(["main subject centered", "leave text-safe margin"], styleGuide.CompositionRules);
        Assert.Equal([firstReferenceSetId, secondReferenceSetId], styleGuide.ReferenceImageSetIds);
        Assert.Equal(timestamp, styleGuide.CreatedAt);
    }

    [Fact]
    public void Revise_CreatesNextVersionWithoutChangingIdentity()
    {
        var timestamp = new DateTimeOffset(2026, 6, 1, 11, 0, 0, TimeSpan.Zero);
        var styleGuide = StyleGuide.Create(
            Guid.NewGuid(),
            "Original",
            ["minimal"],
            ["blue"],
            ["flat"],
            ["wide layout"],
            ["smooth"],
            [],
            [],
            timestamp);

        var revised = styleGuide.Revise(
            "Original revised",
            ["minimal", "more contrast"],
            ["blue", "white"],
            ["flat"],
            ["wide layout"],
            ["smooth"],
            ["avoid clutter"],
            [],
            timestamp.AddMinutes(5));

        Assert.Equal(styleGuide.Id, revised.Id);
        Assert.Equal(styleGuide.SeriesId, revised.SeriesId);
        Assert.Equal(2, revised.Version);
        Assert.Equal("Original revised", revised.Name);
        Assert.Equal(["avoid clutter"], revised.NegativeConstraints);
        Assert.Equal(timestamp, revised.CreatedAt);
        Assert.Equal(timestamp.AddMinutes(5), revised.UpdatedAt);
    }

    [Fact]
    public void Create_RejectsEmptyNameAndMissingVisualPrinciples()
    {
        var timestamp = new DateTimeOffset(2026, 6, 1, 11, 0, 0, TimeSpan.Zero);

        Assert.Throws<ArgumentException>(() =>
            StyleGuide.Create(Guid.NewGuid(), " ", ["minimal"], [], [], [], [], [], [], timestamp));

        Assert.Throws<ArgumentException>(() =>
            StyleGuide.Create(Guid.NewGuid(), "Style", [" "], [], [], [], [], [], [], timestamp));
    }
}
