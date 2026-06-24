using System.Text.Json;
using ContentDeliveryStudio.Application.Delivery;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Infrastructure.Delivery;

namespace ContentDeliveryStudio.Tests;

public sealed class DeliveryPackageTests
{
    [Fact]
    public async Task DeliveryPackageWriter_ExportsOnlyApprovedFinalImagesWithManifest()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
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
            var styleGuideId = Guid.NewGuid();
            var recipeId = Guid.NewGuid();
            var referenceSetId = Guid.NewGuid();
            var generationTaskId = Guid.NewGuid();
            var blueprintId = Guid.NewGuid();
            var outputArtifactId = Guid.NewGuid();
            var sourceAssetId = Guid.NewGuid();
            var evidenceAnchorId = Guid.NewGuid();
            var operatorRunId = Guid.NewGuid();
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
                            HumanApproved: true,
                            FinalReviewer: "Teacher",
                            FinalApprovalNotes: "Ready for classroom delivery.",
                            FinalApprovalDecidedAt: DateTimeOffset.Parse("2026-06-07T09:30:00Z"),
                            StyleGuideId: styleGuideId,
                            StyleGuideVersion: 2,
                            RecipeId: recipeId,
                            ReferenceImageSetIds: [referenceSetId],
                            ExperimentSlug: "001-lighting-soft",
                            ExperimentParameters: new Dictionary<string, string> { ["lighting"] = "soft" },
                            GenerationTaskId: generationTaskId,
                            OutputArtifactId: outputArtifactId,
                            SourceAssetIds: [sourceAssetId],
                            EvidenceAnchorIds: [evidenceAnchorId],
                            ArtifactRole: "final-image",
                            Blueprint: new DeliveryBlueprintMetadata(
                                blueprintId,
                                "panel-narrative-sequence",
                                "Panel narrative sequence",
                                "storyboard",
                                "panel_sequence",
                                "same main character; consistent scene grammar",
                                "alternate camera distance"),
                            OperatorRunIds: [operatorRunId]),
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
            Assert.Equal("Teacher", items[0].GetProperty("finalReviewer").GetString());
            Assert.Equal("Ready for classroom delivery.", items[0].GetProperty("finalApprovalNotes").GetString());
            Assert.Equal(
                DateTimeOffset.Parse("2026-06-07T09:30:00Z"),
                items[0].GetProperty("finalApprovalDecidedAt").GetDateTimeOffset());
            Assert.Equal(styleGuideId, items[0].GetProperty("styleGuideId").GetGuid());
            Assert.Equal(2, items[0].GetProperty("styleGuideVersion").GetInt32());
            Assert.Equal(recipeId, items[0].GetProperty("recipeId").GetGuid());
            Assert.Equal(referenceSetId, items[0].GetProperty("referenceImageSetIds")[0].GetGuid());
            Assert.Equal("001-lighting-soft", items[0].GetProperty("experimentSlug").GetString());
            Assert.Equal("soft", items[0].GetProperty("experimentParameters").GetProperty("lighting").GetString());
            Assert.Equal(generationTaskId, items[0].GetProperty("generationTaskId").GetGuid());
            Assert.Equal(outputArtifactId, items[0].GetProperty("outputArtifactId").GetGuid());
            Assert.Equal(sourceAssetId, items[0].GetProperty("sourceAssetIds")[0].GetGuid());
            Assert.Equal(evidenceAnchorId, items[0].GetProperty("evidenceAnchorIds")[0].GetGuid());
            Assert.Equal(operatorRunId, items[0].GetProperty("operatorRunIds")[0].GetGuid());
            Assert.Equal("final-image", items[0].GetProperty("artifactRole").GetString());
            var blueprint = items[0].GetProperty("blueprint");
            Assert.Equal(blueprintId, blueprint.GetProperty("id").GetGuid());
            Assert.Equal("panel-narrative-sequence", blueprint.GetProperty("key").GetString());
            Assert.Equal("Panel narrative sequence", blueprint.GetProperty("displayName").GetString());
            Assert.Equal("storyboard", blueprint.GetProperty("category").GetString());
            Assert.Equal("panel_sequence", blueprint.GetProperty("sequenceMode").GetString());
            Assert.Contains("same main character", blueprint.GetProperty("consistencySummary").GetString());
            Assert.Equal("alternate camera distance", blueprint.GetProperty("variationSummary").GetString());

            var manifestCsv = await File.ReadAllTextAsync(result.ManifestCsvPath, CancellationToken.None);
            Assert.Contains("outputArtifactId", manifestCsv);
            Assert.Contains("sourceAssetIds", manifestCsv);
            Assert.Contains("evidenceAnchorIds", manifestCsv);
            Assert.Contains(outputArtifactId.ToString(), manifestCsv);
            Assert.Contains(sourceAssetId.ToString(), manifestCsv);
            Assert.Contains(evidenceAnchorId.ToString(), manifestCsv);
            Assert.Contains("operatorRunIds", manifestCsv);
            Assert.Contains(operatorRunId.ToString(), manifestCsv);
            Assert.Contains("finalReviewer", manifestCsv);
            Assert.Contains("Ready for classroom delivery.", manifestCsv);
            Assert.Contains("blueprintConsistencySummary", manifestCsv);
            Assert.Contains("panel-narrative-sequence", manifestCsv);
            Assert.Contains("same main character; consistent scene grammar", manifestCsv);
            Assert.Contains("alternate camera distance", manifestCsv);

            var reviewReport = await File.ReadAllTextAsync(result.ReviewReportPath, CancellationToken.None);
            Assert.Contains("reviewer=Teacher", reviewReport);
            Assert.Contains("notes=Ready for classroom delivery.", reviewReport);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task DeliveryPackageWriter_RejectsDuplicateNormalizedKeys()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        var sourceDirectory = Path.Combine(rootDirectory, "source");
        var packageDirectory = Path.Combine(rootDirectory, "delivery");
        Directory.CreateDirectory(sourceDirectory);

        try
        {
            var firstImage = Path.Combine(sourceDirectory, "first.png");
            var secondImage = Path.Combine(sourceDirectory, "second.png");
            await File.WriteAllBytesAsync(firstImage, [1, 2, 3], CancellationToken.None);
            await File.WriteAllBytesAsync(secondImage, [4, 5, 6], CancellationToken.None);

            var writer = new DeliveryPackageWriter();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                writer.WriteAsync(
                    new DeliveryPackageRequest(
                        "Duplicate key project",
                        packageDirectory,
                        [
                            new DeliveryPackageItem(
                                "Cover Shot",
                                "Cover A",
                                firstImage,
                                Path.Combine(sourceDirectory, "first.json"),
                                "Prompt A",
                                ReviewDecision.Pass,
                                HumanApproved: true),
                            new DeliveryPackageItem(
                                "cover-shot",
                                "Cover B",
                                secondImage,
                                Path.Combine(sourceDirectory, "second.json"),
                                "Prompt B",
                                ReviewDecision.Pass,
                                HumanApproved: true),
                        ]),
                    CancellationToken.None));

            Assert.Contains("duplicate", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task DeliveryPackageWriter_FailureKeepsExistingPackageUntouched()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        var sourceDirectory = Path.Combine(rootDirectory, "source");
        var packageDirectory = Path.Combine(rootDirectory, "delivery");
        Directory.CreateDirectory(sourceDirectory);

        try
        {
            var existingImagesDirectory = Path.Combine(packageDirectory, "images");
            Directory.CreateDirectory(existingImagesDirectory);
            var existingImagePath = Path.Combine(existingImagesDirectory, "cover.png");
            await File.WriteAllBytesAsync(existingImagePath, [9, 9, 9], CancellationToken.None);
            await File.WriteAllTextAsync(
                Path.Combine(packageDirectory, "manifest.json"),
                """{"projectName":"Existing package"}""",
                CancellationToken.None);

            var validImage = Path.Combine(sourceDirectory, "fresh.png");
            await File.WriteAllBytesAsync(validImage, [1, 2, 3], CancellationToken.None);
            var missingReportPath = Path.Combine(sourceDirectory, "missing-report.json");
            var writer = new DeliveryPackageWriter();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                writer.WriteAsync(
                    new DeliveryPackageRequest(
                        "Broken package",
                        packageDirectory,
                        [
                            new DeliveryPackageItem(
                                "cover",
                                "Cover",
                                validImage,
                                Path.Combine(sourceDirectory, "missing-metadata.json"),
                                "Prompt",
                                ReviewDecision.Pass,
                                HumanApproved: true,
                                DeterministicCompositionReportPath: missingReportPath),
                        ]),
                    CancellationToken.None));

            Assert.True(File.Exists(existingImagePath));
            Assert.Equal([9, 9, 9], await File.ReadAllBytesAsync(existingImagePath, CancellationToken.None));

            var siblingEntries = Directory.GetDirectories(rootDirectory)
                .Select(Path.GetFileName)
                .Where(name => name is not null)
                .ToArray();
            Assert.DoesNotContain(siblingEntries, name => name!.Contains(".delivery.", StringComparison.OrdinalIgnoreCase));
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
