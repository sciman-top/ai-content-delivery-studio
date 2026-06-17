using ContentDeliveryStudio.Application.Modules;

namespace ContentDeliveryStudio.Tests;

public sealed class ApplicationModuleCatalogTests
{
    [Fact]
    public void ApplicationModuleCatalog_DefinesPhase12ModuleFolders()
    {
        var modules = ApplicationModuleCatalog.BuiltInModules;

        Assert.Equal(6, modules.Count);
        AssertModule(
            ApplicationModuleCatalog.GetRequired("source-ingestion"),
            "source-ingestion",
            "src/ContentDeliveryStudio.Application/Sources",
            "src/ContentDeliveryStudio.Core/Sources",
            "src/ContentDeliveryStudio.Infrastructure/Sources",
            ["SourceIngestion", "DocumentExtraction"]);
        AssertModule(
            ApplicationModuleCatalog.GetRequired("artifact-planning"),
            "artifact-planning",
            "src/ContentDeliveryStudio.Application/Artifacts",
            "src/ContentDeliveryStudio.Core/Artifacts",
            "src/ContentDeliveryStudio.Infrastructure/Artifacts",
            ["ArtifactPlanning"]);
        AssertModule(
            ApplicationModuleCatalog.GetRequired("pack-registry"),
            "pack-registry",
            "src/ContentDeliveryStudio.Application/Packs",
            "src/ContentDeliveryStudio.Core/Packs",
            "src/ContentDeliveryStudio.Infrastructure/Packs",
            ["PackPackage", "PackRegistry"]);
        AssertModule(
            ApplicationModuleCatalog.GetRequired("repair-routing"),
            "repair-routing",
            "src/ContentDeliveryStudio.Application/RepairRouting",
            "src/ContentDeliveryStudio.Core/Projects",
            null,
            [
                "ReviewOutcomeRouting",
                "PromptRepairSuggestion",
                "RoutedRepairPatchProposal",
                "RoutedRepairPatchApplication",
            ]);
        AssertModule(
            ApplicationModuleCatalog.GetRequired("tool-adapters"),
            "tool-adapters",
            "src/ContentDeliveryStudio.Application/ToolAdapters",
            null,
            null,
            ["ToolAdapterRegistry"]);
        AssertModule(
            ApplicationModuleCatalog.GetRequired("remote-workflows"),
            "remote-workflows",
            "src/ContentDeliveryStudio.Application/RemoteWorkflows",
            null,
            "src/ContentDeliveryStudio.Infrastructure/RemoteWorkflows",
            ["RemoteWorkflowEngineAdapter"]);
    }

    [Fact]
    public void ApplicationModuleCatalog_RejectsDuplicateIdsAndEscapingFolders()
    {
        var module = ApplicationModuleDefinition.Create(
            "source-ingestion",
            "Source Ingestion",
            "src/ContentDeliveryStudio.Application/Sources",
            "src/ContentDeliveryStudio.Core/Sources",
            "src/ContentDeliveryStudio.Infrastructure/Sources",
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

    [Fact]
    public void ApplicationModuleCatalog_BuiltInModuleFoldersExist()
    {
        var repositoryRoot = FindRepositoryRoot();

        foreach (var module in ApplicationModuleCatalog.BuiltInModules)
        {
            AssertFolderExists(repositoryRoot, module.ApplicationFolder);

            if (module.CoreFolder is not null)
            {
                AssertFolderExists(repositoryRoot, module.CoreFolder);
            }

            if (module.InfrastructureFolder is not null)
            {
                AssertFolderExists(repositoryRoot, module.InfrastructureFolder);
            }
        }
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

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ContentDeliveryStudio.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find ContentDeliveryStudio.sln from the test output path.");
    }

    private static void AssertFolderExists(string repositoryRoot, string repositoryRelativeFolder)
    {
        var folderPath = Path.Combine(
            repositoryRoot,
            repositoryRelativeFolder.Replace('/', Path.DirectorySeparatorChar));

        Assert.True(Directory.Exists(folderPath), $"Expected module folder to exist: {repositoryRelativeFolder}");
    }
}
