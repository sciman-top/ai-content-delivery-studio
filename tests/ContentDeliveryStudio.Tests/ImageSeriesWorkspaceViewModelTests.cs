using ContentDeliveryStudio.App.ViewModels;
using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.Tests;

public sealed class ImageSeriesWorkspaceViewModelTests
{
    [Fact]
    public void GalleryRows_OwnSelectionAndPreserveMatchingCandidate()
    {
        var first = CreateGalleryRow("first");
        var second = CreateGalleryRow("second");
        var workspace = new ImageSeriesWorkspaceViewModel
        {
            GalleryRows = [first, second],
        };

        Assert.Same(first, workspace.SelectedGalleryRow);
        workspace.SelectedGalleryRow = second;
        workspace.GalleryRows = [second];

        Assert.True(workspace.HasGalleryRows);
        Assert.Same(second, workspace.SelectedGalleryRow);

        workspace.GalleryRows = [];

        Assert.False(workspace.HasGalleryRows);
        Assert.Null(workspace.SelectedGalleryRow);
    }

    [Fact]
    public void ReviewRows_OwnSelectionAndPreserveMatchingCandidate()
    {
        var first = CreateReviewRow("first");
        var second = CreateReviewRow("second");
        var workspace = new ImageSeriesWorkspaceViewModel
        {
            ReviewRows = [first, second],
        };

        Assert.Same(first, workspace.SelectedReviewRow);
        workspace.SelectedReviewRow = second;
        workspace.ReviewRows = [second];

        Assert.True(workspace.HasReviewRows);
        Assert.Same(second, workspace.SelectedReviewRow);

        workspace.ReviewRows = [];

        Assert.False(workspace.HasReviewRows);
        Assert.Null(workspace.SelectedReviewRow);
    }

    [Fact]
    public void ApplyLocalization_OwnsGalleryAndReviewPresentationText()
    {
        var workspace = new ImageSeriesWorkspaceViewModel();

        workspace.ApplyLocalization(
            "Item",
            "Candidate",
            "Metadata",
            "No candidates",
            "Review item",
            "Decision",
            "Score",
            "Comments",
            "Fix",
            "Route",
            "Approval",
            "No reviews",
            "Reviewer",
            "Notes");

        Assert.Equal("Item", workspace.GalleryItemColumn);
        Assert.Equal("Candidate", workspace.GalleryImageColumn);
        Assert.Equal("Metadata", workspace.GalleryMetadataColumn);
        Assert.Equal("No candidates", workspace.NoGalleryRowsText);
        Assert.Equal("Review item", workspace.ReviewItemColumn);
        Assert.Equal("Decision", workspace.ReviewDecisionColumn);
        Assert.Equal("Score", workspace.ReviewScoreColumn);
        Assert.Equal("Comments", workspace.ReviewCommentsColumn);
        Assert.Equal("Fix", workspace.ReviewFixColumn);
        Assert.Equal("Route", workspace.ReviewRouteColumn);
        Assert.Equal("Approval", workspace.HumanApprovalColumn);
        Assert.Equal("No reviews", workspace.NoReviewRowsText);
        Assert.Equal("Reviewer", workspace.FinalApprovalReviewerLabel);
        Assert.Equal("Notes", workspace.FinalApprovalNotesLabel);
    }

    private static GalleryRowViewModel CreateGalleryRow(string title)
    {
        return new GalleryRowViewModel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            title,
            $"{title}.png",
            $"{title}.json",
            $"Prompt for {title}");
    }

    private static ReviewRowViewModel CreateReviewRow(string title)
    {
        return new ReviewRowViewModel(
            Guid.NewGuid(),
            title,
            ReviewDecision.Pass.ToString(),
            "match:5",
            "Ready",
            string.Empty,
            ReviewOutcomeTargetLayer.None.ToString(),
            HumanApproved: false,
            "Pending",
            string.Empty,
            string.Empty,
            null,
            new StructuredReviewOutput(
                Guid.NewGuid(),
                ReviewDecision.Pass,
                [new StructuredReviewScore("match", "Matches the brief.", 3, 5)],
                [],
                "Ready",
                SuggestedFix: null));
    }
}
