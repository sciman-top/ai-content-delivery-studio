using CommunityToolkit.Mvvm.Input;
using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    [RelayCommand(CanExecute = nameof(CanRunFakeGeneration))]
    private async Task RunFakeGenerationAsync()
    {
        await RunQueueProjectMutationAsync(async (projectId, ignoredTaskId, cancellationToken) =>
        {
            await _generationWorkflowCoordinator.RunFakeGenerationAsync(projectId, Series, cancellationToken);
        });
    }

    private bool CanRunFakeGeneration()
    {
        return CanRunMutation()
            && SelectedProject is not null
            && PromptRows.Count > 0
            && QueueRows.All(row => row.TaskStatus is not (
                GenerationTaskStatus.Queued
                or GenerationTaskStatus.Paused
                or GenerationTaskStatus.Running));
    }

    [RelayCommand(CanExecute = nameof(CanRunFakeGeneration))]
    private async Task PrepareGenerationQueueAsync()
    {
        await RunQueueProjectMutationAsync((projectId, ignoredTaskId, cancellationToken) =>
            _generationWorkflowCoordinator.PrepareFakeGenerationQueueAsync(projectId, cancellationToken));
    }

    [RelayCommand(CanExecute = nameof(CanExecutePreparedGenerationQueue))]
    private async Task ExecutePreparedGenerationQueueAsync()
    {
        await RunQueueProjectMutationAsync(async (projectId, ignoredTaskId, cancellationToken) =>
        {
            await _generationWorkflowCoordinator.ExecuteFakeGenerationQueueAsync(projectId, cancellationToken);
        });
    }

    private bool CanExecutePreparedGenerationQueue()
    {
        return CanRunMutation()
            && SelectedProject is not null
            && QueueRows.Any(row => row.TaskStatus is GenerationTaskStatus.Queued);
    }

    [RelayCommand(CanExecute = nameof(CanPauseSelectedGenerationTask))]
    private Task PauseSelectedGenerationTaskAsync()
    {
        return RunSelectedQueueTaskMutationAsync((projectId, taskId, cancellationToken) =>
            _generationWorkflowCoordinator.PauseGenerationTaskAsync(projectId, taskId, cancellationToken));
    }

    private bool CanPauseSelectedGenerationTask()
    {
        return CanRunMutation() && SelectedProject is not null && SelectedQueueRow?.CanPause is true;
    }

    [RelayCommand(CanExecute = nameof(CanResumeSelectedGenerationTask))]
    private Task ResumeSelectedGenerationTaskAsync()
    {
        return RunSelectedQueueTaskMutationAsync((projectId, taskId, cancellationToken) =>
            _generationWorkflowCoordinator.ResumeGenerationTaskAsync(projectId, taskId, cancellationToken));
    }

    private bool CanResumeSelectedGenerationTask()
    {
        return CanRunMutation() && SelectedProject is not null && SelectedQueueRow?.CanResume is true;
    }

    [RelayCommand(CanExecute = nameof(CanRetrySelectedGenerationTask))]
    private Task RetrySelectedGenerationTaskAsync()
    {
        return RunSelectedQueueTaskMutationAsync(async (projectId, taskId, cancellationToken) =>
        {
            await _generationWorkflowCoordinator.RetryGenerationTaskAsync(projectId, taskId, cancellationToken);
        });
    }

    private bool CanRetrySelectedGenerationTask()
    {
        return CanRunMutation() && SelectedProject is not null && SelectedQueueRow?.CanRetry is true;
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
        return RunSelectedQueueTaskMutationAsync((projectId, taskId, cancellationToken) =>
            _generationWorkflowCoordinator.MoveGenerationTaskAsync(
                projectId,
                taskId,
                direction,
                cancellationToken));
    }

    private bool CanMoveSelectedGenerationTask(GenerationTaskMoveDirection direction)
    {
        if (!CanRunMutation() || SelectedProject is null || SelectedQueueRow?.CanReorder is not true)
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

    private Task RunSelectedQueueTaskMutationAsync(
        Func<Guid, Guid, CancellationToken, Task> mutation)
    {
        if (SelectedQueueRow is null)
        {
            return Task.CompletedTask;
        }

        var taskId = SelectedQueueRow.TaskId;
        return RunQueueProjectMutationAsync((projectId, _, cancellationToken) =>
            mutation(projectId, taskId, cancellationToken));
    }

    private async Task RunQueueProjectMutationAsync(
        Func<Guid, Guid?, CancellationToken, Task> mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        if (SelectedProject is null)
        {
            return;
        }

        var projectId = SelectedProject.Id;
        var selectedTaskId = SelectedQueueRow?.TaskId;
        var selectedSeriesId = SelectedSeries?.Id;
        var selectedItemId = SelectedSeriesItem?.Id;
        var result = await _operationGate.RunExclusiveAsync(async cancellationToken =>
        {
            await mutation(projectId, selectedTaskId, cancellationToken);
            return await CaptureProjectReloadSnapshotAsync(
                projectId,
                selectedSeriesId,
                selectedItemId,
                cancellationToken);
        });

        if (!result.Executed || result.Value is null || !TryApplyProjectReloadSnapshot(result.Value))
        {
            return;
        }

        SelectedQueueRow = QueueRows.FirstOrDefault(row => row.TaskId == selectedTaskId)
            ?? QueueRows.FirstOrDefault();
        ImageSeries.ReviewRows = [];
        DeliveryRows = [];
        RunFakeReviewCommand.NotifyCanExecuteChanged();
    }

    private void NotifyQueueCommandStatesChanged()
    {
        RunFakeGenerationCommand.NotifyCanExecuteChanged();
        PrepareGenerationQueueCommand.NotifyCanExecuteChanged();
        ExecutePreparedGenerationQueueCommand.NotifyCanExecuteChanged();
        PauseSelectedGenerationTaskCommand.NotifyCanExecuteChanged();
        ResumeSelectedGenerationTaskCommand.NotifyCanExecuteChanged();
        RetrySelectedGenerationTaskCommand.NotifyCanExecuteChanged();
        MoveSelectedGenerationTaskUpCommand.NotifyCanExecuteChanged();
        MoveSelectedGenerationTaskDownCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRunFakeImageEdit))]
    private async Task RunFakeImageEditAsync()
    {
        if (SelectedProject is null || ImageSeries.SelectedGalleryRow is null)
        {
            return;
        }

        var projectId = SelectedProject.Id;
        var result = await _operationGate.RunExclusiveAsync(
            cancellationToken => _workbenchInspectorCoordinator.RunFakeImageEditAsync(
                projectId,
                ImageSeries.SelectedGalleryRow,
                NewImageEditPrompt,
                NewImageEditMaskPath,
                ImageEditResultText,
                ImageSeries.GalleryRows,
                ActivityItems,
                cancellationToken));

        if (!result.Executed || result.Value is null)
        {
            return;
        }

        if (SelectedProject?.Id != projectId)
        {
            return;
        }

        ImageSeries.GalleryRows = result.Value.GalleryRows;
        ImageSeries.SelectedGalleryRow = result.Value.SelectedGalleryRow;
        ImageSeries.ReviewRows = [];
        DeliveryRows = [];
        ActivityItems = result.Value.ActivityItems;
    }

    private bool CanRunFakeImageEdit()
    {
        return CanRunMutation()
            && SelectedProject is not null
            && ImageSeries.SelectedGalleryRow is not null
            && !string.IsNullOrWhiteSpace(NewImageEditPrompt);
    }

    [RelayCommand(CanExecute = nameof(CanRunFakeReview))]
    private async Task RunFakeReviewAsync()
    {
        if (SelectedProject is null)
        {
            return;
        }

        var projectId = SelectedProject.Id;
        var result = await _operationGate.RunExclusiveAsync(
            cancellationToken => _reviewWorkflowCoordinator.RunFakeReviewAsync(
                projectId,
                ImageSeries.GalleryRows,
                cancellationToken));

        if (!result.Executed || result.Value is null)
        {
            return;
        }

        if (SelectedProject?.Id != projectId)
        {
            return;
        }

        ImageSeries.ReviewRows = result.Value;
        DeliveryRows = [];
        ExportDeliveryCommand.NotifyCanExecuteChanged();
    }

    private bool CanRunFakeReview()
    {
        return CanRunMutation()
            && SelectedProject is not null
            && ImageSeries.GalleryRows.Count > 0;
    }

    [RelayCommand(CanExecute = nameof(CanApproveSelectedReview))]
    private Task ApproveSelectedReviewAsync()
    {
        return ApplyFinalApprovalAsync(approve: true);
    }

    private bool CanApproveSelectedReview()
    {
        return CanRunMutation()
            && ImageSeries.SelectedReviewRow is { Review.Decision: ReviewDecision.Pass, Review.NeedsRepair: false }
            && !string.IsNullOrWhiteSpace(ImageSeries.FinalApprovalReviewer);
    }

    [RelayCommand(CanExecute = nameof(CanRejectSelectedReview))]
    private Task RejectSelectedReviewAsync()
    {
        return ApplyFinalApprovalAsync(approve: false);
    }

    private bool CanRejectSelectedReview()
    {
        return CanRunMutation()
            && ImageSeries.SelectedReviewRow is not null
            && !string.IsNullOrWhiteSpace(ImageSeries.FinalApprovalReviewer)
            && !string.IsNullOrWhiteSpace(ImageSeries.FinalApprovalNotes);
    }

    private async Task ApplyFinalApprovalAsync(bool approve)
    {
        if (ImageSeries.SelectedReviewRow is null || SelectedProject is null)
        {
            return;
        }

        var projectId = SelectedProject.Id;
        var selectedSeriesId = SelectedSeries?.Id;
        var selectedItemId = SelectedSeriesItem?.Id;
        var candidateImageId = ImageSeries.SelectedReviewRow.CandidateImageId;
        var result = await _operationGate.RunExclusiveAsync(
            async cancellationToken =>
            {
                await _reviewWorkflowCoordinator.ApplyFinalApprovalAsync(
                    projectId,
                    ImageSeries.SelectedReviewRow,
                    approve,
                    ImageSeries.FinalApprovalReviewer,
                    ImageSeries.FinalApprovalNotes,
                    cancellationToken);

                var snapshot = await CaptureProjectReloadSnapshotAsync(
                    projectId,
                    selectedSeriesId,
                    selectedItemId,
                    cancellationToken);

                return new FinalApprovalReloadResult(candidateImageId, snapshot);
            });

        if (!result.Executed || result.Value is null)
        {
            return;
        }

        if (!TryApplyProjectReloadSnapshot(result.Value.Snapshot))
        {
            return;
        }

        ImageSeries.SelectedReviewRow = ImageSeries.ReviewRows.FirstOrDefault(
                row => row.CandidateImageId == result.Value.CandidateImageId)
            ?? ImageSeries.SelectedReviewRow;
        DeliveryRows = [];
        ExportDeliveryCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanExportDelivery))]
    private async Task ExportDeliveryAsync()
    {
        if (SelectedProject is null)
        {
            return;
        }

        var projectId = SelectedProject.Id;
        var result = await _operationGate.RunExclusiveAsync(
            cancellationToken => _deliveryWorkflowCoordinator.ExportDeliveryAsync(
                projectId,
                SelectedProject.Name,
                ImageSeries.GalleryRows,
                ImageSeries.ReviewRows,
                DesignBlueprintRows,
                _activeCreativeBriefId,
                cancellationToken,
                SelectedFinalDeliveryCategoryOption?.Category ?? FinalImageDeliveryCategory.ImageSeries,
                FinalDeliveryRootPath));

        if (!result.Executed || result.Value is null)
        {
            return;
        }

        DeliveryRows = result.Value.DeliveryRows;
    }

    private bool CanExportDelivery()
    {
        return CanRunMutation()
            && SelectedProject is not null
            && ImageSeries.GalleryRows.Count > 0
            && SelectedFinalDeliveryCategoryOption is not null
            && !string.IsNullOrWhiteSpace(FinalDeliveryDestinationPreview)
            && ImageSeries.ReviewRows.Any(row => row.HumanApproved && row.Decision == ReviewDecision.Pass.ToString());
    }

    [RelayCommand(CanExecute = nameof(CanBrowseFinalDeliveryRoot))]
    private async Task BrowseFinalDeliveryRootAsync()
    {
        if (_finalDeliveryRootPickerService is null)
        {
            return;
        }

        var selectedRoot = await _finalDeliveryRootPickerService.PickAsync(
            FinalDeliveryRootPath,
            BrowseFinalDeliveryRootText,
            CancellationToken.None);
        if (!string.IsNullOrWhiteSpace(selectedRoot))
        {
            FinalDeliveryRootPath = selectedRoot;
        }
    }

    private bool CanBrowseFinalDeliveryRoot()
    {
        return CanRunMutation() && _finalDeliveryRootPickerService is not null;
    }

    [RelayCommand(CanExecute = nameof(CanCreateSeries))]
    private async Task CreateSeriesAsync()
    {
        if (SelectedProject is null)
        {
            return;
        }

        var projectId = SelectedProject.Id;
        var result = await _operationGate.RunExclusiveAsync(
            async cancellationToken =>
            {
                var seriesId = await _planEditorWorkflowCoordinator.CreateSeriesAsync(
                    projectId,
                    NewSeriesTitle,
                    NewSeriesDescription,
                    cancellationToken);
                var snapshot = await CaptureProjectReloadSnapshotAsync(
                    projectId,
                    seriesId,
                    selectedItemId: null,
                    cancellationToken);

                return new CreateSeriesReloadResult(seriesId, snapshot);
            });

        if (!result.Executed || result.Value is null)
        {
            return;
        }

        NewSeriesTitle = string.Empty;
        NewSeriesDescription = string.Empty;
        TryApplyProjectReloadSnapshot(result.Value.Snapshot);
    }

    private bool CanCreateSeries()
    {
        return CanRunMutation()
            && SelectedProject is not null
            && !string.IsNullOrWhiteSpace(NewSeriesTitle);
    }

    [RelayCommand(CanExecute = nameof(CanAddItem))]
    private async Task AddItemAsync()
    {
        if (SelectedProject is null || SelectedSeries is null)
        {
            return;
        }

        var projectId = SelectedProject.Id;
        var selectedSeriesId = SelectedSeries.Id;
        var result = await _operationGate.RunExclusiveAsync(
            async cancellationToken =>
            {
                var itemId = await _planEditorWorkflowCoordinator.AddItemAsync(
                    projectId,
                    selectedSeriesId,
                    NewItemTitle,
                    NewItemBrief,
                    cancellationToken);
                var snapshot = await CaptureProjectReloadSnapshotAsync(
                    projectId,
                    selectedSeriesId,
                    itemId,
                    cancellationToken);

                return new AddItemReloadResult(itemId, snapshot);
            });

        if (!result.Executed || result.Value is null)
        {
            return;
        }

        NewItemTitle = string.Empty;
        NewItemBrief = string.Empty;
        TryApplyProjectReloadSnapshot(result.Value.Snapshot);
    }

    private bool CanAddItem()
    {
        return CanRunMutation()
            && SelectedProject is not null
            && SelectedSeries is not null
            && !string.IsNullOrWhiteSpace(NewItemTitle);
    }

    [RelayCommand(CanExecute = nameof(CanCreatePromptVersion))]
    private async Task CreatePromptVersionAsync()
    {
        if (SelectedProject is null || SelectedSeries is null || SelectedSeriesItem is null)
        {
            return;
        }

        var projectId = SelectedProject.Id;
        var selectedSeriesId = SelectedSeries.Id;
        var selectedItemId = SelectedSeriesItem.Id;
        var operation = await _operationGate.RunExclusiveAsync(
            async cancellationToken =>
            {
                var promptVersionId = await _planEditorWorkflowCoordinator.CreatePromptVersionAsync(
                    projectId,
                    selectedItemId,
                    NewPromptText,
                    cancellationToken);

                var snapshot = await CaptureProjectReloadSnapshotAsync(
                    projectId,
                    selectedSeriesId,
                    selectedItemId,
                    cancellationToken);

                return new CreatePromptVersionReloadResult(promptVersionId, snapshot);
            });

        if (!operation.Executed || operation.Value is null)
        {
            return;
        }

        NewPromptText = string.Empty;
        TryApplyProjectReloadSnapshot(operation.Value.Snapshot);
    }

    private bool CanCreatePromptVersion()
    {
        return CanRunMutation()
            && SelectedProject is not null
            && SelectedSeriesItem is not null
            && !string.IsNullOrWhiteSpace(NewPromptText);
    }

    private sealed record AddItemReloadResult(Guid ItemId, ProjectReloadSnapshot Snapshot);

    private sealed record CreatePromptVersionReloadResult(Guid PromptVersionId, ProjectReloadSnapshot Snapshot);

    private sealed record CreateSeriesReloadResult(Guid SeriesId, ProjectReloadSnapshot Snapshot);

    private sealed record FinalApprovalReloadResult(Guid CandidateImageId, ProjectReloadSnapshot Snapshot);

}
