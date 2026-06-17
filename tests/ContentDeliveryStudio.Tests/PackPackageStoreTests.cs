using ContentDeliveryStudio.Application.Packs;
using ContentDeliveryStudio.Core.Packs;
using ContentDeliveryStudio.Infrastructure.Packs;

namespace ContentDeliveryStudio.Tests;

public sealed class PackPackageStoreTests
{
    [Fact]
    public async Task PackPackageStore_ExportsImportsAndValidatesPackRegistry()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        var packagePath = Path.Combine(rootDirectory, "packs.json");
        Directory.CreateDirectory(rootDirectory);

        try
        {
            var timestamp = DateTimeOffset.Parse("2026-06-03T15:00:00Z");
            var registry = BuiltInPackCatalog.CreateGenericImageSeriesRegistry("1.5.0", timestamp);
            var package = PackPackage.FromRegistry("Generic image-series starter packs", timestamp, registry);
            var store = new JsonPackPackageStore();

            await store.ExportAsync(package, packagePath, CancellationToken.None);
            var imported = await store.ImportAsync(packagePath, "1.5.0", CancellationToken.None);
            var importedRegistry = imported.CreateRegistry("1.5.0");
            var workflow = importedRegistry.GetRequired<WorkflowPack>(BuiltInPackCatalog.GenericImageSeriesWorkflowPackId);

            Assert.True(File.Exists(packagePath));
            Assert.Equal(PackPackage.CurrentSchemaVersion, imported.SchemaVersion);
            Assert.Equal("Generic image-series starter packs", imported.Name);
            Assert.Equal(5, importedRegistry.Packs.Count);
            Assert.Contains(WorkflowViewSlotIds.StageWorkspace, workflow.UiDefaults.ViewSlots.Select(slot => slot.SlotId));
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
    public async Task PackPackageStore_RejectsImportedPackWithMissingBlueprintReference()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        var packagePath = Path.Combine(rootDirectory, "bad-packs.json");
        Directory.CreateDirectory(rootDirectory);

        try
        {
            await File.WriteAllTextAsync(
                packagePath,
                """
                {
                  "schemaVersion": "pack-package.v1",
                  "name": "Bad pack package",
                  "exportedAt": "2026-06-03T15:00:00+00:00",
                  "workflowPacks": [
                    {
                      "metadata": {
                        "id": "bad-workflow",
                        "displayName": "Bad Workflow",
                        "version": { "major": 1, "minor": 0, "patch": 0 },
                        "compatibility": {
                          "minimumAppVersion": { "major": 1, "minor": 0, "patch": 0 },
                          "maximumAppVersion": { "major": 2, "minor": 0, "patch": 0 }
                        },
                        "lifecycleState": "active",
                        "migrationNotes": [],
                        "createdAt": "2026-06-03T15:00:00+00:00"
                      },
                      "stageDefinitions": [
                        {
                          "id": "Source",
                          "displayName": "Source",
                          "completionCriteria": ["Source evidence is attached."],
                          "required": true
                        }
                      ],
                      "blueprintPackIds": ["missing-blueprints"],
                      "uiDefaults": {
                        "defaultStageId": "Source",
                        "viewSlots": [
                          {
                            "slotId": "StageWorkspace",
                            "stageId": "Source",
                            "visibleByDefault": true,
                            "order": 0
                          }
                        ]
                      }
                    }
                  ],
                  "blueprintPacks": [],
                  "industryPacks": [],
                  "rendererPacks": [],
                  "reviewRubricPacks": []
                }
                """,
                CancellationToken.None);
            var store = new JsonPackPackageStore();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                store.ImportAsync(packagePath, "1.5.0", CancellationToken.None));
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
