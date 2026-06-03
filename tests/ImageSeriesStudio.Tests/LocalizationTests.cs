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
                ["DefaultDocumentSourceText"] = "Teachers need a clear concept diagram for the central idea.",
                ["DefaultDocumentAudience"] = "teachers",
                ["DocumentStrictnessEditorial"] = "Editorial review",
                ["DocumentStrictnessEducational"] = "Educational use",
                ["DocumentStrictnessScholarlyDraft"] = "Scholarly draft",
                ["DocumentPastedSourceSection"] = "Pasted source text",
                ["DocumentReadableTextConstraint"] = "Use deterministic post-render text for readable labels and callouts.",
                ["DocumentPlanningResultTemplate"] = "Approved targets: {0}.",
            },
            [LanguagePreference.Chinese] = new Dictionary<string, string>
            {
                ["DocumentIllustrationTitle"] = "文稿配图",
                ["DocumentSourceText"] = "来源文本",
                ["DocumentAudience"] = "受众",
                ["DocumentStrictness"] = "严格度",
                ["RunFakeDocumentPlanning"] = "运行假文稿规划",
                ["DocumentPlanningResult"] = "文稿规划结果",
                ["DefaultDocumentSourceText"] = "教师需要一张清晰的核心概念图。",
                ["DefaultDocumentAudience"] = "教师",
                ["DocumentStrictnessEditorial"] = "编辑审阅",
                ["DocumentStrictnessEducational"] = "教学使用",
                ["DocumentStrictnessScholarlyDraft"] = "学术草稿",
                ["DocumentPastedSourceSection"] = "粘贴来源文本",
                ["DocumentReadableTextConstraint"] = "需要可读标签和说明时，使用确定性后期排版文字。",
                ["DocumentPlanningResultTemplate"] = "已批准目标：{0} 个。",
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
    public void LocalizationService_ReturnsLocalizedSeriesItemKindText()
    {
        var service = new LocalizationService();

        service.SetLanguage(LanguagePreference.Chinese);
        Assert.Equal("类型", service.GetText(LocalizationKey.PlanKindColumn));
        Assert.Equal("面板", service.GetSeriesItemKindText(SeriesItemKind.Panel));

        service.SetLanguage(LanguagePreference.English);
        Assert.Equal("Kind", service.GetText(LocalizationKey.PlanKindColumn));
        Assert.Equal("Keyframe", service.GetSeriesItemKindText(SeriesItemKind.Keyframe));
    }

    [Fact]
    public void LocalizationService_ReturnsReviewRouteColumnText()
    {
        var service = new LocalizationService();

        service.SetLanguage(LanguagePreference.English);
        Assert.Equal("Repair route", service.GetText(LocalizationKey.ReviewRouteColumn));

        service.SetLanguage(LanguagePreference.Chinese);
        Assert.Equal("修复路由", service.GetText(LocalizationKey.ReviewRouteColumn));
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
        Assert.Equal("Generate blueprints", service.GetText(LocalizationKey.GenerateDesignBlueprints));
        Assert.Equal("Promote blueprint", service.GetText(LocalizationKey.PromoteDesignBlueprint));
        Assert.Equal("Blueprint routes", service.GetText(LocalizationKey.BlueprintRoutesHeader));
        Assert.Contains("blueprint routes", service.GetText(LocalizationKey.NoBlueprintRows));
        Assert.Equal("Promoted", service.GetText(LocalizationKey.BlueprintPromoted));
        Assert.Equal("Generate directions", service.GetText(LocalizationKey.GeneratePromptDirections));
        Assert.Equal("Promote direction", service.GetText(LocalizationKey.PromotePromptDirection));
        Assert.Contains("recommendation warnings", service.GetText(LocalizationKey.NoPromptDirectionRows));

        service.SetLanguage(LanguagePreference.Chinese);
        Assert.Equal("需求设计", service.GetText(LocalizationKey.Brief));
        Assert.Equal("目标", service.GetText(LocalizationKey.BriefGoal));
        Assert.Equal("创建设计简报", service.GetText(LocalizationKey.CreateBrief));
        Assert.Equal("生成蓝图候选", service.GetText(LocalizationKey.GenerateDesignBlueprints));
        Assert.Equal("晋级为蓝图路线", service.GetText(LocalizationKey.PromoteDesignBlueprint));
        Assert.Equal("蓝图路线", service.GetText(LocalizationKey.BlueprintRoutesHeader));
        Assert.Contains("蓝图路线", service.GetText(LocalizationKey.NoBlueprintRows));
        Assert.Equal("已晋级", service.GetText(LocalizationKey.BlueprintPromoted));
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
        Assert.Equal("Source text", service.GetText(LocalizationKey.DocumentSourceText));
        Assert.Equal("Run fake document planning", service.GetText(LocalizationKey.RunFakeDocumentPlanning));

        service.SetLanguage(LanguagePreference.Chinese);
        Assert.Equal("文稿配图", service.GetText(LocalizationKey.DocumentIllustrationTitle));
        Assert.Equal("来源文本", service.GetText(LocalizationKey.DocumentSourceText));
        Assert.Equal("运行假文稿规划", service.GetText(LocalizationKey.RunFakeDocumentPlanning));
    }

    [Fact]
    public void LocalizationService_ReturnsFinalApprovalText()
    {
        var service = new LocalizationService();

        service.SetLanguage(LanguagePreference.English);
        Assert.Equal("Human approval", service.GetText(LocalizationKey.HumanApprovalColumn));
        Assert.Equal("Reviewer", service.GetText(LocalizationKey.FinalApprovalReviewer));
        Assert.Equal("Pending human approval", service.GetText(LocalizationKey.HumanApprovalPending));
        Assert.Equal("Human approved", service.GetText(LocalizationKey.HumanApprovalApproved));
        Assert.Equal("Human rejected", service.GetText(LocalizationKey.HumanApprovalRejected));

        service.SetLanguage(LanguagePreference.Chinese);
        Assert.Equal("人工批准", service.GetText(LocalizationKey.HumanApprovalColumn));
        Assert.Equal("审核人", service.GetText(LocalizationKey.FinalApprovalReviewer));
        Assert.Equal("等待人工批准", service.GetText(LocalizationKey.HumanApprovalPending));
        Assert.Equal("人工已批准", service.GetText(LocalizationKey.HumanApprovalApproved));
        Assert.Equal("人工已拒绝", service.GetText(LocalizationKey.HumanApprovalRejected));
    }
}
