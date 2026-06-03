using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Core.Projects;

namespace ImageSeriesStudio.Application.RepairRouting;

public sealed class ReviewRepairApplicationService
{
    private readonly IProjectRepository _repository;

    public ReviewRepairApplicationService(IProjectRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<ReviewOutcomeRoutingPlan> RouteReviewOutcomes(
        IReadOnlyList<StructuredReviewOutput> reviews)
    {
        ArgumentNullException.ThrowIfNull(reviews);

        return reviews
            .Select(ReviewOutcomeRoutingPlan.FromReview)
            .ToArray();
    }

    public async Task<RoutedRepairApplicationResult> ApplyRoutedRepairAsync(
        RoutedRepairApplicationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.RepairPlan);

        var appliedLayers = GetApplicableRepairLayers(request.RepairPlan);
        if (appliedLayers.Count == 0)
        {
            throw new InvalidOperationException("Routed repair application currently supports Prompt or Settings routes.");
        }

        var project = await RequireProjectAsync(request.ProjectId, cancellationToken);
        var item = RequireSeriesItem(project, request.SeriesItemId);
        var originalPrompt = item.PromptVersions.SingleOrDefault(prompt => prompt.Id == request.PromptVersionId)
            ?? throw new InvalidOperationException($"Prompt version not found: {request.PromptVersionId}");
        var settings = appliedLayers.Contains(ReviewOutcomeTargetLayer.Settings)
            ? request.RevisedSettings
                ?? throw new InvalidOperationException("Settings repair requires revised generation settings.")
            : originalPrompt.Settings;
        var prompt = item.AddPromptVersion(
            BuildRoutedRepairPromptText(request.RevisedPromptText, request.RepairPlan),
            settings,
            originalPrompt.ProviderProfileId,
            request.Timestamp);

        await _repository.SaveAsync(project, cancellationToken);
        return new RoutedRepairApplicationResult(prompt, appliedLayers);
    }

    public RoutedRepairPatch CreateRoutedRepairPatch(RepairPlan repairPlan, DateTimeOffset createdAt)
    {
        return RoutedRepairPatch.FromRepairPlan(repairPlan, createdAt);
    }

    public async Task<RoutedRepairPatch> CreateRoutedRepairPatchAsync(
        RoutedRepairPatchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.RepairPlan);

        var project = await RequireProjectAsync(request.ProjectId, cancellationToken);
        var patch = RoutedRepairPatch.FromRepairPlan(project.Id, request.RepairPlan, request.Timestamp);
        project.AddRoutedRepairPatch(patch, request.Timestamp);

        await _repository.SaveAsync(project, cancellationToken);
        return patch;
    }

    public async Task<RoutedRepairPatchApplicationResult> ApplyRoutedRepairPatchAsync(
        RoutedRepairPatchApplicationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var project = await RequireProjectAsync(request.ProjectId, cancellationToken);
        var brief = RequireCreativeBrief(project, request.CreativeBriefId);
        var patch = RequireRoutedRepairPatch(project, request.RoutedRepairPatchId);

        DesignBlueprint? targetBlueprint = null;
        if (patch.Items.Any(item => item.TargetLayer is ReviewOutcomeTargetLayer.Blueprint))
        {
            targetBlueprint = request.DesignBlueprintId is { } targetBlueprintId
                ? RequireDesignBlueprint(brief, targetBlueprintId)
                : brief.DesignBlueprints.SingleOrDefault(blueprint => blueprint.Id == brief.PromotedBlueprintId)
                    ?? throw new InvalidOperationException("Blueprint repair patch requires a promoted blueprint or explicit target blueprint.");
        }

        var result = brief.ApplyRoutedRepairPatch(patch, targetBlueprint, request.Timestamp);

        await _repository.SaveAsync(project, cancellationToken);
        return result;
    }

    private static SeriesItem RequireSeriesItem(ImageProject project, Guid seriesItemId)
    {
        return project.Series
            .SelectMany(series => series.Items)
            .SingleOrDefault(item => item.Id == seriesItemId)
            ?? throw new InvalidOperationException($"Series item not found: {seriesItemId}");
    }

    private static CreativeBrief RequireCreativeBrief(ImageProject project, Guid creativeBriefId)
    {
        return project.Series
            .SelectMany(series => series.CreativeBriefs)
            .SingleOrDefault(brief => brief.Id == creativeBriefId)
            ?? throw new InvalidOperationException($"Creative brief not found: {creativeBriefId}");
    }

    private static RoutedRepairPatch RequireRoutedRepairPatch(ImageProject project, Guid routedRepairPatchId)
    {
        return project.RoutedRepairPatches
            .SingleOrDefault(patch => patch.Id == routedRepairPatchId)
            ?? throw new InvalidOperationException($"Routed repair patch not found: {routedRepairPatchId}");
    }

    private static DesignBlueprint RequireDesignBlueprint(CreativeBrief brief, Guid designBlueprintId)
    {
        return brief.DesignBlueprints
            .SingleOrDefault(blueprint => blueprint.Id == designBlueprintId)
            ?? throw new InvalidOperationException($"Design blueprint not found: {designBlueprintId}");
    }

    private static IReadOnlyList<ReviewOutcomeTargetLayer> GetApplicableRepairLayers(RepairPlan repairPlan)
    {
        return repairPlan.Steps
            .Where(step => step.TargetLayer is ReviewOutcomeTargetLayer.Prompt or ReviewOutcomeTargetLayer.Settings)
            .OrderBy(step => step.Order)
            .Select(step => step.TargetLayer)
            .Distinct()
            .ToArray();
    }

    private static string BuildRoutedRepairPromptText(string revisedPromptText, RepairPlan repairPlan)
    {
        if (string.IsNullOrWhiteSpace(revisedPromptText))
        {
            throw new ArgumentException("Revised prompt text cannot be empty.", nameof(revisedPromptText));
        }

        var lines = new List<string>
        {
            revisedPromptText.Trim(),
            string.Empty,
            "Applied repair routes:",
        };
        foreach (var step in repairPlan.Steps
            .Where(step => step.TargetLayer is ReviewOutcomeTargetLayer.Prompt or ReviewOutcomeTargetLayer.Settings)
            .OrderBy(step => step.Order))
        {
            lines.Add($"- {step.TargetLayer} ({step.Severity})");
            lines.AddRange(step.RecommendedActions.Select(action => $"  Action: {action}"));
            lines.AddRange(step.Evidence.Select(evidence => $"  Evidence: {evidence}"));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private async Task<ImageProject> RequireProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await _repository.LoadAsync(projectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project not found: {projectId}");
    }
}

public sealed record RoutedRepairApplicationRequest(
    Guid ProjectId,
    Guid SeriesItemId,
    Guid PromptVersionId,
    RepairPlan RepairPlan,
    string RevisedPromptText,
    GenerationSettings? RevisedSettings,
    DateTimeOffset Timestamp);

public sealed record RoutedRepairApplicationResult(
    PromptVersion PromptVersion,
    IReadOnlyList<ReviewOutcomeTargetLayer> AppliedLayers);

public sealed record RoutedRepairPatchRequest(
    Guid ProjectId,
    RepairPlan RepairPlan,
    DateTimeOffset Timestamp);

public sealed record RoutedRepairPatchApplicationRequest(
    Guid ProjectId,
    Guid CreativeBriefId,
    Guid RoutedRepairPatchId,
    DateTimeOffset Timestamp,
    Guid? DesignBlueprintId = null);
