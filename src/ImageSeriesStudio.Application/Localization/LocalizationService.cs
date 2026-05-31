using System.Globalization;

namespace ImageSeriesStudio.Application.Localization;

public sealed class LocalizationService
{
    private static readonly IReadOnlyDictionary<LocalizationKey, string> English = new Dictionary<LocalizationKey, string>
    {
        [LocalizationKey.AppTitle] = "AI Image Series Studio",
        [LocalizationKey.ProviderModeFake] = "Fake providers",
        [LocalizationKey.Workspace] = "Workspace",
        [LocalizationKey.Workspaces] = "Workspaces",
        [LocalizationKey.Projects] = "Projects",
        [LocalizationKey.Settings] = "Settings",
        [LocalizationKey.Brief] = "Brief",
        [LocalizationKey.BriefEmptyState] = "Project brief and constraints will appear here.",
        [LocalizationKey.Plan] = "Plan",
        [LocalizationKey.PlanEmptyState] = "Series plan and item list will appear here.",
        [LocalizationKey.Prompts] = "Prompts",
        [LocalizationKey.PromptsEmptyState] = "Prompt versions will appear here.",
        [LocalizationKey.Queue] = "Queue",
        [LocalizationKey.QueueEmptyState] = "Generation and review queue state will appear here.",
        [LocalizationKey.Gallery] = "Gallery",
        [LocalizationKey.GalleryEmptyState] = "Generated candidates will appear here.",
        [LocalizationKey.Review] = "Review",
        [LocalizationKey.ReviewEmptyState] = "Rubrics, scores, and decisions will appear here.",
        [LocalizationKey.Delivery] = "Delivery",
        [LocalizationKey.DeliveryEmptyState] = "Final package status will appear here.",
        [LocalizationKey.Inspector] = "Inspector",
        [LocalizationKey.Activity] = "Activity",
        [LocalizationKey.NoItemSelected] = "No item selected. The Phase 2 shell is wired to fake providers only.",
        [LocalizationKey.GenericHostStarted] = "Generic Host started.",
        [LocalizationKey.FakeProvidersRegistered] = "Text, image, and vision providers are registered as fakes.",
        [LocalizationKey.NoRealApiCalls] = "No real API calls are enabled.",
        [LocalizationKey.LanguageLabel] = "Language",
        [LocalizationKey.LanguageSystem] = "System",
        [LocalizationKey.LanguageChinese] = "Chinese",
        [LocalizationKey.LanguageEnglish] = "English",
    };

    private static readonly IReadOnlyDictionary<LocalizationKey, string> Chinese = new Dictionary<LocalizationKey, string>
    {
        [LocalizationKey.AppTitle] = "AI 图像系列工作台",
        [LocalizationKey.ProviderModeFake] = "假 Provider",
        [LocalizationKey.Workspace] = "工作区",
        [LocalizationKey.Workspaces] = "工作区",
        [LocalizationKey.Projects] = "项目",
        [LocalizationKey.Settings] = "设置",
        [LocalizationKey.Brief] = "简报",
        [LocalizationKey.BriefEmptyState] = "项目简报和约束将显示在这里。",
        [LocalizationKey.Plan] = "计划",
        [LocalizationKey.PlanEmptyState] = "系列计划和条目列表将显示在这里。",
        [LocalizationKey.Prompts] = "提示词",
        [LocalizationKey.PromptsEmptyState] = "提示词版本将显示在这里。",
        [LocalizationKey.Queue] = "队列",
        [LocalizationKey.QueueEmptyState] = "生成和评审队列状态将显示在这里。",
        [LocalizationKey.Gallery] = "图库",
        [LocalizationKey.GalleryEmptyState] = "生成候选图将显示在这里。",
        [LocalizationKey.Review] = "评审",
        [LocalizationKey.ReviewEmptyState] = "评分规则、分数和决策将显示在这里。",
        [LocalizationKey.Delivery] = "交付",
        [LocalizationKey.DeliveryEmptyState] = "最终交付包状态将显示在这里。",
        [LocalizationKey.Inspector] = "检查器",
        [LocalizationKey.Activity] = "活动",
        [LocalizationKey.NoItemSelected] = "尚未选择条目。Phase 2 shell 仅接入假 Provider。",
        [LocalizationKey.GenericHostStarted] = "Generic Host 已启动。",
        [LocalizationKey.FakeProvidersRegistered] = "文本、图像和视觉评审 Provider 均注册为假实现。",
        [LocalizationKey.NoRealApiCalls] = "未启用任何真实 API 调用。",
        [LocalizationKey.LanguageLabel] = "语言",
        [LocalizationKey.LanguageSystem] = "跟随系统",
        [LocalizationKey.LanguageChinese] = "中文",
        [LocalizationKey.LanguageEnglish] = "英文",
    };

    private readonly Func<CultureInfo> _currentCulture;

    public LocalizationService()
        : this(() => CultureInfo.CurrentUICulture)
    {
    }

    public LocalizationService(Func<CultureInfo> currentCulture)
    {
        _currentCulture = currentCulture;
        SetLanguage(LanguagePreference.System);
    }

    public LanguagePreference Preference { get; private set; }

    public SupportedLanguage CurrentLanguage { get; private set; }

    public void SetLanguage(LanguagePreference preference)
    {
        Preference = preference;
        CurrentLanguage = ResolveLanguage(preference);
    }

    public string GetText(LocalizationKey key)
    {
        var catalog = CurrentLanguage is SupportedLanguage.Chinese ? Chinese : English;
        return catalog.TryGetValue(key, out var text) ? text : English[key];
    }

    private SupportedLanguage ResolveLanguage(LanguagePreference preference)
    {
        return preference switch
        {
            LanguagePreference.Chinese => SupportedLanguage.Chinese,
            LanguagePreference.English => SupportedLanguage.English,
            _ => ResolveSystemLanguage(_currentCulture()),
        };
    }

    private static SupportedLanguage ResolveSystemLanguage(CultureInfo culture)
    {
        return culture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
            ? SupportedLanguage.Chinese
            : SupportedLanguage.English;
    }
}

public enum LanguagePreference
{
    System = 0,
    Chinese = 1,
    English = 2,
}

public enum SupportedLanguage
{
    Chinese = 0,
    English = 1,
}

public enum LocalizationKey
{
    AppTitle,
    ProviderModeFake,
    Workspace,
    Workspaces,
    Projects,
    Settings,
    Brief,
    BriefEmptyState,
    Plan,
    PlanEmptyState,
    Prompts,
    PromptsEmptyState,
    Queue,
    QueueEmptyState,
    Gallery,
    GalleryEmptyState,
    Review,
    ReviewEmptyState,
    Delivery,
    DeliveryEmptyState,
    Inspector,
    Activity,
    NoItemSelected,
    GenericHostStarted,
    FakeProvidersRegistered,
    NoRealApiCalls,
    LanguageLabel,
    LanguageSystem,
    LanguageChinese,
    LanguageEnglish,
}
