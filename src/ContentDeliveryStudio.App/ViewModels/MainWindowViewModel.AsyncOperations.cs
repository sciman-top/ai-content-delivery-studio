namespace ContentDeliveryStudio.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    internal Task BackgroundTask => _operationGate.BackgroundTask;

    internal bool IsMutatingOperationActive => _isMutatingOperationActive;

    internal Task RefreshProjectsAsync(Guid? selectedProjectId = null)
    {
        return _operationGate.RunLatestWinsAsync(
            MainWindowOperationLane.ProjectRefresh,
            cancellationToken => RefreshProjectsCoreAsync(selectedProjectId, cancellationToken));
    }

    private void ScheduleStartupRefresh()
    {
        _operationGate.RunBackgroundLatestWins(
            MainWindowOperationLane.ProjectRefresh,
            cancellationToken => RefreshProjectsCoreAsync(selectedProjectId: null, cancellationToken));
    }

    private void QueueSelectedProjectLoad(ProjectSummaryViewModel? project)
    {
        if (_suppressSelectedProjectLoad)
        {
            return;
        }

        if (project is null)
        {
            _operationGate.RunBackgroundLatestWins(
                MainWindowOperationLane.PlanLoad,
                cancellationToken => ClearPlanAsync(cancellationToken));
            return;
        }

        _operationGate.RunBackgroundLatestWins(
            MainWindowOperationLane.PlanLoad,
            cancellationToken => LoadPlanAsync(project.Id, selectedSeriesId: null, selectedItemId: null, cancellationToken));
    }

    private void QueueGalleryWarmup(IEnumerable<string> assetPaths)
    {
        ArgumentNullException.ThrowIfNull(assetPaths);

        var warmupPaths = assetPaths.Take(24).ToArray();
        _operationGate.RunBackgroundLatestWins(
            MainWindowOperationLane.GalleryWarmup,
            cancellationToken => warmupPaths.Length == 0
                ? Task.CompletedTask
                : _galleryThumbnailWarmupService.WarmupAsync(warmupPaths, cancellationToken));
    }

    private void SetExclusiveBusyState(bool isBusy)
    {
        _isMutatingOperationActive = isBusy;
        NotifyMutationBusyStateChanged();
    }

    private void NotifyMutationBusyStateChanged()
    {
        CreateProjectCommand.NotifyCanExecuteChanged();
        RunFakePlanningCommand.NotifyCanExecuteChanged();
        RunFakeDocumentPlanningCommand.NotifyCanExecuteChanged();
        ImportDocumentSourceFileCommand.NotifyCanExecuteChanged();
        CreateBriefCommand.NotifyCanExecuteChanged();
        GeneratePromptDirectionsCommand.NotifyCanExecuteChanged();
        GenerateDesignBlueprintsCommand.NotifyCanExecuteChanged();
        PromoteDesignBlueprintCommand.NotifyCanExecuteChanged();
        PromotePromptDirectionCommand.NotifyCanExecuteChanged();
        RunFakeGenerationCommand.NotifyCanExecuteChanged();
        RunFakeImageEditCommand.NotifyCanExecuteChanged();
        RunFakeReviewCommand.NotifyCanExecuteChanged();
        ApproveSelectedReviewCommand.NotifyCanExecuteChanged();
        RejectSelectedReviewCommand.NotifyCanExecuteChanged();
        ExportDeliveryCommand.NotifyCanExecuteChanged();
        CreateSeriesCommand.NotifyCanExecuteChanged();
        AddItemCommand.NotifyCanExecuteChanged();
        CreatePromptVersionCommand.NotifyCanExecuteChanged();
    }

    private bool CanRunMutation() => !_isMutatingOperationActive;

    private async Task RefreshProjectsCoreAsync(Guid? selectedProjectId, CancellationToken cancellationToken)
    {
        var result = await _projectWorkspaceCoordinator.RefreshProjectsAsync(selectedProjectId, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        ApplyWorkspaceResult(result);
    }

    private void ApplyWorkspaceResult(ProjectWorkspaceResult result)
    {
        ApplyWorkspaceResult(result, queueSelectedProjectLoad: true);
    }

    private void ApplyWorkspaceResult(
        ProjectWorkspaceResult result,
        bool queueSelectedProjectLoad,
        Guid? selectedProjectIdOverride = null)
    {
        var selectedProject = ResolveWorkspaceSelection(result, selectedProjectIdOverride);

        _suppressSelectedProjectLoad = true;
        try
        {
            Projects = result.Projects;
            SelectedProject = selectedProject;
        }
        finally
        {
            _suppressSelectedProjectLoad = false;
        }

        if (selectedProject is null)
        {
            if (queueSelectedProjectLoad)
            {
                ApplyEmptyPlanState();
            }

            return;
        }

        if (queueSelectedProjectLoad)
        {
            QueueSelectedProjectLoad(selectedProject);
        }
    }

    private static ProjectSummaryViewModel? ResolveWorkspaceSelection(
        ProjectWorkspaceResult result,
        Guid? selectedProjectIdOverride)
    {
        if (selectedProjectIdOverride is null)
        {
            return result.SelectedProject;
        }

        return result.Projects.FirstOrDefault(project => project.Id == selectedProjectIdOverride)
            ?? result.SelectedProject;
    }

    private async Task LoadPlanAsync(
        Guid projectId,
        Guid? selectedSeriesId,
        Guid? selectedItemId,
        CancellationToken cancellationToken)
    {
        var state = await LoadPlanStateAsync(
            projectId,
            selectedSeriesId,
            selectedItemId,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        ApplyLoadedPlanState(state);
    }

    private async Task<ProjectWorkbenchStateResult> LoadPlanStateAsync(
        Guid projectId,
        Guid? selectedSeriesId,
        Guid? selectedItemId,
        CancellationToken cancellationToken,
        Guid? activeCreativeBriefId = null)
    {
        return await _projectWorkbenchStateCoordinator.LoadAsync(
            projectId,
            selectedSeriesId,
            selectedItemId,
            activeCreativeBriefId ?? _activeCreativeBriefId,
            NoItemsInSeriesText,
            cancellationToken);
    }

    private void ApplyLoadedPlanState(ProjectWorkbenchStateResult state)
    {
        if (state.Series.Count == 0)
        {
            ApplyEmptyPlanState();
            return;
        }

        ApplyWorkbenchState(state);
        CreateSeriesCommand.NotifyCanExecuteChanged();
        RebuildWorkflowGraphRows();
    }

    private async Task RefreshWorkspaceIfProjectStillSelectedAsync(
        Guid projectId,
        bool queueSelectedProjectLoad,
        CancellationToken cancellationToken)
    {
        if (SelectedProject?.Id != projectId)
        {
            return;
        }

        var workspace = await _projectWorkspaceCoordinator.RefreshProjectsAsync(projectId, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (SelectedProject?.Id != projectId)
        {
            return;
        }

        ApplyWorkspaceResult(workspace, queueSelectedProjectLoad, selectedProjectIdOverride: projectId);
    }

    private Task ClearPlanAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ApplyEmptyPlanState();
        return Task.CompletedTask;
    }

    private Task ReloadCurrentProjectAsync(
        Guid projectId,
        Guid? selectedSeriesId,
        Guid? selectedItemId,
        CancellationToken cancellationToken)
    {
        return LoadPlanAsync(projectId, selectedSeriesId, selectedItemId, cancellationToken);
    }

    private async Task<ProjectReloadSnapshot> CaptureProjectReloadSnapshotAsync(
        Guid projectId,
        Guid? selectedSeriesId,
        Guid? selectedItemId,
        CancellationToken cancellationToken,
        Guid? activeCreativeBriefId = null)
    {
        var state = await LoadPlanStateAsync(
            projectId,
            selectedSeriesId,
            selectedItemId,
            cancellationToken,
            activeCreativeBriefId ?? _activeCreativeBriefId);
        cancellationToken.ThrowIfCancellationRequested();
        return new ProjectReloadSnapshot(projectId, activeCreativeBriefId ?? _activeCreativeBriefId, state);
    }

    private bool TryApplyProjectReloadSnapshot(ProjectReloadSnapshot snapshot)
    {
        if (SelectedProject?.Id != snapshot.ProjectId)
        {
            return false;
        }

        _activeCreativeBriefId = snapshot.ActiveCreativeBriefId;
        ApplyLoadedPlanState(snapshot.State);
        return true;
    }

    private sealed record ProjectReloadSnapshot(
        Guid ProjectId,
        Guid? ActiveCreativeBriefId,
        ProjectWorkbenchStateResult State);
}
