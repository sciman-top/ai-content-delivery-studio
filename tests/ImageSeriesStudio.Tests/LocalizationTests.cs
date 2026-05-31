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
}
