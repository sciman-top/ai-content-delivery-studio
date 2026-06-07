using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Core.Projects;

namespace ImageSeriesStudio.App.ViewModels;

public sealed class PlanEditorWorkflowCoordinator
{
    private readonly ProjectApplicationService _projectService;

    public PlanEditorWorkflowCoordinator(ProjectApplicationService projectService)
    {
        _projectService = projectService;
    }

    public async Task<Guid> CreateSeriesAsync(
        Guid projectId,
        string title,
        string description,
        CancellationToken cancellationToken)
    {
        var series = await _projectService.AddSeriesAsync(
            projectId,
            title.Trim(),
            description.Trim(),
            DateTimeOffset.UtcNow,
            cancellationToken);

        return series.Id;
    }

    public async Task<Guid> AddItemAsync(
        Guid projectId,
        Guid seriesId,
        string title,
        string brief,
        CancellationToken cancellationToken)
    {
        var item = await _projectService.AddItemAsync(
            projectId,
            seriesId,
            title.Trim(),
            brief.Trim(),
            DateTimeOffset.UtcNow,
            cancellationToken);

        return item.Id;
    }

    public async Task<Guid> CreatePromptVersionAsync(
        Guid projectId,
        Guid seriesItemId,
        string promptText,
        CancellationToken cancellationToken)
    {
        var prompt = await _projectService.AddPromptVersionAsync(
            projectId,
            seriesItemId,
            promptText.Trim(),
            CreateDefaultGenerationSettings(),
            providerProfileId: null,
            DateTimeOffset.UtcNow,
            cancellationToken);

        return prompt.Id;
    }

    private static GenerationSettings CreateDefaultGenerationSettings()
    {
        return new GenerationSettings(1024, 1024, "standard", "png");
    }
}
