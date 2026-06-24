using CommunityToolkit.Mvvm.Input;
using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    [RelayCommand(CanExecute = nameof(CanRunFakeGeneration))]
    private async Task RunFakeGenerationAsync()
    {
        if (SelectedProject is null)
        {
            return;
        }

        var projectId = SelectedProject.Id;
        var selectedSeriesId = SelectedSeries?.Id;
        var selectedItemId = SelectedSeriesItem?.Id;
        var result = await _operationGate.RunExclusiveAsync(
            async cancellationToken =>
            {
                var generationResult = await _generationWorkflowCoordinator.RunFakeGenerationAsync(
                    projectId,
                    Series,
                    cancellationToken);
                var snapshot = await CaptureProjectReloadSnapshotAsync(
                    projectId,
                    selectedSeriesId,
                    selectedItemId,
                    cancellationToken);

                return new GenerationReloadResult(generationResult.QueueRows, snapshot);
            });

        if (!result.Executed || result.Value is null)
        {
            return;
        }

        if (!TryApplyProjectReloadSnapshot(result.Value.Snapshot))
        {
            return;
        }

        QueueRows = result.Value.QueueRows;
        ReviewRows = [];
        DeliveryRows = [];
        RunFakeReviewCommand.NotifyCanExecuteChanged();
    }

    private bool CanRunFakeGeneration()
    {
        return CanRunMutation()
            && SelectedProject is not null
            && PromptRows.Count > 0;
    }

    [RelayCommand(CanExecute = nameof(CanRunFakeImageEdit))]
    private async Task RunFakeImageEditAsync()
    {
        if (SelectedProject is null || SelectedGalleryRow is null)
        {
            return;
        }

        var projectId = SelectedProject.Id;
        var result = await _operationGate.RunExclusiveAsync(
            cancellationToken => _workbenchInspectorCoordinator.RunFakeImageEditAsync(
                projectId,
                SelectedGalleryRow,
                NewImageEditPrompt,
                NewImageEditMaskPath,
                ImageEditResultText,
                GalleryRows,
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

        GalleryRows = result.Value.GalleryRows;
        SelectedGalleryRow = result.Value.SelectedGalleryRow;
        ReviewRows = [];
        DeliveryRows = [];
        ActivityItems = result.Value.ActivityItems;
    }

    private bool CanRunFakeImageEdit()
    {
        return CanRunMutation()
            && SelectedProject is not null
            && SelectedGalleryRow is not null
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
                GalleryRows,
                cancellationToken));

        if (!result.Executed || result.Value is null)
        {
            return;
        }

        if (SelectedProject?.Id != projectId)
        {
            return;
        }

        ReviewRows = result.Value;
        DeliveryRows = [];
        ExportDeliveryCommand.NotifyCanExecuteChanged();
    }

    private bool CanRunFakeReview()
    {
        return CanRunMutation()
            && SelectedProject is not null
            && GalleryRows.Count > 0;
    }

    [RelayCommand(CanExecute = nameof(CanApproveSelectedReview))]
    private Task ApproveSelectedReviewAsync()
    {
        return ApplyFinalApprovalAsync(approve: true);
    }

    private bool CanApproveSelectedReview()
    {
        return CanRunMutation()
            && SelectedReviewRow is { Review.Decision: ReviewDecision.Pass, Review.NeedsRepair: false }
            && !string.IsNullOrWhiteSpace(FinalApprovalReviewer);
    }

    [RelayCommand(CanExecute = nameof(CanRejectSelectedReview))]
    private Task RejectSelectedReviewAsync()
    {
        return ApplyFinalApprovalAsync(approve: false);
    }

    private bool CanRejectSelectedReview()
    {
        return CanRunMutation()
            && SelectedReviewRow is not null
            && !string.IsNullOrWhiteSpace(FinalApprovalReviewer)
            && !string.IsNullOrWhiteSpace(FinalApprovalNotes);
    }

    private async Task ApplyFinalApprovalAsync(bool approve)
    {
        if (SelectedReviewRow is null || SelectedProject is null)
        {
            return;
        }

        var projectId = SelectedProject.Id;
        var selectedSeriesId = SelectedSeries?.Id;
        var selectedItemId = SelectedSeriesItem?.Id;
        var candidateImageId = SelectedReviewRow.CandidateImageId;
        var result = await _operationGate.RunExclusiveAsync(
            async cancellationToken =>
            {
                await _reviewWorkflowCoordinator.ApplyFinalApprovalAsync(
                    projectId,
                    SelectedReviewRow,
                    approve,
                    FinalApprovalReviewer,
                    FinalApprovalNotes,
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

        SelectedReviewRow = ReviewRows.FirstOrDefault(row => row.CandidateImageId == result.Value.CandidateImageId)
            ?? SelectedReviewRow;
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
                GalleryRows,
                ReviewRows,
                DesignBlueprintRows,
                _activeCreativeBriefId,
                cancellationToken));

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
            && GalleryRows.Count > 0
            && ReviewRows.Any(row => row.HumanApproved && row.Decision == ReviewDecision.Pass.ToString());
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

    private sealed record GenerationReloadResult(
        IReadOnlyList<QueueRowViewModel> QueueRows,
        ProjectReloadSnapshot Snapshot);
}
