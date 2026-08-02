using ContentDeliveryStudio.Application.Delivery;
using ContentDeliveryStudio.Application.Projects;

namespace ContentDeliveryStudio.Tests;

public sealed class DeliveryExportRequestTests
{
    [Fact]
    public void CreateForFinalDelivery_UsesCategoryPathAndHonorsCustomRoot()
    {
        using var localStudioRoot = LocalStudioDataPathScope.Create();
        var customRoot = Path.Combine(localStudioRoot.RootPath, "external-final-root");
        var projectId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        var timestamp = DateTimeOffset.Parse("2026-08-01T11:00:00Z");

        var request = DeliveryExportRequest.CreateForFinalDelivery(
            "Poster report",
            projectId,
            FinalImageDeliveryCategory.PosterReportVisuals,
            timestamp,
            [],
            customRoot);

        Assert.Equal(
            Path.Combine(
                Path.GetFullPath(customRoot),
                "poster-report-visuals",
                projectId.ToString("N"),
                "20260801-110000"),
            request.OutputDirectory);
        Assert.Empty(request.Items);
    }

    [Fact]
    public void CreateForFinalDelivery_RejectsEmptyProjectIdBeforeWriting()
    {
        using var localStudioRoot = LocalStudioDataPathScope.Create();

        var exception = Assert.Throws<ArgumentException>(() =>
            DeliveryExportRequest.CreateForFinalDelivery(
                "Invalid",
                Guid.Empty,
                FinalImageDeliveryCategory.ImageSeries,
                DateTimeOffset.UtcNow,
                []));

        Assert.Equal("projectId", exception.ParamName);
    }
}
