using System.Globalization;
using ImageSeriesStudio.Application.Localization;
using ImageSeriesStudio.App.ViewModels;
using ImageSeriesStudio.Core.Projects;

namespace ImageSeriesStudio.Tests;

public sealed class WorkflowGraphCoordinatorTests
{
    [Fact]
    public void BuildRows_IncludesProjectSeriesItemPromptAndCandidateNodes()
    {
        var coordinator = new WorkflowGraphCoordinator(new LocalizationService(() => new CultureInfo("en-US")));
        var project = new ProjectSummaryViewModel(Guid.NewGuid(), "Graph UI demo", DateTimeOffset.Parse("2026-06-09T12:00:00Z"));
        var itemId = Guid.NewGuid();
        var series = new[]
        {
            new SeriesSummaryViewModel(
                Guid.NewGuid(),
                "Lesson visuals",
                [
                    new SeriesItemViewModel(
                        itemId,
                        "Opening frame",
                        "Opening visual for a lesson.",
                        SeriesItemKind.Standard,
                        SeriesItemStatus.Draft,
                        [
                            new PromptVersionViewModel(
                                Guid.NewGuid(),
                                1,
                                "Create a clean opening frame.",
                                "1024x1024 standard png",
                                DateTimeOffset.Parse("2026-06-09T12:01:00Z")),
                        ]),
                ]),
        };
        var galleryRows = new[]
        {
            new GalleryRowViewModel(
                Guid.NewGuid(),
                itemId,
                "Opening frame",
                @"C:\temp\candidate.png",
                @"C:\temp\candidate.json",
                "Create a clean opening frame."),
        };

        var rows = coordinator.BuildRows(
            project,
            series,
            galleryRows,
            reviewRows: [],
            deliveryRows: []);

        Assert.Contains(rows, row => row.NodeType == "Project" && row.Title == "Graph UI demo");
        Assert.Contains(rows, row => row.NodeType == "Series" && row.Title == "Lesson visuals");
        Assert.Contains(rows, row => row.NodeType == "Item" && row.Title == "Opening frame");
        Assert.Contains(rows, row => row.NodeType == "Prompt" && row.LinksTo == "Opening frame");
        Assert.Contains(rows, row => row.NodeType == "Candidate" && row.LinksTo == "Opening frame");
    }

    [Fact]
    public void BuildRows_IncludesReviewAndDeliveryNodesWhenPresent()
    {
        var coordinator = new WorkflowGraphCoordinator(new LocalizationService(() => new CultureInfo("en-US")));
        var project = new ProjectSummaryViewModel(Guid.NewGuid(), "Delivery graph demo", DateTimeOffset.Parse("2026-06-09T13:00:00Z"));
        var candidateId = Guid.NewGuid();
        var rows = coordinator.BuildRows(
            project,
            series: [],
            galleryRows:
            [
                new GalleryRowViewModel(
                    candidateId,
                    Guid.NewGuid(),
                    "Approved frame",
                    @"C:\temp\approved.png",
                    @"C:\temp\approved.json",
                    "Approved prompt"),
            ],
            reviewRows:
            [
                new ReviewRowViewModel(
                    candidateId,
                    "Approved frame",
                    ReviewDecision.Pass.ToString(),
                    "match:5",
                    "Ready.",
                    string.Empty,
                    "None",
                    HumanApproved: true,
                    "Human approved",
                    "Teacher",
                    "Looks ready.",
                    DateTimeOffset.Parse("2026-06-09T13:01:00Z"),
                    new StructuredReviewOutput(
                        candidateId,
                        ReviewDecision.Pass,
                        [new StructuredReviewScore("match", "Matches prompt.", 3, 5)],
                        [],
                        "Ready.",
                        null)),
            ],
            deliveryRows:
            [
                new DeliveryRowViewModel(
                    @"C:\temp\delivery\20260609-130000",
                    @"C:\temp\delivery\manifest.json",
                    @"C:\temp\delivery\manifest.csv",
                    @"C:\temp\delivery\review-report.md",
                    "1"),
            ]);

        Assert.Contains(rows, row => row.NodeType == "Review" && row.Title == "Pass");
        Assert.Contains(rows, row => row.NodeType == "Delivery" && row.Summary == "1 images");
    }
}
