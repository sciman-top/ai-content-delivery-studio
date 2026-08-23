using System.Globalization;
using ContentDeliveryStudio.Application.Localization;
using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.Tests;

public sealed class LocalizationTests
{
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
    public void LocalizationService_PinsRepresentativeUserVisibleText()
    {
        var service = new LocalizationService();

        var pinnedText = new Dictionary<LanguagePreference, (LocalizationKey Key, string Expected)>
        {
            [LanguagePreference.Chinese] = (LocalizationKey.AppTitle, "AI 内容交付工作台"),
            [LanguagePreference.English] = (LocalizationKey.AppTitle, "AI Content Delivery Studio"),
        };
        foreach (var (preference, (key, expected)) in pinnedText)
        {
            service.SetLanguage(preference);
            Assert.Equal(expected, service.GetText(key));
        }

        service.SetLanguage(LanguagePreference.Chinese);
        Assert.Equal("等待人工批准", service.GetText(LocalizationKey.HumanApprovalPending));
        Assert.Equal("草稿", service.GetSeriesItemStatusText(SeriesItemStatus.Draft));
        Assert.Equal("面板", service.GetSeriesItemKindText(SeriesItemKind.Panel));
        Assert.Equal("风格与配方", service.GetText(LocalizationKey.StyleRecipeInspector));

        service.SetLanguage(LanguagePreference.English);
        Assert.Equal("Pending human approval", service.GetText(LocalizationKey.HumanApprovalPending));
        Assert.Equal("Needs review", service.GetSeriesItemStatusText(SeriesItemStatus.NeedsReview));
        Assert.Equal("Keyframe", service.GetSeriesItemKindText(SeriesItemKind.Keyframe));
        Assert.Equal("Style and recipe", service.GetText(LocalizationKey.StyleRecipeInspector));
    }
}
