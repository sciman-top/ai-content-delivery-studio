using System.Globalization;
using ImageSeriesStudio.Application.Localization;
using ImageSeriesStudio.Core.Projects;

namespace ImageSeriesStudio.Tests;

public sealed class LocalizationTests
{
    [Fact]
    public void LocalizationService_ReturnsChineseAndEnglishShellText()
    {
        var service = new LocalizationService();

        service.SetLanguage(LanguagePreference.Chinese);
        Assert.Equal("AI 图像系列工作台", service.GetText(LocalizationKey.AppTitle));
        Assert.Equal("假 Provider", service.GetText(LocalizationKey.ProviderModeFake));

        service.SetLanguage(LanguagePreference.English);
        Assert.Equal("AI Image Series Studio", service.GetText(LocalizationKey.AppTitle));
        Assert.Equal("Fake providers", service.GetText(LocalizationKey.ProviderModeFake));
    }

    [Fact]
    public void LocalizationService_ReturnsDocumentIllustrationEntryTextForBothLanguages()
    {
        var service = new LocalizationService();
        var expectedTexts = new Dictionary<LanguagePreference, IReadOnlyDictionary<string, string>>
        {
            [LanguagePreference.English] = new Dictionary<string, string>
            {
                ["DocumentIllustrationTitle"] = "Document illustration",
                ["DocumentSourceText"] = "Source text",
                ["DocumentAudience"] = "Audience",
                ["DocumentStrictness"] = "Strictness",
                ["RunFakeDocumentPlanning"] = "Run fake document planning",
                ["DocumentPlanningResult"] = "Document planning result",
            },
            [LanguagePreference.Chinese] = new Dictionary<string, string>
            {
                ["DocumentIllustrationTitle"] = "文稿配图",
                ["DocumentSourceText"] = "来源文本",
                ["DocumentAudience"] = "受众",
                ["DocumentStrictness"] = "严格度",
                ["RunFakeDocumentPlanning"] = "运行假文稿规划",
                ["DocumentPlanningResult"] = "文稿规划结果",
            },
        };
        var definedKeys = Enum.GetNames<LocalizationKey>();

        foreach (var keyName in expectedTexts.SelectMany(pair => pair.Value.Keys).Distinct())
        {
            Assert.Contains(keyName, definedKeys);
        }

        foreach (var (preference, texts) in expectedTexts)
        {
            service.SetLanguage(preference);

            foreach (var (keyName, expectedText) in texts)
            {
                var key = Enum.Parse<LocalizationKey>(keyName);

                Assert.Equal(expectedText, service.GetText(key));
            }
        }
    }

    [Fact]
    public void LocalizationService_ResolvesSystemLanguageFromCulture()
    {
        var service = new LocalizationService(() => new CultureInfo("zh-CN"));

        service.SetLanguage(LanguagePreference.System);

        Assert.Equal(SupportedLanguage.Chinese, service.CurrentLanguage);
        Assert.Equal("工作区", service.GetText(LocalizationKey.Workspace));
    }

    [Fact]
    public void LocalizationService_FallsBackToEnglishForUnsupportedSystemCulture()
    {
        var service = new LocalizationService(() => new CultureInfo("fr-FR"));

        service.SetLanguage(LanguagePreference.System);

        Assert.Equal(SupportedLanguage.English, service.CurrentLanguage);
        Assert.Equal("Workspace", service.GetText(LocalizationKey.Workspace));
    }

    [Fact]
    public void LocalizationService_CoversAllKeysForBothLanguages()
    {
        var service = new LocalizationService();

        foreach (var preference in new[] { LanguagePreference.Chinese, LanguagePreference.English })
        {
            service.SetLanguage(preference);

            foreach (var key in Enum.GetValues<LocalizationKey>())
            {
                Assert.False(string.IsNullOrWhiteSpace(service.GetText(key)));
            }
        }
    }

    [Fact]
    public void LocalizationService_ReturnsLocalizedSeriesItemStatusText()
    {
        var service = new LocalizationService();

        service.SetLanguage(LanguagePreference.Chinese);
        Assert.Equal("草稿", service.GetSeriesItemStatusText(SeriesItemStatus.Draft));

        service.SetLanguage(LanguagePreference.English);
        Assert.Equal("Needs review", service.GetSeriesItemStatusText(SeriesItemStatus.NeedsReview));
    }

    [Fact]
    public void LocalizationService_ReturnsStyleRecipeInspectorText()
    {
        var service = new LocalizationService();

        service.SetLanguage(LanguagePreference.English);
        Assert.Equal("Style and recipe", service.GetText(LocalizationKey.StyleRecipeInspector));
        Assert.Equal("Image type", service.GetText(LocalizationKey.ImageTypePreset));

        service.SetLanguage(LanguagePreference.Chinese);
        Assert.Equal("风格与配方", service.GetText(LocalizationKey.StyleRecipeInspector));
        Assert.Equal("图片类型", service.GetText(LocalizationKey.ImageTypePreset));
    }
}
