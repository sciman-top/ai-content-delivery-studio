using CommunityToolkit.Mvvm.ComponentModel;
using ImageSeriesStudio.Application.Localization;

namespace ImageSeriesStudio.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly LocalizationService _localizationService;

    private string _appTitle = string.Empty;
    private string _providerMode = string.Empty;
    private string _workspaceHeader = string.Empty;
    private string _inspectorTitle = string.Empty;
    private string _activityTitle = string.Empty;
    private string _inspectorSummary = string.Empty;
    private string _languageLabel = string.Empty;
    private IReadOnlyList<string> _navigationItems = [];
    private IReadOnlyList<WorkbenchTabViewModel> _workbenchTabs = [];
    private IReadOnlyList<string> _activityItems = [];
    private IReadOnlyList<LanguageOptionViewModel> _languageOptions = [];
    private LanguageOptionViewModel? _selectedLanguageOption;

    public MainWindowViewModel(LocalizationService localizationService)
    {
        _localizationService = localizationService;
        RefreshLocalizedText();
        SelectedLanguageOption = LanguageOptions.First(option => option.Preference == _localizationService.Preference);
    }

    public string AppTitle
    {
        get => _appTitle;
        private set => SetProperty(ref _appTitle, value);
    }

    public string ProviderMode
    {
        get => _providerMode;
        private set => SetProperty(ref _providerMode, value);
    }

    public string WorkspaceHeader
    {
        get => _workspaceHeader;
        private set => SetProperty(ref _workspaceHeader, value);
    }

    public string InspectorTitle
    {
        get => _inspectorTitle;
        private set => SetProperty(ref _inspectorTitle, value);
    }

    public string ActivityTitle
    {
        get => _activityTitle;
        private set => SetProperty(ref _activityTitle, value);
    }

    public string InspectorSummary
    {
        get => _inspectorSummary;
        private set => SetProperty(ref _inspectorSummary, value);
    }

    public string LanguageLabel
    {
        get => _languageLabel;
        private set => SetProperty(ref _languageLabel, value);
    }

    public IReadOnlyList<string> NavigationItems
    {
        get => _navigationItems;
        private set => SetProperty(ref _navigationItems, value);
    }

    public IReadOnlyList<WorkbenchTabViewModel> WorkbenchTabs
    {
        get => _workbenchTabs;
        private set => SetProperty(ref _workbenchTabs, value);
    }

    public IReadOnlyList<string> ActivityItems
    {
        get => _activityItems;
        private set => SetProperty(ref _activityItems, value);
    }

    public IReadOnlyList<LanguageOptionViewModel> LanguageOptions
    {
        get => _languageOptions;
        private set => SetProperty(ref _languageOptions, value);
    }

    public LanguageOptionViewModel? SelectedLanguageOption
    {
        get => _selectedLanguageOption;
        set
        {
            if (value is null || !SetProperty(ref _selectedLanguageOption, value))
            {
                return;
            }

            _localizationService.SetLanguage(value.Preference);
            RefreshLocalizedText();
        }
    }

    private void RefreshLocalizedText()
    {
        var previousPreference = SelectedLanguageOption?.Preference ?? _localizationService.Preference;

        AppTitle = Text(LocalizationKey.AppTitle);
        ProviderMode = Text(LocalizationKey.ProviderModeFake);
        WorkspaceHeader = Text(LocalizationKey.Workspace);
        InspectorTitle = Text(LocalizationKey.Inspector);
        ActivityTitle = Text(LocalizationKey.Activity);
        InspectorSummary = Text(LocalizationKey.NoItemSelected);
        LanguageLabel = Text(LocalizationKey.LanguageLabel);
        NavigationItems =
        [
            Text(LocalizationKey.Workspaces),
            Text(LocalizationKey.Projects),
            Text(LocalizationKey.Settings),
        ];
        WorkbenchTabs =
        [
            new(Text(LocalizationKey.Brief), Text(LocalizationKey.BriefEmptyState)),
            new(Text(LocalizationKey.Plan), Text(LocalizationKey.PlanEmptyState)),
            new(Text(LocalizationKey.Prompts), Text(LocalizationKey.PromptsEmptyState)),
            new(Text(LocalizationKey.Queue), Text(LocalizationKey.QueueEmptyState)),
            new(Text(LocalizationKey.Gallery), Text(LocalizationKey.GalleryEmptyState)),
            new(Text(LocalizationKey.Review), Text(LocalizationKey.ReviewEmptyState)),
            new(Text(LocalizationKey.Delivery), Text(LocalizationKey.DeliveryEmptyState)),
        ];
        ActivityItems =
        [
            Text(LocalizationKey.GenericHostStarted),
            Text(LocalizationKey.FakeProvidersRegistered),
            Text(LocalizationKey.NoRealApiCalls),
        ];
        LanguageOptions =
        [
            new(LanguagePreference.System, Text(LocalizationKey.LanguageSystem)),
            new(LanguagePreference.Chinese, Text(LocalizationKey.LanguageChinese)),
            new(LanguagePreference.English, Text(LocalizationKey.LanguageEnglish)),
        ];

        _selectedLanguageOption = LanguageOptions.First(option => option.Preference == previousPreference);
        OnPropertyChanged(nameof(SelectedLanguageOption));
    }

    private string Text(LocalizationKey key)
    {
        return _localizationService.GetText(key);
    }
}

public sealed record WorkbenchTabViewModel(string Title, string EmptyState);

public sealed record LanguageOptionViewModel(LanguagePreference Preference, string DisplayName);
