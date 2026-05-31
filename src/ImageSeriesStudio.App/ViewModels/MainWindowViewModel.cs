using CommunityToolkit.Mvvm.ComponentModel;

namespace ImageSeriesStudio.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    public string AppTitle { get; } = "AI Image Series Studio";

    public string ProviderMode { get; } = "Fake providers";

    public IReadOnlyList<string> NavigationItems { get; } =
    [
        "Workspaces",
        "Projects",
        "Settings",
    ];

    public IReadOnlyList<WorkbenchTabViewModel> WorkbenchTabs { get; } =
    [
        new("Brief", "Project brief and constraints will appear here."),
        new("Plan", "Series plan and item list will appear here."),
        new("Prompts", "Prompt versions will appear here."),
        new("Queue", "Generation and review queue state will appear here."),
        new("Gallery", "Generated candidates will appear here."),
        new("Review", "Rubrics, scores, and decisions will appear here."),
        new("Delivery", "Final package status will appear here."),
    ];

    public string InspectorSummary { get; } =
        "No item selected. The Phase 1 shell is wired to fake providers only.";

    public IReadOnlyList<string> ActivityItems { get; } =
    [
        "Generic Host started.",
        "Text, image, and vision providers are registered as fakes.",
        "No real API calls are enabled.",
    ];
}

public sealed record WorkbenchTabViewModel(string Title, string EmptyState);
