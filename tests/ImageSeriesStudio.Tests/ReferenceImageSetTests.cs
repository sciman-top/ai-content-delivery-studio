using ImageSeriesStudio.Core.References;

namespace ImageSeriesStudio.Tests;

public sealed class ReferenceImageSetTests
{
    [Fact]
    public void AddImage_NormalizesWorkspaceRelativePathAndTracksRole()
    {
        var timestamp = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var set = ReferenceImageSet.Create(Guid.NewGuid(), "Physics style board", timestamp);

        var image = set.AddImage(
            " references\\style\\chalkboard.png ",
            ReferenceImageRole.Style,
            "Keep the same chalk texture and cool highlights.",
            timestamp.AddMinutes(1));

        Assert.Equal("references/style/chalkboard.png", image.AssetPath);
        Assert.Equal(ReferenceImageRole.Style, image.Role);
        Assert.Equal("Keep the same chalk texture and cool highlights.", image.Description);
        Assert.Equal(timestamp.AddMinutes(1), set.UpdatedAt);
        Assert.Contains(image, set.Images);
    }

    [Fact]
    public void AddImage_RejectsAbsoluteAndEscapingPaths()
    {
        var timestamp = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var set = ReferenceImageSet.Create(Guid.NewGuid(), "References", timestamp);

        Assert.Throws<ArgumentException>(() =>
            set.AddImage("C:\\Users\\me\\secret.png", ReferenceImageRole.Subject, "", timestamp));
        Assert.Throws<ArgumentException>(() =>
            set.AddImage("../outside.png", ReferenceImageRole.Subject, "", timestamp));
        Assert.Throws<ArgumentException>(() =>
            set.AddImage("references/../outside.png", ReferenceImageRole.Subject, "", timestamp));
    }

    [Fact]
    public void AddImage_RejectsDuplicateAssetPathInsideSet()
    {
        var timestamp = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var set = ReferenceImageSet.Create(Guid.NewGuid(), "References", timestamp);
        set.AddImage("references/style.png", ReferenceImageRole.Style, "", timestamp);

        Assert.Throws<InvalidOperationException>(() =>
            set.AddImage("references\\STYLE.png", ReferenceImageRole.Palette, "", timestamp));
    }
}
