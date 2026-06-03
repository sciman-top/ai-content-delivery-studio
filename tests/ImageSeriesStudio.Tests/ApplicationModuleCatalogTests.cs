using ImageSeriesStudio.Application.Modules;

namespace ImageSeriesStudio.Tests;

public sealed class ApplicationModuleCatalogTests
{
    [Fact]
    public void ApplicationModuleCatalog_DefinesPhase12ModuleFolders()
    {
        var modules = ApplicationModuleCatalog.BuiltInModules;

        Assert.Equal(5, modules.Count);
        AssertModule(
            ApplicationModuleCatalog.GetRequired("source-ingestion"),
            "source-ingestion",
            "src/ImageSeriesStudio.Application/Sources",
            "src/ImageSeriesStudio.Core/Sources",
            "src/ImageSeriesStudio.Infrastructure/Sources",
            ["SourceIngestion", "DocumentExtraction"]);
        AssertModule(
            ApplicationModuleCatalog.GetRequired("artifact-planning"),
            "artifact-planning",
            "src/ImageSeriesStudio.Application/Artifacts",
            "src/ImageSeriesStudio.Core/Artifacts",
            "src/ImageSeriesStudio.Infrastructure/Artifacts",
            ["ArtifactPlanning"]);
        AssertModule(
            ApplicationModuleCatalog.GetRequired("pack-registry"),
            "pack-registry",
            "src/ImageSeriesStudio.Application/Packs",
            "src/ImageSeriesStudio.Core/Packs",
            "src/ImageSeriesStudio.Infrastructure/Packs",
            ["PackPackage", "PackRegistry"]);
        AssertModule(
            ApplicationModuleCatalog.GetRequired("repair-routing"),
            "repair-routing",
            "src/ImageSeriesStudio.Application/Repairs",
            "src/ImageSeriesStudio.Core/Projects",
            null,
            ["ReviewOutcomeRouting", "PromptRepairSuggestion"]);
        AssertModule(
            ApplicationModuleCatalog.GetRequired("tool-adapters"),
            "tool-adapters",
            "src/ImageSeriesStudio.Application/ToolAdapters",
            null,
            "src/ImageSeriesStudio.Infrastructure/ToolAdapters",
            ["ToolAdapterRegistry"]);
    }

    [Fact]
    public void ApplicationModuleCatalog_RejectsDuplicateIdsAndEscapingFolders()
    {
        var module = ApplicationModuleDefinition.Create(
            "source-ingestion",
            "Source Ingestion",
            "src/ImageSeriesStudio.Application/Sources",
            "src/ImageSeriesStudio.Core/Sources",
            "src/ImageSeriesStudio.Infrastructure/Sources",
            ["SourceIngestion"]);

        Assert.Throws<ArgumentException>(() => ApplicationModuleCatalog.Create([module, module]));
        Assert.Throws<ArgumentException>(() =>
            ApplicationModuleDefinition.Create(
                "bad-module",
                "Bad Module",
                "../outside",
                null,
                null,
                ["BadUseCase"]));
    }

    private static void AssertModule(
        ApplicationModuleDefinition module,
        string id,
        string applicationFolder,
        string? coreFolder,
        string? infrastructureFolder,
        IReadOnlyList<string> useCaseNames)
    {
        Assert.Equal(id, module.Id);
        Assert.Equal(applicationFolder, module.ApplicationFolder);
        Assert.Equal(coreFolder, module.CoreFolder);
        Assert.Equal(infrastructureFolder, module.InfrastructureFolder);
        Assert.Equal(useCaseNames, module.UseCaseNames);
    }
}
