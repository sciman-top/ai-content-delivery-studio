using ImageSeriesStudio.Application.Workflows;
using ImageSeriesStudio.Core.Experiments;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.References;
using ImageSeriesStudio.Core.Styles;
using ImageSeriesStudio.Infrastructure.Workflows;

namespace ImageSeriesStudio.Tests;

public sealed class WorkflowPackageTests
{
    [Fact]
    public async Task WorkflowPackageStore_ExportsAndImportsProviderNeutralWorkflow()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var packagePath = Path.Combine(rootDirectory, "workflow.json");
        Directory.CreateDirectory(rootDirectory);

        try
        {
            var timestamp = new DateTimeOffset(2026, 6, 2, 16, 0, 0, TimeSpan.Zero);
            var seriesId = Guid.NewGuid();
            var providerProfileId = Guid.NewGuid();
            var referenceSet = ReferenceImageSet.Create(Guid.NewGuid(), "Physics references", timestamp);
            referenceSet.AddImage(
                "references\\style\\chalkboard.png",
                ReferenceImageRole.Style,
                "Keep chalk texture.",
                timestamp.AddMinutes(1));
            var styleGuide = StyleGuide.Create(
                seriesId,
                "Physics chalk",
                ["clear hierarchy"],
                ["cyan", "gold"],
                ["soft rim light"],
                ["leave label space"],
                ["chalk texture"],
                ["no unreadable text"],
                [referenceSet.Id],
                timestamp);
            var recipe = GenerationRecipe.Create(
                providerProfileId,
                "fake-image-v1",
                ImageTypePresetCatalog.EducationalPoster,
                1024,
                1280,
                "draft",
                "png",
                ImageBackgroundMode.Opaque,
                compression: null,
                ImageModerationMode.Auto,
                seed: 7,
                ["verify text space"]);
            var package = WorkflowPackage.Create(
                "Physics poster workflow",
                timestamp,
                [WorkflowStyleGuide.FromStyleGuide(styleGuide)],
                [WorkflowGenerationRecipe.FromGenerationRecipe(recipe)],
                [WorkflowReferenceImageSet.FromReferenceImageSet(referenceSet)],
                [
                    WorkflowParameterExperimentDefinition.Create(
                        "Palette sweep",
                        "Create a poster background with {{palette}}.",
                        new GenerationSettings(1024, 1280, "draft", "png", seed: 7),
                        [new ParameterGridAxis("palette", ["cyan gold", "mono blue"])],
                        recipe.Id),
                ]);
            var store = new JsonWorkflowPackageStore();

            await store.ExportAsync(package, packagePath, CancellationToken.None);
            var imported = await store.ImportAsync(packagePath, CancellationToken.None);

            Assert.True(File.Exists(packagePath));
            Assert.Equal(WorkflowPackage.CurrentSchemaVersion, imported.SchemaVersion);
            Assert.Equal("Physics poster workflow", imported.Name);
            Assert.Equal("Physics chalk", Assert.Single(imported.StyleGuides).Name);
            Assert.Equal(ImageTypePresetCatalog.EducationalPoster, Assert.Single(imported.GenerationRecipes).ImageTypePresetId);
            Assert.Equal("references/style/chalkboard.png", Assert.Single(Assert.Single(imported.ReferenceImageSets).Images).AssetPath);
            Assert.Equal("palette", Assert.Single(Assert.Single(imported.ParameterExperiments).Axes).Name);
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
    public async Task WorkflowPackageStore_RejectsImportedReferencePathsThatEscapeWorkspace()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var packagePath = Path.Combine(rootDirectory, "bad-workflow.json");
        Directory.CreateDirectory(rootDirectory);

        try
        {
            await File.WriteAllTextAsync(
                packagePath,
                """
                {
                  "schemaVersion": "workflow-package.v1",
                  "name": "Bad workflow",
                  "exportedAt": "2026-06-02T16:00:00+00:00",
                  "styleGuides": [],
                  "generationRecipes": [],
                  "referenceImageSets": [
                    {
                      "id": "11111111-1111-1111-1111-111111111111",
                      "name": "Bad references",
                      "images": [
                        {
                          "assetPath": "../outside.png",
                          "role": "Style",
                          "description": "escape"
                        }
                      ]
                    }
                  ],
                  "parameterExperiments": []
                }
                """,
                CancellationToken.None);
            var store = new JsonWorkflowPackageStore();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                store.ImportAsync(packagePath, CancellationToken.None));
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
