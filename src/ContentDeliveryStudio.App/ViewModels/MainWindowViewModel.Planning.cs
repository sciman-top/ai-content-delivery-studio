using System.IO;
using CommunityToolkit.Mvvm.Input;
using ContentDeliveryStudio.Application.Sources;
using ContentDeliveryStudio.Core.Documents;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Sources;

namespace ContentDeliveryStudio.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private async Task<bool> RunImageSeriesFakePlanningAsync(
        string goal,
        string audience,
        int itemCount,
        string styleBrief)
    {
        if (SelectedProject is null)
        {
            return false;
        }

        var projectId = SelectedProject.Id;
        var selectedSeriesItemId = SelectedSeriesItem?.Id;
        var result = await _operationGate.RunExclusiveAsync(
            async cancellationToken =>
            {
                var planningResult = await _planningWorkflowCoordinator.RunFakePlanningAsync(
                    projectId,
                    goal,
                    audience,
                    itemCount,
                    styleBrief,
                    cancellationToken);

                return await CaptureProjectReloadSnapshotAsync(
                    projectId,
                    planningResult.SeriesId,
                    selectedSeriesItemId,
                    cancellationToken);
            });

        if (!result.Executed || result.Value is null)
        {
            return false;
        }

        return TryApplyProjectReloadSnapshot(result.Value);
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
                var planningResult = await _planningWorkflowCoordinator.RunFakeDocumentPlanningAsync(
                    currentProject.Id,
                    currentProject.Name,
                    NewDocumentSourceText,
                    NewDocumentAudience,
                    SelectedDocumentStrictnessOption?.Value ?? IllustrationStrictnessLevel.Educational,
                    _defaultDocumentAudience,
                    cancellationToken);
                var workspace = await _projectWorkspaceCoordinator.RefreshProjectsAsync(
                    currentProject.Id,
                    cancellationToken);
                var snapshot = await CaptureProjectReloadSnapshotAsync(
                    currentProject.Id,
                    planningResult.SeriesId,
                    selectedItemId: null,
                    cancellationToken);

                return new DocumentPlanningReloadResult(
                    workspace,
                    planningResult.ResultSummary,
                    ActivityItems.Concat([planningResult.ResultSummary]).ToArray(),
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

    private async Task<bool> RunImageSeriesCreateBriefAsync(
        SeriesSummaryViewModel selectedSeries,
        string goal,
        string audience,
        string styleBrief)
    {
        if (SelectedProject is null)
        {
            return false;
        }

        var projectId = SelectedProject.Id;
        var selectedSeriesId = selectedSeries.Id;
        var selectedItemId = SelectedSeriesItem?.Id;
        var result = await _operationGate.RunExclusiveAsync(
            async cancellationToken =>
            {
                var briefId = await _briefWorkflowCoordinator.CreateBriefAsync(
                    projectId,
                    selectedSeries,
                    goal,
                    audience,
                    styleBrief,
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
            return false;
        }

        return TryApplyProjectReloadSnapshot(result.Value.Snapshot);
    }

    private async Task<bool> RunImageSeriesGeneratePromptDirectionsAsync(
        SeriesSummaryViewModel selectedSeries,
        string goal,
        string audience,
        string styleBrief)
    {
        if (SelectedProject is null)
        {
            return false;
        }

        var projectId = SelectedProject.Id;
        var selectedSeriesId = selectedSeries.Id;
        var selectedItemId = SelectedSeriesItem?.Id;
        var result = await _operationGate.RunExclusiveAsync(
            async cancellationToken =>
            {
                var brief = await _briefWorkflowCoordinator.GeneratePromptDirectionsAsync(
                    projectId,
                    selectedSeries,
                    _activeCreativeBriefId,
                    goal,
                    audience,
                    styleBrief,
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
            return false;
        }

        if (!TryApplyProjectReloadSnapshot(result.Value.Snapshot))
        {
            return false;
        }

        ImageSeriesBriefWorkspace.SelectedPromptDirection = PromptDirectionRows.FirstOrDefault(
            direction => direction.CreativeBriefId == result.Value.BriefId)
            ?? PromptDirectionRows.FirstOrDefault();
        return true;
    }

    private async Task<bool> RunImageSeriesGenerateDesignBlueprintsAsync(
        SeriesSummaryViewModel selectedSeries,
        string goal,
        string audience,
        string styleBrief)
    {
        if (SelectedProject is null)
        {
            return false;
        }

        var projectId = SelectedProject.Id;
        var selectedSeriesId = selectedSeries.Id;
        var selectedItemId = SelectedSeriesItem?.Id;
        var result = await _operationGate.RunExclusiveAsync(
            async cancellationToken =>
            {
                var brief = await _briefWorkflowCoordinator.GenerateDesignBlueprintsAsync(
                    projectId,
                    selectedSeries,
                    _activeCreativeBriefId,
                    goal,
                    audience,
                    styleBrief,
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
            return false;
        }

        if (!TryApplyProjectReloadSnapshot(result.Value.Snapshot))
        {
            return false;
        }

        ImageSeriesBriefWorkspace.SelectedDesignBlueprint = DesignBlueprintRows.FirstOrDefault(
            blueprint => blueprint.CreativeBriefId == result.Value.BriefId)
            ?? DesignBlueprintRows.FirstOrDefault();
        return true;
    }

    private async Task<bool> RunImageSeriesPromoteDesignBlueprintAsync(
        DesignBlueprintRowViewModel selectedBlueprint)
    {
        if (SelectedProject is null)
        {
            return false;
        }

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
            return false;
        }

        if (!TryApplyProjectReloadSnapshot(result.Value.Snapshot))
        {
            return false;
        }

        ImageSeriesBriefWorkspace.SelectedDesignBlueprint = DesignBlueprintRows.FirstOrDefault(
            blueprint => blueprint.BlueprintId == result.Value.BlueprintId)
            ?? DesignBlueprintRows.FirstOrDefault();
        return true;
    }

    private async Task<bool> RunImageSeriesPromotePromptDirectionAsync(
        SeriesItemViewModel selectedItem,
        PromptDirectionRowViewModel selectedDirection)
    {
        if (SelectedProject is null)
        {
            return false;
        }

        var projectId = SelectedProject.Id;
        var selectedSeriesId = SelectedSeries?.Id;
        var selectedItemId = selectedItem.Id;
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
            return false;
        }

        return TryApplyProjectReloadSnapshot(operation.Value);
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
