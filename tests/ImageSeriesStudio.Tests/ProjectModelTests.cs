using ImageSeriesStudio.Core.Projects;

namespace ImageSeriesStudio.Tests;

public sealed class ProjectModelTests
{
    [Fact]
    public void SeriesItem_ProgressesThroughApprovedDeliveryWorkflow()
    {
        var timestamp = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);
        var item = SeriesItem.Create("cover", "Opening image", timestamp);

        item.MarkReady(timestamp.AddMinutes(1));
        item.MarkGenerating(timestamp.AddMinutes(2));
        item.MarkNeedsReview(timestamp.AddMinutes(3));
        item.Approve(timestamp.AddMinutes(4));
        item.MarkDelivered(timestamp.AddMinutes(5));

        Assert.Equal(SeriesItemStatus.Delivered, item.Status);
        Assert.Equal(timestamp, item.CreatedAt);
        Assert.Equal(timestamp.AddMinutes(5), item.UpdatedAt);
    }

    [Fact]
    public void SeriesItem_RejectsBackwardTransitionWithoutReopen()
    {
        var timestamp = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);
        var item = SeriesItem.Create("cover", "Opening image", timestamp);

        item.MarkReady(timestamp.AddMinutes(1));
        item.MarkGenerating(timestamp.AddMinutes(2));
        item.MarkNeedsReview(timestamp.AddMinutes(3));

        var exception = Assert.Throws<InvalidSeriesItemStateTransitionException>(
            () => item.MarkReady(timestamp.AddMinutes(4)));

        Assert.Contains("NeedsReview -> Ready", exception.Message);
        Assert.Equal(SeriesItemStatus.NeedsReview, item.Status);
    }

    [Fact]
    public void SeriesItem_CanReopenForRevisionBeforeMovingForwardAgain()
    {
        var timestamp = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);
        var item = SeriesItem.Create("cover", "Opening image", timestamp);

        item.MarkReady(timestamp.AddMinutes(1));
        item.MarkGenerating(timestamp.AddMinutes(2));
        item.MarkNeedsReview(timestamp.AddMinutes(3));

        item.ReopenForRevision("Text is unreadable", timestamp.AddMinutes(4));
        item.MarkReady(timestamp.AddMinutes(5));

        Assert.Equal(SeriesItemStatus.Ready, item.Status);
        Assert.Equal("Text is unreadable", item.RevisionReason);
        Assert.Equal(timestamp.AddMinutes(5), item.UpdatedAt);
    }
}
