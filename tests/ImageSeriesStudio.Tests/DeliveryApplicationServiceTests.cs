using ImageSeriesStudio.Application.Delivery;
using ImageSeriesStudio.Core.Projects;

namespace ImageSeriesStudio.Tests;

public sealed class DeliveryApplicationServiceTests
{
    [Fact]
    public async Task ExportDeliveryPackageAsync_RejectsMissingWriter()
    {
        var service = new DeliveryApplicationService(deliveryPackageWriter: null);
        var request = new DeliveryExportRequest("Project", "outputs/delivery", []);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ExportDeliveryPackageAsync(request, CancellationToken.None));

        Assert.Equal("Delivery package writer is not registered.", error.Message);
    }

    [Fact]
    public async Task ExportDeliveryPackageAsync_DelegatesToRegisteredWriter()
    {
        var writer = new RecordingDeliveryPackageWriter();
        var service = new DeliveryApplicationService(writer);
        var request = new DeliveryExportRequest(
            "Project",
            "outputs/delivery",
            [
                new DeliveryExportItem(
                    "final",
                    "Final",
                    "outputs/final.png",
                    "outputs/final.json",
                    "Prompt",
                    ReviewDecision.Pass,
                    HumanApproved: true),
            ]);

        var result = await service.ExportDeliveryPackageAsync(request, CancellationToken.None);

        Assert.Same(request, writer.Request);
        Assert.Equal("outputs/delivery", result.PackageDirectory);
        Assert.Equal("outputs/delivery/manifest.json", result.ManifestJsonPath);
        Assert.Equal("outputs/final.png", Assert.Single(result.FinalImagePaths));
    }

    private sealed class RecordingDeliveryPackageWriter : IDeliveryPackageWriter
    {
        public DeliveryExportRequest? Request { get; private set; }

        public Task<DeliveryExportResult> WriteAsync(
            DeliveryExportRequest request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new DeliveryExportResult(
                request.OutputDirectory,
                "outputs/delivery/manifest.json",
                "outputs/delivery/manifest.csv",
                "outputs/delivery/review-report.md",
                ["outputs/final.png"]));
        }
    }
}
