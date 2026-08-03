using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.App.ViewModels;

internal delegate Task<bool> ImageSeriesQueueMutationRunner(
    Guid? selectedTaskId,
    Func<Guid, Guid?, CancellationToken, Task> mutation);

internal delegate Task<bool> ImageSeriesFakeGenerationRunner(
    IReadOnlyList<SeriesSummaryViewModel> series);

public sealed partial class ImageSeriesWorkspaceViewModel : ObservableObject
{
    private readonly GenerationWorkflowCoordinator _generationWorkflowCoordinator;
    private readonly ImageSeriesQueueMutationRunner _runMutationAndReload;
    private readonly ImageSeriesFakeGenerationRunner _runFakeGeneration;
    private readonly Func<bool> _canMutate;
    private readonly Func<bool> _hasSelectedProject;
    private readonly Func<bool> _hasCurrentLiveGenerationAuthority;
    private readonly Action _queueProjectionChanged;

    [ObservableProperty]
    private IReadOnlyList<QueueRowViewModel> _queueRows = [];

    [ObservableProperty]
    private QueueRowViewModel? _selectedQueueRow;

    [ObservableProperty]
    private string _prepareGenerationQueueText = string.Empty;

    [ObservableProperty]
    private string _runFakeGenerationText = string.Empty;

    [ObservableProperty]
    private string _executeGenerationQueueText = string.Empty;

    [ObservableProperty]
    private string _executeApprovedLiveGenerationText = string.Empty;

    [ObservableProperty]
    private string _liveGenerationAuthorityRequiredText = string.Empty;

    [ObservableProperty]
    private string _pauseGenerationTaskText = string.Empty;

    [ObservableProperty]
    private string _resumeGenerationTaskText = string.Empty;

    [ObservableProperty]
    private string _retryGenerationTaskText = string.Empty;

    [ObservableProperty]
    private string _moveGenerationTaskUpText = string.Empty;

    [ObservableProperty]
    private string _moveGenerationTaskDownText = string.Empty;

    [ObservableProperty]
    private string _queueItemColumn = string.Empty;

    [ObservableProperty]
    private string _queuePositionColumn = string.Empty;

    [ObservableProperty]
    private string _queueStatusColumn = string.Empty;

    [ObservableProperty]
    private string _queueAttemptsColumn = string.Empty;

    [ObservableProperty]
    private string _queueOutputColumn = string.Empty;

    [ObservableProperty]
    private string _queueErrorColumn = string.Empty;

    [ObservableProperty]
    private string _noQueueRowsText = string.Empty;

    internal ImageSeriesWorkspaceViewModel(
        GenerationWorkflowCoordinator generationWorkflowCoordinator,
        ImageSeriesQueueMutationRunner runMutationAndReload,
        ImageSeriesFakeGenerationRunner runFakeGeneration,
        Func<bool> canMutate,
        Func<bool> hasSelectedProject,
        Func<bool> hasCurrentLiveGenerationAuthority,
        Action queueProjectionChanged,
        ImageSeriesPlanningWorkspaceViewModel planning,
        ImageSeriesBriefWorkspaceViewModel brief,
        ImageSeriesGenerationSettingsWorkspaceViewModel generationSettings,
        ImageSeriesGalleryWorkspaceViewModel gallery,
        ImageSeriesReviewWorkspaceViewModel review,
        ImageSeriesDeliveryWorkspaceViewModel delivery)
    {
        _generationWorkflowCoordinator = generationWorkflowCoordinator
            ?? throw new ArgumentNullException(nameof(generationWorkflowCoordinator));
        _runMutationAndReload = runMutationAndReload
            ?? throw new ArgumentNullException(nameof(runMutationAndReload));
        _runFakeGeneration = runFakeGeneration ?? throw new ArgumentNullException(nameof(runFakeGeneration));
        _canMutate = canMutate ?? throw new ArgumentNullException(nameof(canMutate));
        _hasSelectedProject = hasSelectedProject ?? throw new ArgumentNullException(nameof(hasSelectedProject));
        _hasCurrentLiveGenerationAuthority = hasCurrentLiveGenerationAuthority
            ?? throw new ArgumentNullException(nameof(hasCurrentLiveGenerationAuthority));
        _queueProjectionChanged = queueProjectionChanged
            ?? throw new ArgumentNullException(nameof(queueProjectionChanged));
        Planning = planning ?? throw new ArgumentNullException(nameof(planning));
        Brief = brief ?? throw new ArgumentNullException(nameof(brief));
        GenerationSettings = generationSettings ?? throw new ArgumentNullException(nameof(generationSettings));
        Gallery = gallery ?? throw new ArgumentNullException(nameof(gallery));
        Review = review ?? throw new ArgumentNullException(nameof(review));
        Delivery = delivery ?? throw new ArgumentNullException(nameof(delivery));
    }

