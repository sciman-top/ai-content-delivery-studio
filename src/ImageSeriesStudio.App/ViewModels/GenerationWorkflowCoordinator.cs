using System.IO;
using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Core.Generation;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;

namespace ImageSeriesStudio.App.ViewModels;

public sealed class GenerationWorkflowCoordinator
{
    private readonly ProjectApplicationService _projectService;

    public GenerationWorkflowCoordinator(ProjectApplicationService projectService)
    {
        _projectService = projectService;
    }

    public async Task<GenerationWorkflowRunResult> RunFakeGenerationAsync(
        Guid projectId,
        IReadOnlyList<SeriesSummaryViewModel> series,
        CancellationToken cancellationToken)
    {
        var outputDirectory = LocalStudioDataPaths.ResolveProjectDirectory("generated", projectId);
        var run = await _projectService.RunGenerationQueueAsync(
            projectId,
            outputDirectory,
            cancellationToken);

        return new GenerationWorkflowRunResult(
            run,
            BuildQueueRows(run, series),
            BuildGalleryRows(run, series));
    }

    public async Task<GalleryRowViewModel> RunFakeImageEditAsync(
        Guid projectId,
        GalleryRowViewModel selectedRow,
        string editPrompt,
        string? maskPath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selectedRow);

        var outputDirectory = LocalStudioDataPaths.ResolveProjectDirectory("edited", projectId);

        var result = await _projectService.RunImageEditAsync(
            new ImageEditWorkflowRequest(
                projectId,
                selectedRow.SeriesItemId,
                selectedRow.CandidateImageId,
                selectedRow.AssetPath,
                string.IsNullOrWhiteSpace(maskPath) ? null : maskPath.Trim(),
                editPrompt.Trim(),
                CreateDefaultGenerationSettings(),
                outputDirectory,
                CreateImageEditOutputFileName(selectedRow)),
            cancellationToken);

        return new GalleryRowViewModel(
            result.CandidateImageId,
            selectedRow.SeriesItemId,
            $"{selectedRow.ItemTitle} (edited)",
            result.AssetPath,
            result.MetadataPath,
            BuildEditedPromptText(selectedRow.PromptText, editPrompt.Trim()));
    }

    private static IReadOnlyList<QueueRowViewModel> BuildQueueRows(
        GenerationQueueRun run,
        IReadOnlyList<SeriesSummaryViewModel> series)
    {
        var itemTitles = series
            .SelectMany(row => row.Items)
            .ToDictionary(item => item.Id, item => item.Title);
        var imageIndex = 0;
        var images = run.Images.ToArray();

        return run.Tasks.Select(task =>
        {
            var outputPath = string.Empty;
            if (task.Status is GenerationTaskStatus.Succeeded && imageIndex < images.Length)
            {
                outputPath = images[imageIndex].AssetPath;
                imageIndex++;
            }

            return new QueueRowViewModel(
                itemTitles.GetValueOrDefault(task.SeriesItemId, task.SeriesItemId.ToString("N")),
                task.Status.ToString(),
                task.AttemptCount.ToString(),
                outputPath,
                task.ErrorMessage ?? string.Empty);
        }).ToArray();
    }

    private static IReadOnlyList<GalleryRowViewModel> BuildGalleryRows(
        GenerationQueueRun run,
        IReadOnlyList<SeriesSummaryViewModel> series)
    {
        var itemTitles = series
            .SelectMany(row => row.Items)
            .ToDictionary(item => item.Id, item => item.Title);
        var prompts = series
            .SelectMany(row => row.Items)
            .SelectMany(item => item.PromptVersions)
            .ToDictionary(prompt => prompt.Id, prompt => prompt.PromptText);
        var succeededTasks = run.Tasks
            .Where(task => task.Status is GenerationTaskStatus.Succeeded)
            .ToArray();

        return run.Images.Select((image, index) =>
        {
            var task = index < succeededTasks.Length ? succeededTasks[index] : null;
            var itemTitle = task is null
                ? image.CandidateImageId.ToString("N")
                : itemTitles.GetValueOrDefault(task.SeriesItemId, task.SeriesItemId.ToString("N"));
            var promptText = task is null
                ? string.Empty
                : prompts.GetValueOrDefault(task.PromptVersionId, string.Empty);

            return new GalleryRowViewModel(
                image.CandidateImageId,
                task?.SeriesItemId ?? Guid.Empty,
                itemTitle,
                image.AssetPath,
                image.MetadataPath,
                promptText);
        }).ToArray();
    }

    private static GenerationSettings CreateDefaultGenerationSettings()
    {
        return new GenerationSettings(1024, 1024, "standard", "png");
    }

    private static string CreateImageEditOutputFileName(GalleryRowViewModel row)
    {
        return $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{row.CandidateImageId:N}-{SanitizeFileName(row.ItemTitle)}-edited.png";
    }

    private static string BuildEditedPromptText(string sourcePrompt, string editPrompt)
    {
        return string.IsNullOrWhiteSpace(sourcePrompt)
            ? editPrompt
            : string.Join(
                Environment.NewLine,
                [
                    $"Source prompt: {sourcePrompt}",
                    $"Edit instruction: {editPrompt}",
                ]);
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(character => invalidChars.Contains(character) ? '-' : character).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "image" : sanitized.Trim();
    }
}

public sealed record GenerationWorkflowRunResult(
    GenerationQueueRun Run,
    IReadOnlyList<QueueRowViewModel> QueueRows,
    IReadOnlyList<GalleryRowViewModel> GalleryRows);
