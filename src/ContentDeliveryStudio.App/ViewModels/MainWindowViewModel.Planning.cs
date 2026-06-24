using System.IO;
using CommunityToolkit.Mvvm.Input;
using ContentDeliveryStudio.Application.Sources;
using ContentDeliveryStudio.Core.Documents;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Sources;

namespace ContentDeliveryStudio.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    [RelayCommand(CanExecute = nameof(CanRunFakePlanning))]
    private async Task RunFakePlanningAsync()
    {
        if (SelectedProject is null || !TryGetPlanningItemCount(out var itemCount))
        {
            return;
        }

        var projectId = SelectedProject.Id;
        var selectedSeriesItemId = SelectedSeriesItem?.Id;
        var result = await _operationGate.RunExclusiveAsync(
            async cancellationToken =>
            {
                var planningResult = await _planningWorkflowCoordinator.RunFakePlanningAsync(
                    projectId,
                    NewPlanningGoal,
                    NewPlanningAudience,
                    itemCount,
                    NewPlanningStyleBrief,
                    cancellationToken);

                return await CaptureProjectReloadSnapshotAsync(
                    projectId,
                    planningResult.SeriesId,
                    selectedSeriesItemId,
                    cancellationToken);
            });

        if (!result.Executed || result.Value is null)
        {
            return;
        }

        TryApplyProjectReloadSnapshot(result.Value);
    }

    private bool CanRunFakePlanning()
    {
        return CanRunMutation()
            && SelectedProject is not null
            && !string.IsNullOrWhiteSpace(NewPlanningGoal)
            && !string.IsNullOrWhiteSpace(NewPlanningAudience)
            && TryGetPlanningItemCount(out _);
    }

    [RelayCommand(CanExecute = nameof(CanRunFakeDocumentPlanning))]
    private async Task RunFakeDocumentPlanningAsync()
    {
        if (SelectedProject is null || string.IsNullOrWhiteSpace(NewDocumentSourceText))
        {
            return;
        }

        DocumentPlanningResultSummary = string.Empty;
        var currentProject = SelectedProject;
        var result = await _operationGate.RunExclusiveAsync(
            async cancellationToken =>
            {
                var planningResult = await _workbenchInspectorCoordinator.RunFakeDocumentPlanningAsync(
                    currentProject,
                    NewDocumentSourceText,
                    NewDocumentAudience,
                    SelectedDocumentStrictnessOption?.Value ?? IllustrationStrictnessLevel.Educational,
                    _defaultDocumentAudience,
                    ActivityItems,
                    cancellationToken);
                var snapshot = await CaptureProjectReloadSnapshotAsync(
                    currentProject.Id,
                    planningResult.SeriesId,
                    selectedItemId: null,
                    cancellationToken);

                return new DocumentPlanningReloadResult(
                    planningResult.Workspace,
                    planningResult.ResultSummary,
                    planningResult.ActivityItems,
                    snapshot);
            });

        if (!result.Executed || result.Value is null)
        {
            return;
        }

        if (SelectedProject?.Id != currentProject.Id)
        {
            return;
        }

        ApplyWorkspaceResult(
            result.Value.Workspace,
            queueSelectedProjectLoad: false,
            selectedProjectIdOverride: currentProject.Id);
        if (!TryApplyProjectReloadSnapshot(result.Value.Snapshot))
        {
            return;
        }

        DocumentPlanningResultSummary = result.Value.ResultSummary;
        ActivityItems = result.Value.ActivityItems;
    }

    private bool CanRunFakeDocumentPlanning()
    {
        return CanRunMutation()
            && SelectedProject is not null
            && !string.IsNullOrWhiteSpace(NewDocumentSourceText);
    }

    [RelayCommand(CanExecute = nameof(CanImportDocumentSourceFile))]
    private async Task ImportDocumentSourceFileAsync(string? filePath)
    {
        if (SelectedProject is null)
        {
            return;
        }

        var normalizedPath = NormalizeDocumentSourceFilePath(filePath);
        if (normalizedPath is null)
        {
            return;
        }

        var sourceKind = ResolveDocumentSourceKind(normalizedPath);
        if (sourceKind is null)
        {
            return;
        }

        var projectId = SelectedProject.Id;
        var importResult = await _operationGate.RunExclusiveAsync(
            async cancellationToken =>
            {
                await _projectService.IngestSourceAsync(
                    projectId,
                    new SourceIngestionRequest(
                        sourceKind.Value,
                        Path.GetFileName(normalizedPath),
                        string.Empty,
                        OriginalPath: normalizedPath,
                        MimeType: ResolveDocumentMimeType(sourceKind.Value),
                        SizeBytes: new FileInfo(normalizedPath).Length,
                        Sha256: null),
                    DateTimeOffset.UtcNow,
                    cancellationToken);

                var loadedProject = await _projectService.LoadProjectAsync(projectId, cancellationToken)
                    ?? throw new InvalidOperationException($"Project not found: {projectId}");
                var importedAsset = loadedProject.SourceAssets.LastOrDefault(asset =>
                    string.Equals(asset.OriginalPath, normalizedPath, StringComparison.OrdinalIgnoreCase));

                var importedText = importedAsset is null || importedAsset.ExtractedContents.Count == 0
                    ? string.Empty
                    : string.Join(
                        Environment.NewLine,
                        importedAsset.ExtractedContents.Select(content => content.Text));

                return new DocumentSourceImportResult(normalizedPath, importedText);
            });

        if (!importResult.Executed || importResult.Value is null)
        {
            return;
        }

        if (SelectedProject?.Id == projectId)
        {
            NewDocumentSourceText = importResult.Value.ImportedText;
            ImportedDocumentSourcePath = importResult.Value.NormalizedPath;
            NewDocumentSourceFilePath = importResult.Value.NormalizedPath;
            ActivityItems = new[] { $"Imported {Path.GetFileName(importResult.Value.NormalizedPath)}." }
                .Concat(ActivityItems)
                .ToArray();
            RunFakeDocumentPlanningCommand.NotifyCanExecuteChanged();
        }

        await _operationGate.RunLatestWinsAsync(
            MainWindowOperationLane.DocumentSourceState,
            cancellationToken => RefreshWorkspaceIfProjectStillSelectedAsync(
                projectId,
                queueSelectedProjectLoad: false,
                cancellationToken));
    }

    private bool CanImportDocumentSourceFile(string? filePath)
    {
        return CanRunMutation()
            && SelectedProject is not null
            && SupportsDocumentSourceFile(filePath);
    }

    [RelayCommand]
    private async Task BrowseDocumentSourceFileAsync()
    {
        if (_documentSourceFilePickerService is null)
        {
            return;
        }

        var filePath = await _operationGate.RunLatestWinsAsync(
            MainWindowOperationLane.DocumentSourceState,
            cancellationToken => _documentSourceFilePickerService.PickAsync(cancellationToken));

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        if (ImportDocumentSourceFileCommand.CanExecute(filePath))
        {
            await ImportDocumentSourceFileCommand.ExecuteAsync(filePath);
        }
    }

    [RelayCommand(CanExecute = nameof(CanCreateBrief))]
    private async Task CreateBriefAsync()
    {
        if (SelectedProject is null || SelectedSeries is null)
        {
            return;
        }

        var projectId = SelectedProject.Id;
        var selectedSeriesId = SelectedSeries.Id;
        var selectedItemId = SelectedSeriesItem?.Id;
        var result = await _operationGate.RunExclusiveAsync(
            async cancellationToken =>
            {
                var briefId = await _briefWorkflowCoordinator.CreateBriefAsync(
                    projectId,
                    SelectedSeries,
                    NewPlanningGoal,
                    NewPlanningAudience,
                    NewPlanningStyleBrief,
                    cancellationToken);
                var snapshot = await CaptureProjectReloadSnapshotAsync(
                    projectId,
                    selectedSeriesId,
                    selectedItemId,
                    cancellationToken,
                    activeCreativeBriefId: briefId);

                return new CreativeBriefReloadResult(briefId, snapshot);
            });

        if (!result.Executed || result.Value is null)
        {
            return;
        }

        TryApplyProjectReloadSnapshot(result.Value.Snapshot);
    }

    private bool CanCreateBrief()
    {
        return CanRunMutation()
            && SelectedProject is not null
            && SelectedSeries is not null
            && !string.IsNullOrWhiteSpace(NewPlanningGoal)
            && !string.IsNullOrWhiteSpace(NewPlanningAudience);
    }

    [RelayCommand(CanExecute = nameof(CanGeneratePromptDirections))]
    private async Task GeneratePromptDirectionsAsync()
    {
        if (SelectedProject is null || SelectedSeries is null)
        {
            return;
        }

        var projectId = SelectedProject.Id;
        var selectedSeriesId = SelectedSeries.Id;
        var selectedItemId = SelectedSeriesItem?.Id;
        var result = await _operationGate.RunExclusiveAsync(
            async cancellationToken =>
            {
                var brief = await _briefWorkflowCoordinator.GeneratePromptDirectionsAsync(
                    projectId,
                    SelectedSeries,
                    _activeCreativeBriefId,
                    NewPlanningGoal,
                    NewPlanningAudience,
                    NewPlanningStyleBrief,
                    cancellationToken);
                var snapshot = await CaptureProjectReloadSnapshotAsync(
                    projectId,
                    selectedSeriesId,
                    selectedItemId,
                    cancellationToken,
                    activeCreativeBriefId: brief.Id);

                return new CreativeBriefReloadResult(brief.Id, snapshot);
            });

        if (!result.Executed || result.Value is null)
        {
            return;
        }

        if (!TryApplyProjectReloadSnapshot(result.Value.Snapshot))
        {
            return;
        }

        SelectedPromptDirection = PromptDirectionRows.FirstOrDefault(direction => direction.CreativeBriefId == result.Value.BriefId)
            ?? PromptDirectionRows.FirstOrDefault();
    }

    private bool CanGeneratePromptDirections()
    {
        return CanCreateBrief();
    }

    [RelayCommand(CanExecute = nameof(CanGenerateDesignBlueprints))]
    private async Task GenerateDesignBlueprintsAsync()
    {
        if (SelectedProject is null || SelectedSeries is null)
        {
            return;
        }

        var projectId = SelectedProject.Id;
        var selectedSeriesId = SelectedSeries.Id;
        var selectedItemId = SelectedSeriesItem?.Id;
        var result = await _operationGate.RunExclusiveAsync(
            async cancellationToken =>
            {
                var brief = await _briefWorkflowCoordinator.GenerateDesignBlueprintsAsync(
                    projectId,
                    SelectedSeries,
                    _activeCreativeBriefId,
                    NewPlanningGoal,
                    NewPlanningAudience,
                    NewPlanningStyleBrief,
                    cancellationToken);
                var snapshot = await CaptureProjectReloadSnapshotAsync(
                    projectId,
                    selectedSeriesId,
                    selectedItemId,
                    cancellationToken,
                    activeCreativeBriefId: brief.Id);

                return new CreativeBriefReloadResult(brief.Id, snapshot);
            });

        if (!result.Executed || result.Value is null)
        {
            return;
        }

        if (!TryApplyProjectReloadSnapshot(result.Value.Snapshot))
        {
            return;
        }

        SelectedDesignBlueprint = DesignBlueprintRows.FirstOrDefault(blueprint => blueprint.CreativeBriefId == result.Value.BriefId)
            ?? DesignBlueprintRows.FirstOrDefault();
    }

    private bool CanGenerateDesignBlueprints()
    {
        return CanCreateBrief();
    }

    [RelayCommand(CanExecute = nameof(CanPromoteDesignBlueprint))]
    private async Task PromoteDesignBlueprintAsync()
    {
        if (SelectedProject is null || SelectedDesignBlueprint is null)
        {
            return;
        }

        var selectedBlueprint = SelectedDesignBlueprint;
        var projectId = SelectedProject.Id;
        var selectedSeriesId = SelectedSeries?.Id;
        var selectedItemId = SelectedSeriesItem?.Id;
        var result = await _operationGate.RunExclusiveAsync(
            async cancellationToken =>
            {
                var blueprint = await _projectService.PromoteDesignBlueprintAsync(
                    projectId,
                    selectedBlueprint.CreativeBriefId,
                    selectedBlueprint.BlueprintId,
                    DateTimeOffset.UtcNow,
                    cancellationToken);
                var snapshot = await CaptureProjectReloadSnapshotAsync(
                    projectId,
                    selectedSeriesId,
                    selectedItemId,
                    cancellationToken,
                    activeCreativeBriefId: selectedBlueprint.CreativeBriefId);

                return new DesignBlueprintPromotionReloadResult(blueprint.Id, snapshot);
            });

        if (!result.Executed || result.Value is null)
        {
            return;
        }

        if (!TryApplyProjectReloadSnapshot(result.Value.Snapshot))
        {
            return;
        }

        SelectedDesignBlueprint = DesignBlueprintRows.FirstOrDefault(blueprint => blueprint.BlueprintId == result.Value.BlueprintId)
            ?? DesignBlueprintRows.FirstOrDefault();
    }

    private bool CanPromoteDesignBlueprint()
    {
        return CanRunMutation()
            && SelectedProject is not null
            && SelectedDesignBlueprint is not null;
    }

    [RelayCommand(CanExecute = nameof(CanPromotePromptDirection))]
    private async Task PromotePromptDirectionAsync()
    {
        if (SelectedProject is null || SelectedSeriesItem is null || SelectedPromptDirection is null)
        {
            return;
        }

        var selectedDirection = SelectedPromptDirection;
        var projectId = SelectedProject.Id;
        var selectedSeriesId = SelectedSeries?.Id;
        var selectedItemId = SelectedSeriesItem.Id;
        var operation = await _operationGate.RunExclusiveAsync(
            async cancellationToken =>
            {
                await _projectService.PromotePromptDirectionAsync(
                    projectId,
                    selectedItemId,
                    selectedDirection.CreativeBriefId,
                    selectedDirection.DirectionKey,
                    DateTimeOffset.UtcNow,
                    cancellationToken);

                return await CaptureProjectReloadSnapshotAsync(
                    projectId,
                    selectedSeriesId,
                    selectedItemId,
                    cancellationToken,
                    activeCreativeBriefId: selectedDirection.CreativeBriefId);
            });

        if (!operation.Executed || operation.Value is null)
        {
            return;
        }

        TryApplyProjectReloadSnapshot(operation.Value);
    }

    private bool CanPromotePromptDirection()
    {
        return CanRunMutation()
            && SelectedProject is not null
            && SelectedSeriesItem is not null
            && SelectedPromptDirection is not null;
    }

    private async Task<Guid> EnsureActiveCreativeBriefIdAsync(CancellationToken cancellationToken)
    {
        if (SelectedProject is null || SelectedSeries is null)
        {
            throw new InvalidOperationException("A project and series must be selected before resolving a brief.");
        }

        var briefId = await _briefWorkflowCoordinator.EnsureActiveCreativeBriefIdAsync(
            SelectedProject.Id,
            SelectedSeries,
            _activeCreativeBriefId,
            NewPlanningGoal,
            NewPlanningAudience,
            NewPlanningStyleBrief,
            cancellationToken);
        _activeCreativeBriefId = briefId;
        return briefId;
    }

    private async Task<CreativeBrief> CreateBriefForSelectedSeriesAsync(CancellationToken cancellationToken)
    {
        if (SelectedProject is null || SelectedSeries is null)
        {
            throw new InvalidOperationException("A project and series must be selected before creating a brief.");
        }

        var briefId = await _briefWorkflowCoordinator.CreateBriefAsync(
            SelectedProject.Id,
            SelectedSeries,
            NewPlanningGoal,
            NewPlanningAudience,
            NewPlanningStyleBrief,
            cancellationToken);
        var project = await _projectService.LoadProjectAsync(SelectedProject.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Project not found: {SelectedProject.Id}");

        return project.Series
            .SelectMany(series => series.CreativeBriefs)
            .Single(brief => brief.Id == briefId);
    }

    private IReadOnlyList<string> BuildBriefMustInclude()
    {
        return BriefWorkflowCoordinator.BuildBriefMustInclude(SelectedSeries, NewPlanningGoal);
    }

    private sealed record CreativeBriefReloadResult(Guid BriefId, ProjectReloadSnapshot Snapshot);

    private sealed record DesignBlueprintPromotionReloadResult(Guid BlueprintId, ProjectReloadSnapshot Snapshot);

    private sealed record DocumentPlanningReloadResult(
        ProjectWorkspaceResult Workspace,
        string ResultSummary,
        IReadOnlyList<string> ActivityItems,
        ProjectReloadSnapshot Snapshot);

    private sealed record DocumentSourceImportResult(string NormalizedPath, string ImportedText);
}
