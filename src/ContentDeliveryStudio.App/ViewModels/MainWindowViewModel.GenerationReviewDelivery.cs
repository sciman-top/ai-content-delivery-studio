using CommunityToolkit.Mvvm.Input;
using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private Task<bool> RunImageSeriesFakeGenerationAsync(IReadOnlyList<SeriesSummaryViewModel> series)
    {
        return RunImageSeriesQueueMutationAndReloadAsync(
            SelectedQueueRow?.TaskId,
            async (projectId, ignoredTaskId, cancellationToken) =>
        {
            await _generationWorkflowCoordinator.RunFakeGenerationAsync(projectId, series, cancellationToken);
        });
    }

    private async Task<bool> RunImageSeriesQueueMutationAndReloadAsync(
        Guid? selectedTaskId,
        Func<Guid, Guid?, CancellationToken, Task> mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        if (SelectedProject is null)
        {
            return false;
        }

        var projectId = SelectedProject.Id;
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
            return false;
        }

        ImageSeriesReviewWorkspace.ApplyProjection([]);
        ImageSeriesDeliveryWorkspace.ApplyProjection([]);
        return true;
    }

    private async Task<WorkbenchInspectorImageEditResult?> RunImageSeriesGalleryEditAsync(
        GalleryRowViewModel selectedRow,
        string editPrompt,
        string? maskPath,
        IReadOnlyList<GalleryRowViewModel> currentRows)
    {
        if (SelectedProject is null)
        {
            return null;
        }

        var projectId = SelectedProject.Id;
        var result = await _operationGate.RunExclusiveAsync(
            cancellationToken => _workbenchInspectorCoordinator.RunFakeImageEditAsync(
                projectId,
                selectedRow,
                editPrompt,
                maskPath,
                ImageEditResultText,
                currentRows,
                ActivityItems,
                cancellationToken));

        if (!result.Executed || result.Value is null)
        {
            return null;
        }

        if (SelectedProject?.Id != projectId)
        {
            return null;
        }

        return result.Value;
    }

    private void ApplyImageSeriesGalleryEditResult(WorkbenchInspectorImageEditResult result)
    {
        ImageSeriesReviewWorkspace.ApplyProjection([]);
        ImageSeriesDeliveryWorkspace.ApplyProjection([]);
        ActivityItems = result.ActivityItems;
    }

    private void OnImageSeriesGalleryProjectionChanged()
    {
        ImageSeriesReviewWorkspace.NotifyCommandStatesChanged();
        RebuildWorkflowGraphRows();
    }

    private async Task<IReadOnlyList<ReviewRowViewModel>?> RunImageSeriesReviewAsync(
        IReadOnlyList<GalleryRowViewModel> galleryRows)
    {
        if (SelectedProject is null)
        {
            return null;
        }

        var projectId = SelectedProject.Id;
        var result = await _operationGate.RunExclusiveAsync(
            cancellationToken => _reviewWorkflowCoordinator.RunFakeReviewAsync(
                projectId,
                galleryRows,
                cancellationToken));

        if (!result.Executed || result.Value is null)
        {
            return null;
        }

        if (SelectedProject?.Id != projectId)
        {
            return null;
        }

        return result.Value;
    }

    private async Task<bool> RunImageSeriesFinalApprovalAsync(
        ReviewRowViewModel selectedRow,
        bool approve,
        string reviewer,
        string notes)
    {
        if (SelectedProject is null)
        {
            return false;
        }

        var projectId = SelectedProject.Id;
        var selectedSeriesId = SelectedSeries?.Id;
        var selectedItemId = SelectedSeriesItem?.Id;
        var candidateImageId = selectedRow.CandidateImageId;
        var result = await _operationGate.RunExclusiveAsync(
            async cancellationToken =>
            {
                await _reviewWorkflowCoordinator.ApplyFinalApprovalAsync(
                    projectId,
                    selectedRow,
                    approve,
                    reviewer,
                    notes,
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
            return false;
        }

        if (!TryApplyProjectReloadSnapshot(result.Value.Snapshot))
        {
            return false;
        }

        ImageSeriesReviewWorkspace.SelectedReviewRow = ReviewRows.FirstOrDefault(
            row => row.CandidateImageId == result.Value.CandidateImageId)
            ?? ImageSeriesReviewWorkspace.SelectedReviewRow;
        return true;
    }

    private void OnImageSeriesReviewMutated()
    {
        ImageSeriesDeliveryWorkspace.ApplyProjection([]);
        ImageSeriesDeliveryWorkspace.NotifyCommandStatesChanged();
    }

    private void OnImageSeriesReviewProjectionChanged()
    {
        ImageSeriesDeliveryWorkspace.NotifyCommandStatesChanged();
        RebuildWorkflowGraphRows();
    }

    private async Task<IReadOnlyList<DeliveryRowViewModel>?> RunImageSeriesDeliveryAsync(
        FinalImageDeliveryCategory category,
        string deliveryRootPath)
    {
        if (SelectedProject is null)
        {
            return null;
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
                cancellationToken,
                category,
                deliveryRootPath));

        if (!result.Executed || result.Value is null)
        {
            return null;
        }

        return result.Value.DeliveryRows;
    }

    private void OnImageSeriesDeliveryProjectionChanged()
    {
        RebuildWorkflowGraphRows();
    }

    private async Task<bool> RunImageSeriesCreateSeriesAsync(string title, string description)
    {
        if (SelectedProject is null)
        {
            return false;
        }

        var projectId = SelectedProject.Id;
        var result = await _operationGate.RunExclusiveAsync(
            async cancellationToken =>
            {
                var seriesId = await _planEditorWorkflowCoordinator.CreateSeriesAsync(
                    projectId,
                    title,
                    description,
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
            return false;
        }

        return TryApplyProjectReloadSnapshot(result.Value.Snapshot);
    }

    private async Task<bool> RunImageSeriesAddItemAsync(
        SeriesSummaryViewModel selectedSeries,
        string title,
        string brief)
    {
        if (SelectedProject is null)
        {
            return false;
        }

        var projectId = SelectedProject.Id;
        var selectedSeriesId = selectedSeries.Id;
        var result = await _operationGate.RunExclusiveAsync(
            async cancellationToken =>
            {
                var itemId = await _planEditorWorkflowCoordinator.AddItemAsync(
                    projectId,
                    selectedSeriesId,
                    title,
                    brief,
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
            return false;
        }

        return TryApplyProjectReloadSnapshot(result.Value.Snapshot);
    }

    private async Task<bool> RunImageSeriesCreatePromptVersionAsync(
        SeriesSummaryViewModel selectedSeries,
        SeriesItemViewModel selectedItem,
        string promptText)
    {
        if (SelectedProject is null)
        {
            return false;
        }

        var projectId = SelectedProject.Id;
        var selectedSeriesId = selectedSeries.Id;
        var selectedItemId = selectedItem.Id;
        var operation = await _operationGate.RunExclusiveAsync(
            async cancellationToken =>
            {
                var promptVersionId = await _planEditorWorkflowCoordinator.CreatePromptVersionAsync(
                    projectId,
                    selectedItemId,
                    promptText,
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
            return false;
        }

        return TryApplyProjectReloadSnapshot(operation.Value.Snapshot);
    }

    private void OnImageSeriesSelectedSeriesChanged(SeriesSummaryViewModel? selectedSeries)
    {
        ImageSeriesBriefWorkspace.NotifyCommandStatesChanged();
    }

    private void OnImageSeriesSelectedItemChanged(SeriesItemViewModel? selectedItem)
    {
        ImageSeriesBriefWorkspace.NotifyCommandStatesChanged();
    }

    private void OnImageSeriesPromptProjectionChanged()
    {
        RunFakeGenerationCommand.NotifyCanExecuteChanged();
    }

    private sealed record AddItemReloadResult(Guid ItemId, ProjectReloadSnapshot Snapshot);

    private sealed record CreatePromptVersionReloadResult(Guid PromptVersionId, ProjectReloadSnapshot Snapshot);

    private sealed record CreateSeriesReloadResult(Guid SeriesId, ProjectReloadSnapshot Snapshot);

    private sealed record FinalApprovalReloadResult(Guid CandidateImageId, ProjectReloadSnapshot Snapshot);

}
