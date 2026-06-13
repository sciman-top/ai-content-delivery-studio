using ImageSeriesStudio.Core.Documents;

namespace ImageSeriesStudio.App.ViewModels;

public sealed class WorkbenchInspectorCoordinator
{
    private readonly ProjectWorkspaceCoordinator _projectWorkspaceCoordinator;
    private readonly PlanningWorkflowCoordinator _planningWorkflowCoordinator;
    private readonly GenerationWorkflowCoordinator _generationWorkflowCoordinator;

    public WorkbenchInspectorCoordinator(
        ProjectWorkspaceCoordinator projectWorkspaceCoordinator,
        PlanningWorkflowCoordinator planningWorkflowCoordinator,
        GenerationWorkflowCoordinator generationWorkflowCoordinator)
    {
        _projectWorkspaceCoordinator = projectWorkspaceCoordinator;
        _planningWorkflowCoordinator = planningWorkflowCoordinator;
        _generationWorkflowCoordinator = generationWorkflowCoordinator;
    }

    public Task<ProjectWorkspaceResult> CreateProjectAsync(
        string projectName,
        CancellationToken cancellationToken)
    {
        return _projectWorkspaceCoordinator.CreateProjectAsync(projectName, cancellationToken);
    }

    public async Task<WorkbenchInspectorDocumentPlanningResult> RunFakeDocumentPlanningAsync(
        ProjectSummaryViewModel selectedProject,
        string sourceText,
        string? audience,
        IllustrationStrictnessLevel strictness,
        string defaultDocumentAudience,
        IReadOnlyList<string> currentActivityItems,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selectedProject);

        var result = await _planningWorkflowCoordinator.RunFakeDocumentPlanningAsync(
            selectedProject.Id,
            selectedProject.Name,
            sourceText,
            audience,
            strictness,
            defaultDocumentAudience,
            cancellationToken);
        var workspace = await _projectWorkspaceCoordinator.RefreshProjectsAsync(
            selectedProject.Id,
            cancellationToken);

        return new WorkbenchInspectorDocumentPlanningResult(
            workspace,
            result.SeriesId,
            result.ResultSummary,
            currentActivityItems.Concat([result.ResultSummary]).ToArray());
    }

    public async Task<WorkbenchInspectorImageEditResult> RunFakeImageEditAsync(
        Guid projectId,
        GalleryRowViewModel selectedRow,
        string editPrompt,
        string? maskPath,
        string imageEditResultText,
        IReadOnlyList<GalleryRowViewModel> currentGalleryRows,
        IReadOnlyList<string> currentActivityItems,
        CancellationToken cancellationToken)
    {
        var editedRow = await _generationWorkflowCoordinator.RunFakeImageEditAsync(
            projectId,
            selectedRow,
            editPrompt,
            maskPath,
            cancellationToken);

        return new WorkbenchInspectorImageEditResult(
            [.. currentGalleryRows, editedRow],
            editedRow,
            [imageEditResultText, .. currentActivityItems]);
    }

    public Task RefreshProviderCenterAsync(
        ProviderCenterViewModel providerCenter,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(providerCenter);

        return providerCenter.RefreshAsync(cancellationToken);
    }

    public Task CheckProviderHealthAsync(
        ProviderCenterViewModel providerCenter,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(providerCenter);

        return providerCenter.CheckHealthAsync(cancellationToken);
    }
}

public sealed record WorkbenchInspectorDocumentPlanningResult(
    ProjectWorkspaceResult Workspace,
    Guid? SeriesId,
    string ResultSummary,
    IReadOnlyList<string> ActivityItems);

public sealed record WorkbenchInspectorImageEditResult(
    IReadOnlyList<GalleryRowViewModel> GalleryRows,
    GalleryRowViewModel SelectedGalleryRow,
    IReadOnlyList<string> ActivityItems);
