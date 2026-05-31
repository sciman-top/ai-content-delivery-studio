using System.Text.Json;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Infrastructure.Delivery;

namespace ImageSeriesStudio.Tests;

public sealed class DeliveryPackageTests
{
    [Fact]
    public async Task DeliveryPackageWriter_ExportsOnlyApprovedFinalImagesWithManifest()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var sourceDirectory = Path.Combine(rootDirectory, "source");
        var packageDirectory = Path.Combine(rootDirectory, "delivery");
        Directory.CreateDirectory(sourceDirectory);

        try
        {
            var approvedImage = Path.Combine(sourceDirectory, "approved.png");
            var approvedMetadata = Path.Combine(sourceDirectory, "approved.json");
            var rejectedImage = Path.Combine(sourceDirectory, "rejected.png");
            var rejectedMetadata = Path.Combine(sourceDirectory, "rejected.json");

            await File.WriteAllBytesAsync(approvedImage, [1, 2, 3], CancellationToken.None);
            await File.WriteAllTextAsync(approvedMetadata, """{"providerId":"fake-image"}""", CancellationToken.None);
            await File.WriteAllBytesAsync(rejectedImage, [4, 5, 6], CancellationToken.None);
            await File.WriteAllTextAsync(rejectedMetadata, """{"providerId":"fake-image"}""", CancellationToken.None);

            var writer = new DeliveryPackageWriter();
            var result = await writer.WriteAsync(
                new DeliveryPackageRequest(
                    "Sample project",
                    packageDirectory,
                    [
                        new DeliveryPackageItem(
                            "cover",
                            "Cover",
                            approvedImage,
                            approvedMetadata,
                            "A clean blue poster background",
                            ReviewDecision.Pass,
                            HumanApproved: true),
                        new DeliveryPackageItem(
                            "alt",
                            "Rejected alternate",
                            rejectedImage,
                            rejectedMetadata,
                            "A rejected alternate",
                            ReviewDecision.Fail,
                            HumanApproved: false),
                    ]),
                CancellationToken.None);

            var finalImage = Assert.Single(result.FinalImagePaths);
            Assert.True(File.Exists(finalImage));
            Assert.True(File.Exists(result.ManifestJsonPath));
            Assert.True(File.Exists(result.ManifestCsvPath));
            Assert.True(File.Exists(result.ReviewReportPath));
            Assert.True(File.Exists(Path.Combine(result.PackageDirectory, "prompts", "cover.txt")));
            Assert.True(File.Exists(Path.Combine(result.PackageDirectory, "metadata", "cover.json")));
            Assert.False(File.Exists(Path.Combine(result.PackageDirectory, "images", "alt.png")));

            using var manifestStream = File.OpenRead(result.ManifestJsonPath);
            using var manifest = await JsonDocument.ParseAsync(manifestStream, cancellationToken: CancellationToken.None);
            var items = manifest.RootElement.GetProperty("items");

            Assert.Equal("Sample project", manifest.RootElement.GetProperty("projectName").GetString());
            Assert.Equal(1, items.GetArrayLength());
            Assert.Equal("Cover", items[0].GetProperty("title").GetString());
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }
}