    public ImageSeriesPlanningWorkspaceViewModel Planning { get; }

    public ImageSeriesBriefWorkspaceViewModel Brief { get; }

    public ImageSeriesGenerationSettingsWorkspaceViewModel GenerationSettings { get; }

    public ImageSeriesGalleryWorkspaceViewModel Gallery { get; }

    public ImageSeriesReviewWorkspaceViewModel Review { get; }

    public ImageSeriesDeliveryWorkspaceViewModel Delivery { get; }

    public bool HasQueueRows => QueueRows.Count > 0;

    public bool IsLiveGenerationExecutionAvailable =>
        CanRunProjectMutation()
        && _hasCurrentLiveGenerationAuthority()
        && QueueRows.Any(row => row.TaskStatus is GenerationTaskStatus.Queued);

    internal void ApplyProjection(IReadOnlyList<QueueRowViewModel> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var selectedTaskId = SelectedQueueRow?.TaskId;
        QueueRows = rows;
        SelectedQueueRow = rows.FirstOrDefault(row => row.TaskId == selectedTaskId)
            ?? rows.FirstOrDefault();
    }

    internal void ApplyLocalization(MainWindowLocalizationPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        PrepareGenerationQueueText = payload.PrepareGenerationQueueText;
        RunFakeGenerationText = payload.RunFakeGenerationText;
        ExecuteGenerationQueueText = payload.ExecuteGenerationQueueText;
        ExecuteApprovedLiveGenerationText = payload.ExecuteApprovedLiveGenerationText;
        LiveGenerationAuthorityRequiredText = payload.LiveGenerationAuthorityRequiredText;
        PauseGenerationTaskText = payload.PauseGenerationTaskText;
        ResumeGenerationTaskText = payload.ResumeGenerationTaskText;
        RetryGenerationTaskText = payload.RetryGenerationTaskText;
        MoveGenerationTaskUpText = payload.MoveGenerationTaskUpText;
        MoveGenerationTaskDownText = payload.MoveGenerationTaskDownText;
        QueueItemColumn = payload.QueueItemColumn;
        QueuePositionColumn = payload.QueuePositionColumn;
        QueueStatusColumn = payload.QueueStatusColumn;
        QueueAttemptsColumn = payload.QueueAttemptsColumn;
        QueueOutputColumn = payload.QueueOutputColumn;
        QueueErrorColumn = payload.QueueErrorColumn;
        NoQueueRowsText = payload.NoQueueRowsText;
    }

