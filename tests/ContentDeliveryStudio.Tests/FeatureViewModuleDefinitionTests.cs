using ContentDeliveryStudio.Application.Modules;
using ContentDeliveryStudio.Core.Packs;

namespace ContentDeliveryStudio.Tests;

public sealed class FeatureViewModuleDefinitionTests
{
    [Fact]
    public void FeatureViewModuleDefinition_CapturesWpfViewModelLocalizationCommandsAndFakeTests()
    {
        var module = FeatureViewModuleDefinition.Create(
            "source-attachment-panel",
            "source-ingestion",
            WorkflowViewSlotIds.SourceList,
            "ContentDeliveryStudio.App.Views.SourceListView",
            "ContentDeliveryStudio.App.ViewModels.SourceListViewModel",
            ["Sources.Title", "Sources.AttachButton"],
            ["AttachSource", "ExtractDocument"],
            ["SourceIngestionWorkflowTests", "DocumentExtractionProviderTests"]);

        Assert.Equal("source-attachment-panel", module.Id);
        Assert.Equal("source-ingestion", module.ApplicationModuleId);
        Assert.Equal(WorkflowViewSlotIds.SourceList, module.ViewSlotId);
        Assert.Equal("ContentDeliveryStudio.App.Views.SourceListView", module.ViewTypeName);
        Assert.Equal("ContentDeliveryStudio.App.ViewModels.SourceListViewModel", module.ViewModelTypeName);
        Assert.Equal(["Sources.Title", "Sources.AttachButton"], module.LocalizationKeys);
        Assert.Equal(["AttachSource", "ExtractDocument"], module.CommandNames);
        Assert.Equal(["SourceIngestionWorkflowTests", "DocumentExtractionProviderTests"], module.FakeServiceTestNames);
    }

    [Fact]
    public void FeatureViewModuleDefinition_RejectsUnknownModulesSlotsAndMissingFakeTests()
    {
        Assert.Throws<InvalidOperationException>(() =>
            FeatureViewModuleDefinition.Create(
                "unknown-module",
                "missing-module",
                WorkflowViewSlotIds.Inspector,
                "ContentDeliveryStudio.App.Views.MissingView",
                "ContentDeliveryStudio.App.ViewModels.MissingViewModel",
                ["Missing.Title"],
                ["Run"],
                ["MissingFakeTest"]));
        Assert.Throws<ArgumentException>(() =>
            FeatureViewModuleDefinition.Create(
                "bad-slot",
                "source-ingestion",
                "PermanentGlobalTab",
                "ContentDeliveryStudio.App.Views.BadView",
                "ContentDeliveryStudio.App.ViewModels.BadViewModel",
                ["Bad.Title"],
                ["Run"],
                ["BadFakeTest"]));
        Assert.Throws<ArgumentException>(() =>
            FeatureViewModuleDefinition.Create(
                "no-fake-test",
                "source-ingestion",
                WorkflowViewSlotIds.ActivityPanel,
                "ContentDeliveryStudio.App.Views.SourceActivityView",
                "ContentDeliveryStudio.App.ViewModels.SourceActivityViewModel",
                ["Sources.Activity"],
                ["RefreshActivity"],
                []));
    }
}
