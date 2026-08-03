using ContentDeliveryStudio.Core.Packs;
using ContentDeliveryStudio.Infrastructure.Packs;

namespace ContentDeliveryStudio.Tests;

public sealed class PackPackageStoreTests
{
    [Fact]
    public async Task ImportAsync_LoadsLegacyV1ScenarioProfileWithoutClaimingRuntimeComposition()
    {
        var packagePath = await WritePackageAsync(CreateLegacyPackageJson(includeBlueprint: true));

        try
        {
            var package = await new JsonPackPackageStore()
                .ImportAsync(packagePath, "1.5.0", CancellationToken.None);
            var registry = package.CreateRegistry("1.5.0");
            var workflow = registry.GetRequired<WorkflowPack>("legacy-image-series");

            Assert.Equal("pack-package.v1", package.SchemaVersion);
            Assert.Equal(["article-illustration"], workflow.ScenarioIds);
            Assert.Equal(["legacy-blueprints"], workflow.BlueprintPackIds);
            Assert.Equal("Brief", workflow.UiDefaults.DefaultStageId);
            Assert.Contains(
                workflow.UiDefaults.ViewSlots,
                slot => slot.SlotId == WorkflowViewSlotIds.StageWorkspace && slot.StageId == "Brief");
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(packagePath)!, recursive: true);
        }
    }

    [Fact]
    public async Task ImportAsync_RejectsLegacyV1PackageWithMissingBlueprintReference()
    {
        var packagePath = await WritePackageAsync(CreateLegacyPackageJson(includeBlueprint: false));

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new JsonPackPackageStore().ImportAsync(packagePath, "1.5.0", CancellationToken.None));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(packagePath)!, recursive: true);
        }
    }

    private static async Task<string> WritePackageAsync(string json)
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "ContentDeliveryStudio.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "legacy-pack.v1.json");
        await File.WriteAllTextAsync(path, json);
        return path;
    }

    private static string CreateLegacyPackageJson(bool includeBlueprint)
    {
        var blueprint = includeBlueprint
            ? """
              {
                "metadata": {
                  "id": "legacy-blueprints",
                  "displayName": "Legacy Blueprints",
                  "version": { "major": 1, "minor": 0, "patch": 0 },
                  "compatibility": {
                    "minimumAppVersion": { "major": 1, "minor": 0, "patch": 0 },
                    "maximumAppVersion": { "major": 2, "minor": 0, "patch": 0 }
                  },
                  "lifecycleState": "active",
                  "migrationNotes": [],
                  "createdAt": "2026-06-03T15:00:00+00:00"
                },
                "blueprintIds": ["article-illustration"]
              }
              """
            : string.Empty;

        return $$"""
          {
            "schemaVersion": "pack-package.v1",
            "name": "Legacy scenario profile",
            "exportedAt": "2026-06-03T15:00:00+00:00",
            "workflowPacks": [
              {
                "metadata": {
                  "id": "legacy-image-series",
                  "displayName": "Legacy Image Series",
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
                    "id": "Brief",
                    "displayName": "Brief",
                    "completionCriteria": ["Brief is ready."],
                    "required": true
                  }
                ],
                "blueprintPackIds": ["legacy-blueprints"],
                "uiDefaults": {
                  "defaultStageId": "Brief",
                  "viewSlots": [
                    {
                      "slotId": "StageWorkspace",
                      "stageId": "Brief",
                      "visibleByDefault": true,
                      "order": 0
                    }
                  ]
                },
                "scenarioIds": ["article-illustration"],
                "industryPackIds": [],
                "rendererPackIds": [],
                "reviewRubricPackIds": []
              }
            ],
            "blueprintPacks": [{{blueprint}}],
            "industryPacks": [],
            "rendererPacks": [],
            "reviewRubricPacks": []
          }
          """;
    }
}
