using System.Globalization;
using ImageSeriesStudio.Core.Projects;

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
        [LocalizationKey.ProjectName] = "Project name",
        [LocalizationKey.NewProjectNamePlaceholder] = "Untitled image series",
        [LocalizationKey.CreateProject] = "Create project",
        [LocalizationKey.AvailableProjects] = "Available projects",
        [LocalizationKey.CurrentProject] = "Current project",
        [LocalizationKey.NoProjectLoaded] = "No project loaded.",
        [LocalizationKey.PlanEditor] = "Plan editor",
        [LocalizationKey.SeriesTitle] = "Series title",
        [LocalizationKey.SeriesDescription] = "Series description",
        [LocalizationKey.CreateSeries] = "Create series",
        [LocalizationKey.AvailableSeries] = "Available series",
        [LocalizationKey.ItemTitle] = "Item title",
        [LocalizationKey.ItemBrief] = "Item brief",
        [LocalizationKey.AddItem] = "Add item",
        [LocalizationKey.SeriesItems] = "Series items",
        [LocalizationKey.NoSeriesSelected] = "No series selected.",
        [LocalizationKey.PlanSeriesColumn] = "Series",
        [LocalizationKey.PlanItemColumn] = "Item",
        [LocalizationKey.PlanBriefColumn] = "Brief",
        [LocalizationKey.PlanStatusColumn] = "Status",
        [LocalizationKey.NoPlanRows] = "Create a project, then add a series and items from the inspector.",
        [LocalizationKey.NoItemsInSeries] = "No items in this series yet.",
        [LocalizationKey.StatusDraft] = "Draft",
        [LocalizationKey.StatusReady] = "Ready",
        [LocalizationKey.StatusGenerating] = "Generating",
        [LocalizationKey.StatusNeedsReview] = "Needs review",
        [LocalizationKey.StatusApproved] = "Approved",
        [LocalizationKey.StatusDelivered] = "Delivered",
        [LocalizationKey.PromptEditor] = "Prompt editor",
        [LocalizationKey.SelectedItem] = "Selected item",
        [LocalizationKey.PromptText] = "Prompt text",
        [LocalizationKey.DefaultGenerationSettings] = "Default: 1024x1024, standard, png",
        [LocalizationKey.CreatePromptVersion] = "Create prompt version",
        [LocalizationKey.PromptHistory] = "Prompt history",
        [LocalizationKey.PromptVersionColumn] = "Version",
        [LocalizationKey.PromptItemColumn] = "Item",
        [LocalizationKey.PromptTextColumn] = "Prompt",
        [LocalizationKey.PromptSettingsColumn] = "Settings",
        [LocalizationKey.PromptCreatedColumn] = "Created",
        [LocalizationKey.NoPromptRows] = "Select an item and create a prompt version from the inspector.",
        [LocalizationKey.NoItemSelectedForPrompt] = "Select a series item before creating a prompt version.",
        [LocalizationKey.FakePlanningTitle] = "Fake planning",
        [LocalizationKey.PlanningGoal] = "Goal",
        [LocalizationKey.PlanningAudience] = "Audience",
        [LocalizationKey.PlanningItemCount] = "Item count",
        [LocalizationKey.PlanningStyleBrief] = "Style brief",
        [LocalizationKey.RunFakePlanning] = "Run fake planning",
        [LocalizationKey.DefaultPlanningAudience] = "creators",
        [LocalizationKey.DefaultPlanningStyleBrief] = "clean editorial style",
        [LocalizationKey.RunFakeGeneration] = "Run fake generation",
        [LocalizationKey.QueueItemColumn] = "Item",
        [LocalizationKey.QueueStatusColumn] = "Status",
        [LocalizationKey.QueueAttemptsColumn] = "Attempts",
        [LocalizationKey.QueueOutputColumn] = "Output",
        [LocalizationKey.QueueErrorColumn] = "Error",
        [LocalizationKey.NoQueueRows] = "Create prompt versions, then run fake generation from the inspector.",
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
        [LocalizationKey.ProjectName] = "项目名称",
        [LocalizationKey.NewProjectNamePlaceholder] = "未命名图像系列",
        [LocalizationKey.CreateProject] = "创建项目",
        [LocalizationKey.AvailableProjects] = "可用项目",
        [LocalizationKey.CurrentProject] = "当前项目",
        [LocalizationKey.NoProjectLoaded] = "尚未加载项目。",
        [LocalizationKey.PlanEditor] = "计划编辑",
        [LocalizationKey.SeriesTitle] = "系列标题",
        [LocalizationKey.SeriesDescription] = "系列说明",
        [LocalizationKey.CreateSeries] = "创建系列",
        [LocalizationKey.AvailableSeries] = "可用系列",
        [LocalizationKey.ItemTitle] = "条目标题",
        [LocalizationKey.ItemBrief] = "条目简述",
        [LocalizationKey.AddItem] = "添加条目",
        [LocalizationKey.SeriesItems] = "系列条目",
        [LocalizationKey.NoSeriesSelected] = "尚未选择系列。",
        [LocalizationKey.PlanSeriesColumn] = "系列",
        [LocalizationKey.PlanItemColumn] = "条目",
        [LocalizationKey.PlanBriefColumn] = "简述",
        [LocalizationKey.PlanStatusColumn] = "状态",
        [LocalizationKey.NoPlanRows] = "先创建项目，再从检查器添加系列和条目。",
        [LocalizationKey.NoItemsInSeries] = "该系列尚未添加条目。",
        [LocalizationKey.StatusDraft] = "草稿",
        [LocalizationKey.StatusReady] = "就绪",
        [LocalizationKey.StatusGenerating] = "生成中",
        [LocalizationKey.StatusNeedsReview] = "待评审",
        [LocalizationKey.StatusApproved] = "已通过",
        [LocalizationKey.StatusDelivered] = "已交付",
        [LocalizationKey.PromptEditor] = "提示词编辑",
        [LocalizationKey.SelectedItem] = "已选条目",
        [LocalizationKey.PromptText] = "提示词文本",
        [LocalizationKey.DefaultGenerationSettings] = "默认：1024x1024，standard，png",
        [LocalizationKey.CreatePromptVersion] = "创建提示词版本",
        [LocalizationKey.PromptHistory] = "提示词历史",
        [LocalizationKey.PromptVersionColumn] = "版本",
        [LocalizationKey.PromptItemColumn] = "条目",
        [LocalizationKey.PromptTextColumn] = "提示词",
        [LocalizationKey.PromptSettingsColumn] = "设置",
        [LocalizationKey.PromptCreatedColumn] = "创建时间",
        [LocalizationKey.NoPromptRows] = "请先选择条目，再从检查器创建提示词版本。",
        [LocalizationKey.NoItemSelectedForPrompt] = "创建提示词版本前需要先选择系列条目。",
        [LocalizationKey.FakePlanningTitle] = "假规划",
        [LocalizationKey.PlanningGoal] = "目标",
        [LocalizationKey.PlanningAudience] = "受众",
        [LocalizationKey.PlanningItemCount] = "条目数量",
        [LocalizationKey.PlanningStyleBrief] = "风格简述",
        [LocalizationKey.RunFakePlanning] = "运行假规划",
        [LocalizationKey.DefaultPlanningAudience] = "创作者",
        [LocalizationKey.DefaultPlanningStyleBrief] = "干净的编辑风格",
        [LocalizationKey.RunFakeGeneration] = "运行假生成",
        [LocalizationKey.QueueItemColumn] = "条目",
        [LocalizationKey.QueueStatusColumn] = "状态",
        [LocalizationKey.QueueAttemptsColumn] = "尝试次数",
        [LocalizationKey.QueueOutputColumn] = "输出",
        [LocalizationKey.QueueErrorColumn] = "错误",
        [LocalizationKey.NoQueueRows] = "先创建提示词版本，再从检查器运行假生成。",
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

    public string GetSeriesItemStatusText(SeriesItemStatus status)
    {
        return status switch
        {
            SeriesItemStatus.Draft => GetText(LocalizationKey.StatusDraft),
            SeriesItemStatus.Ready => GetText(LocalizationKey.StatusReady),
            SeriesItemStatus.Generating => GetText(LocalizationKey.StatusGenerating),
            SeriesItemStatus.NeedsReview => GetText(LocalizationKey.StatusNeedsReview),
            SeriesItemStatus.Approved => GetText(LocalizationKey.StatusApproved),
            SeriesItemStatus.Delivered => GetText(LocalizationKey.StatusDelivered),
            _ => status.ToString(),
        };
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
    ProjectName,
    NewProjectNamePlaceholder,
    CreateProject,
    AvailableProjects,
    CurrentProject,
    NoProjectLoaded,
    PlanEditor,
    SeriesTitle,
    SeriesDescription,
    CreateSeries,
    AvailableSeries,
    ItemTitle,
    ItemBrief,
    AddItem,
    SeriesItems,
    NoSeriesSelected,
    PlanSeriesColumn,
    PlanItemColumn,
    PlanBriefColumn,
    PlanStatusColumn,
    NoPlanRows,
    NoItemsInSeries,
    StatusDraft,
    StatusReady,
    StatusGenerating,
    StatusNeedsReview,
    StatusApproved,
    StatusDelivered,
    PromptEditor,
    SelectedItem,
    PromptText,
    DefaultGenerationSettings,
    CreatePromptVersion,
    PromptHistory,
    PromptVersionColumn,
    PromptItemColumn,
    PromptTextColumn,
    PromptSettingsColumn,
    PromptCreatedColumn,
    NoPromptRows,
    NoItemSelectedForPrompt,
    FakePlanningTitle,
    PlanningGoal,
    PlanningAudience,
    PlanningItemCount,
    PlanningStyleBrief,
    RunFakePlanning,
    DefaultPlanningAudience,
    DefaultPlanningStyleBrief,
    RunFakeGeneration,
    QueueItemColumn,
    QueueStatusColumn,
    QueueAttemptsColumn,
    QueueOutputColumn,
    QueueErrorColumn,
    NoQueueRows,
}
