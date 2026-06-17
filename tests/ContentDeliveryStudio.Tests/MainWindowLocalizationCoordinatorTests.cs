using System.Globalization;
using ContentDeliveryStudio.Application.Localization;
using ContentDeliveryStudio.App.ViewModels;
using ContentDeliveryStudio.Core.Documents;
using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.Tests;

public sealed class MainWindowLocalizationCoordinatorTests
{
    [Fact]
    public void BuildPayload_ReturnsLocalizedShellAndOptionPayload()
    {
        var localizationService = new LocalizationService(() => new CultureInfo("en-US"));
        localizationService.SetLanguage(LanguagePreference.Chinese);
        var coordinator = new MainWindowLocalizationCoordinator(localizationService);

        var payload = coordinator.BuildPayload();

        Assert.Equal("AI 内容交付工作台", payload.AppTitle);
        Assert.Equal("语言", payload.LanguageLabel);
        Assert.Equal(["工作区", "项目", "设置"], payload.NavigationItems);
        Assert.Equal("需求设计", payload.WorkbenchTabs.First(tab => tab.Kind is WorkbenchTabKind.Brief).Title);
        Assert.Equal("图视图", payload.WorkbenchTabs.First(tab => tab.Kind is WorkbenchTabKind.Graph).Title);
        Assert.Equal(
            [
                localizationService.GetText(LocalizationKey.GenericHostStarted),
                localizationService.GetText(LocalizationKey.FakeProvidersRegistered),
                localizationService.GetText(LocalizationKey.NoRealApiCalls),
            ],
            payload.ActivityItems);
        Assert.Equal(["跟随系统", "中文", "英文"], payload.LanguageOptions.Select(option => option.DisplayName));
        Assert.Contains(
            payload.DocumentStrictnessOptions,
            option => option.Value is IllustrationStrictnessLevel.ScholarlyDraft && option.DisplayName == "学术草稿");
        Assert.Equal("默认编辑风格指南", Assert.Single(payload.StyleGuideOptions).Name);
        Assert.Equal("假标准 PNG", Assert.Single(payload.GenerationRecipeOptions).DisplayName);
        Assert.NotEmpty(payload.ImageTypePresetOptions);
    }

    [Fact]
    public void ResolveDocumentDefaults_ReusesTypedValuesAndRestoresLocalizedDefaults()
    {
        var coordinator = new MainWindowLocalizationCoordinator(new LocalizationService(() => new CultureInfo("en-US")));

        var restored = coordinator.ResolveDocumentDefaults(
            currentSourceText: "",
            currentAudience: "teachers",
            previousSourceTextDefault: "old source",
            previousAudienceDefault: "teachers",
            currentSourceTextDefault: "new source",
            currentAudienceDefault: "new audience");

        Assert.Equal("new source", restored.SourceText);
        Assert.Equal("new audience", restored.Audience);

        var preserved = coordinator.ResolveDocumentDefaults(
            currentSourceText: "custom source",
            currentAudience: "custom audience",
            previousSourceTextDefault: "old source",
            previousAudienceDefault: "old audience",
            currentSourceTextDefault: "new source",
            currentAudienceDefault: "new audience");

        Assert.Equal("custom source", preserved.SourceText);
        Assert.Equal("custom audience", preserved.Audience);
    }

    [Fact]
    public void SelectDocumentStrictnessOption_RestoresRequestedValueOrFallsBackToEducational()
    {
        var coordinator = new MainWindowLocalizationCoordinator(new LocalizationService(() => new CultureInfo("en-US")));
        var options = new[]
        {
            new DocumentStrictnessOptionViewModel(IllustrationStrictnessLevel.Editorial, "Editorial"),
            new DocumentStrictnessOptionViewModel(IllustrationStrictnessLevel.Educational, "Educational"),
        };

        Assert.Equal(
            IllustrationStrictnessLevel.Editorial,
            coordinator.SelectDocumentStrictnessOption(options, IllustrationStrictnessLevel.Editorial).Value);
        Assert.Equal(
            IllustrationStrictnessLevel.Educational,
            coordinator.SelectDocumentStrictnessOption(options, IllustrationStrictnessLevel.ScholarlyDraft).Value);
    }

    [Fact]
    public void RestoreSelectionState_RestoresLocalizedDefaultsAndExistingSelections()
    {
        var localizationService = new LocalizationService(() => new CultureInfo("en-US"));
        localizationService.SetLanguage(LanguagePreference.Chinese);
        var coordinator = new MainWindowLocalizationCoordinator(localizationService);
        var payload = coordinator.BuildPayload();

        var restored = coordinator.RestoreSelectionState(
            payload,
            currentDocumentSourceText: "",
            currentDocumentAudience: "teachers",
            previousDocumentSourceDefault: "old source",
            previousDocumentAudienceDefault: "teachers",
            selectedStrictness: IllustrationStrictnessLevel.ScholarlyDraft,
            selectedImageTypePresetId: payload.ImageTypePresetOptions.First().Id,
            selectedStyleGuideId: payload.StyleGuideOptions.First().Id,
            selectedGenerationRecipeId: payload.GenerationRecipeOptions.First().Id,
            selectedLanguagePreference: LanguagePreference.Chinese);

        Assert.Equal(payload.DefaultDocumentSourceText, restored.DocumentSourceText);
        Assert.Equal(payload.DefaultDocumentAudience, restored.DocumentAudience);
        Assert.Equal(IllustrationStrictnessLevel.ScholarlyDraft, restored.SelectedDocumentStrictnessOption.Value);
        Assert.Equal(payload.ImageTypePresetOptions.First().Id, restored.SelectedImageTypePresetOption?.Id);
        Assert.Equal(payload.StyleGuideOptions.First().Id, restored.SelectedStyleGuideOption?.Id);
        Assert.Equal(payload.GenerationRecipeOptions.First().Id, restored.SelectedGenerationRecipeOption?.Id);
        Assert.Equal(LanguagePreference.Chinese, restored.SelectedLanguageOption.Preference);
    }
}