    internal void NotifyCommandStatesChanged()
    {
        PrepareGenerationQueueCommand.NotifyCanExecuteChanged();
        RunFakeGenerationCommand.NotifyCanExecuteChanged();
        ExecutePreparedGenerationQueueCommand.NotifyCanExecuteChanged();
        PauseSelectedGenerationTaskCommand.NotifyCanExecuteChanged();
        ResumeSelectedGenerationTaskCommand.NotifyCanExecuteChanged();
        RetrySelectedGenerationTaskCommand.NotifyCanExecuteChanged();
        MoveSelectedGenerationTaskUpCommand.NotifyCanExecuteChanged();
        MoveSelectedGenerationTaskDownCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(IsLiveGenerationExecutionAvailable));
    }

    partial void OnQueueRowsChanged(IReadOnlyList<QueueRowViewModel> value)
    {
        OnPropertyChanged(nameof(HasQueueRows));
        NotifyCommandStatesChanged();
        _queueProjectionChanged();
    }

    partial void OnSelectedQueueRowChanged(QueueRowViewModel? value)
    {
        NotifyCommandStatesChanged();
    }

    [RelayCommand(CanExecute = nameof(CanPrepareGenerationQueue))]
    private Task PrepareGenerationQueueAsync()
    {
        return RunMutationAndReloadAsync((projectId, ignoredTaskId, cancellationToken) =>
            _generationWorkflowCoordinator.PrepareFakeGenerationQueueAsync(projectId, cancellationToken));
    }

    private bool CanPrepareGenerationQueue()
    {
        return CanRunFakeGeneration();
    }

    [RelayCommand(CanExecute = nameof(CanRunFakeGeneration))]
    private Task RunFakeGenerationAsync()
    {
        return _runFakeGeneration(Planning.Series);
    }

    private bool CanRunFakeGeneration()
    {
        return CanRunProjectMutation()
            && Planning.PromptRows.Count > 0
            && QueueRows.All(row => row.TaskStatus is not (
                GenerationTaskStatus.Queued
                or GenerationTaskStatus.Paused
                or GenerationTaskStatus.Running));
    }

    [RelayCommand(CanExecute = nameof(CanExecutePreparedGenerationQueue))]
    private Task ExecutePreparedGenerationQueueAsync()
    {
        return RunMutationAndReloadAsync(async (projectId, ignoredTaskId, cancellationToken) =>
        {
            await _generationWorkflowCoordinator.ExecuteFakeGenerationQueueAsync(projectId, cancellationToken);
        });
    }

    private bool CanExecutePreparedGenerationQueue()
    {
        return CanRunProjectMutation()
            && QueueRows.Any(row => row.TaskStatus is GenerationTaskStatus.Queued);
    }

    [RelayCommand(CanExecute = nameof(CanPauseSelectedGenerationTask))]
    private Task PauseSelectedGenerationTaskAsync()
    {
        return RunSelectedTaskMutationAsync((projectId, taskId, cancellationToken) =>
            _generationWorkflowCoordinator.PauseGenerationTaskAsync(projectId, taskId, cancellationToken));
    }

    private bool CanPauseSelectedGenerationTask()
    {
        return CanRunProjectMutation() && SelectedQueueRow?.CanPause is true;
    }

    [RelayCommand(CanExecute = nameof(CanResumeSelectedGenerationTask))]
    private Task ResumeSelectedGenerationTaskAsync()
    {
        return RunSelectedTaskMutationAsync((projectId, taskId, cancellationToken) =>
            _generationWorkflowCoordinator.ResumeGenerationTaskAsync(projectId, taskId, cancellationToken));
    }

    private bool CanResumeSelectedGenerationTask()
    {
        return CanRunProjectMutation() && SelectedQueueRow?.CanResume is true;
    }

    [RelayCommand(CanExecute = nameof(CanRetrySelectedGenerationTask))]
    private Task RetrySelectedGenerationTaskAsync()
    {
        return RunSelectedTaskMutationAsync((projectId, taskId, cancellationToken) =>
            _generationWorkflowCoordinator.RetryGenerationTaskAsync(projectId, taskId, cancellationToken));
    }

    private bool CanRetrySelectedGenerationTask()
    {
        return CanRunProjectMutation() && SelectedQueueRow?.CanRetry is true;
    }

    [RelayCommand(CanExecute = nameof(CanMoveSelectedGenerationTaskUp))]
    private Task MoveSelectedGenerationTaskUpAsync()
    {
        return MoveSelectedGenerationTaskAsync(GenerationTaskMoveDirection.Up);
    }

    private bool CanMoveSelectedGenerationTaskUp()
    {
        return CanMoveSelectedGenerationTask(GenerationTaskMoveDirection.Up);
    }

    [RelayCommand(CanExecute = nameof(CanMoveSelectedGenerationTaskDown))]
    private Task MoveSelectedGenerationTaskDownAsync()
    {
        return MoveSelectedGenerationTaskAsync(GenerationTaskMoveDirection.Down);
    }

    private bool CanMoveSelectedGenerationTaskDown()
    {
        return CanMoveSelectedGenerationTask(GenerationTaskMoveDirection.Down);
    }

    private Task MoveSelectedGenerationTaskAsync(GenerationTaskMoveDirection direction)
    {
        return RunSelectedTaskMutationAsync((projectId, taskId, cancellationToken) =>
            _generationWorkflowCoordinator.MoveGenerationTaskAsync(
                projectId,
                taskId,
                direction,
                cancellationToken));
    }

    private bool CanMoveSelectedGenerationTask(GenerationTaskMoveDirection direction)
    {
        if (!CanRunProjectMutation() || SelectedQueueRow?.CanReorder is not true)
        {
            return false;
        }

        var activeRows = QueueRows
            .Where(row => row.CanReorder)
            .OrderBy(row => row.QueuePosition ?? int.MaxValue)
            .ThenBy(row => row.TaskId)
            .ToArray();
        var index = Array.FindIndex(activeRows, row => row.TaskId == SelectedQueueRow.TaskId);
        return direction is GenerationTaskMoveDirection.Up
            ? index > 0
            : index >= 0 && index < activeRows.Length - 1;
    }

    private Task RunSelectedTaskMutationAsync(Func<Guid, Guid, CancellationToken, Task> mutation)
    {
        if (SelectedQueueRow is null)
        {
            return Task.CompletedTask;
        }

        var taskId = SelectedQueueRow.TaskId;
        return RunMutationAndReloadAsync((projectId, ignoredTaskId, cancellationToken) =>
            mutation(projectId, taskId, cancellationToken));
    }

    private async Task RunMutationAndReloadAsync(Func<Guid, Guid?, CancellationToken, Task> mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        var selectedTaskId = SelectedQueueRow?.TaskId;
        await _runMutationAndReload(selectedTaskId, mutation);
    }

    private bool CanRunProjectMutation()
    {
        return _canMutate() && _hasSelectedProject();
    }
}
