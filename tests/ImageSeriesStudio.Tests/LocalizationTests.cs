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

    [Fact]
    public void LocalizationService_ReturnsBriefStudioText()
    {
        var service = new LocalizationService();

        service.SetLanguage(LanguagePreference.English);
        Assert.Equal("Brief", service.GetText(LocalizationKey.Brief));
        Assert.Equal("Goal", service.GetText(LocalizationKey.BriefGoal));
        Assert.Equal("Create brief", service.GetText(LocalizationKey.CreateBrief));
        Assert.Equal("Generate directions", service.GetText(LocalizationKey.GeneratePromptDirections));
        Assert.Equal("Promote direction", service.GetText(LocalizationKey.PromotePromptDirection));
        Assert.Contains("recommendation warnings", service.GetText(LocalizationKey.NoPromptDirectionRows));

        service.SetLanguage(LanguagePreference.Chinese);
        Assert.Equal("需求设计", service.GetText(LocalizationKey.Brief));
        Assert.Equal("目标", service.GetText(LocalizationKey.BriefGoal));
        Assert.Equal("创建设计简报", service.GetText(LocalizationKey.CreateBrief));
        Assert.Equal("生成提示词方向", service.GetText(LocalizationKey.GeneratePromptDirections));
        Assert.Equal("晋级为提示词版本", service.GetText(LocalizationKey.PromotePromptDirection));
        Assert.Contains("推荐风险", service.GetText(LocalizationKey.NoPromptDirectionRows));
    }

    [Fact]
    public void LocalizationService_ReturnsDocumentIllustrationText()
    {
        var service = new LocalizationService();

        service.SetLanguage(LanguagePreference.English);
        Assert.Equal("Document illustration", service.GetText(LocalizationKey.DocumentIllustrationTitle));
        Assert.Equal("Document text", service.GetText(LocalizationKey.DocumentSourceText));
        Assert.Equal("Run fake document planning", service.GetText(LocalizationKey.RunFakeDocumentPlanning));

        service.SetLanguage(LanguagePreference.Chinese);
        Assert.Equal("文稿配图", service.GetText(LocalizationKey.DocumentIllustrationTitle));
        Assert.Equal("文稿文本", service.GetText(LocalizationKey.DocumentSourceText));
        Assert.Equal("运行假文稿配图规划", service.GetText(LocalizationKey.RunFakeDocumentPlanning));
    }
}
