using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Core.Styles;

namespace ImageSeriesStudio.Application.Projects;

public sealed class BriefWorkflowApplicationService
{
    private readonly IProjectRepository _repository;
    private readonly ITextPlanningProvider? _textPlanningProvider;

    public BriefWorkflowApplicationService(
        IProjectRepository repository,
        ITextPlanningProvider? textPlanningProvider)
    {
        _repository = repository;
        _textPlanningProvider = textPlanningProvider;
    }

    public async Task<CreativeBrief> CreateCreativeBriefAsync(
        Guid projectId,
        Guid seriesId,
        string goal,
        string audience,
        ImageTextPolicy textPolicy,
        string styleIntent,
        IReadOnlyList<string> mustInclude,
        IReadOnlyList<string> mustAvoid,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var project = await RequireProjectAsync(projectId, cancellationToken);
        var series = project.Series.SingleOrDefault(series => series.Id == seriesId)
            ?? throw new InvalidOperationException($"Series not found: {seriesId}");
        var brief = series.AddCreativeBrief(
            goal,
            audience,
            textPolicy,
            styleIntent,
            mustInclude,
            mustAvoid,
            timestamp);

        await _repository.SaveAsync(project, cancellationToken);
        return brief;
    }

    public async Task<CreativeBrief> CreatePromptDirectionsAsync(
        Guid projectId,
        Guid creativeBriefId,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        if (_textPlanningProvider is null)
        {
            throw new InvalidOperationException("Text planning provider is not registered.");
        }

        var project = await RequireProjectAsync(projectId, cancellationToken);
        var brief = project.Series
            .SelectMany(series => series.CreativeBriefs)
            .SingleOrDefault(brief => brief.Id == creativeBriefId)
            ?? throw new InvalidOperationException($"Creative brief not found: {creativeBriefId}");
        var request = new BriefPlanningRequest(
            brief.Goal,
            brief.Audience,
            brief.StyleIntent,
            brief.MustInclude,
            brief.MustAvoid,
            DirectionCount: 3);
        ValidateBoundedTextPlanningRequest(request);

        var result = await _textPlanningProvider.CreatePromptDirectionsAsync(
            request,
            cancellationToken);

        brief.ReplaceDirections(
            result.Directions
                .Select(direction => PromptDirection.Create(
                    direction.Key,
                    direction.Name,
                    direction.IntendedUse,
                    direction.PromptText,
                    direction.NegativePrompt,
                    direction.Strength,
                    direction.Risk,
                    timestamp,
                    direction.Recommendation))
                .ToArray(),
            timestamp);

        await _repository.SaveAsync(project, cancellationToken);
        return brief;
    }

    public async Task<CreativeBrief> CreateDesignBlueprintsAsync(
        Guid projectId,
        Guid creativeBriefId,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        if (_textPlanningProvider is null)
        {
            throw new InvalidOperationException("Text planning provider is not registered.");
        }

        var project = await RequireProjectAsync(projectId, cancellationToken);
        var brief = project.Series
            .SelectMany(series => series.CreativeBriefs)
            .SingleOrDefault(candidate => candidate.Id == creativeBriefId)
            ?? throw new InvalidOperationException($"Creative brief not found: {creativeBriefId}");
        var request = new BlueprintPlanningRequest(
            brief.Goal,
            brief.Audience,
            brief.StyleIntent,
            brief.MustInclude,
            brief.MustAvoid,
            brief.TextPolicy,
            CandidateCount: 3);
        ValidateBoundedTextPlanningRequest(request);

        var result = await _textPlanningProvider.CreateDesignBlueprintsAsync(
            request,
            cancellationToken);

        brief.ReplaceBlueprints(
            result.Blueprints
                .Select(blueprint => DesignBlueprint.Create(
                    blueprint.Key,
                    blueprint.DisplayName,
                    blueprint.Category,
                    blueprint.Summary,
                    blueprint.IntendedUse,
                    blueprint.MinimumRecommendedItemCount,
                    blueprint.MaximumRecommendedItemCount,
                    blueprint.SupportsPanelSequence,
                    blueprint.DefaultTextPolicy,
                    blueprint.DefaultReviewRubricTemplateId,
                    blueprint.ConsistencyRules,
                    blueprint.VariationRules,
                    blueprint.RiskNotes,
                    timestamp))
                .ToArray(),
            timestamp);

        await _repository.SaveAsync(project, cancellationToken);
        return brief;
    }

    public async Task<DesignBlueprint> PromoteDesignBlueprintAsync(
        Guid projectId,
        Guid creativeBriefId,
        Guid blueprintId,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var project = await RequireProjectAsync(projectId, cancellationToken);
        var brief = project.Series
            .SelectMany(series => series.CreativeBriefs)
            .SingleOrDefault(candidate => candidate.Id == creativeBriefId)
            ?? throw new InvalidOperationException($"Creative brief not found: {creativeBriefId}");

        var blueprint = brief.PromoteBlueprint(blueprintId, timestamp);
        await _repository.SaveAsync(project, cancellationToken);
        return blueprint;
    }

    public async Task<PromptVersion> PromotePromptDirectionAsync(
        Guid projectId,
        Guid seriesItemId,
        Guid creativeBriefId,
        string directionKey,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var project = await RequireProjectAsync(projectId, cancellationToken);
        var (item, direction) = ResolvePromptDirectionTarget(
            project,
            seriesItemId,
            creativeBriefId,
            directionKey);
        var settings = direction.Recommendation is { } recommendation
            ? CreateGenerationSettings(recommendation)
            : CreateDefaultGenerationSettings();

        return await PromotePromptDirectionAsync(project, item, direction, settings, timestamp, cancellationToken);
    }

    public async Task<PromptVersion> PromotePromptDirectionAsync(
        Guid projectId,
        Guid seriesItemId,
        Guid creativeBriefId,
        string directionKey,
        GenerationSettings settings,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var project = await RequireProjectAsync(projectId, cancellationToken);
        var (item, direction) = ResolvePromptDirectionTarget(
            project,
            seriesItemId,
            creativeBriefId,
            directionKey);

        return await PromotePromptDirectionAsync(project, item, direction, settings, timestamp, cancellationToken);
    }

    private async Task<PromptVersion> PromotePromptDirectionAsync(
        ImageProject project,
        SeriesItem item,
        PromptDirection direction,
        GenerationSettings settings,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var providerProfile = ResolveProviderProfile(project, providerProfileId: null, timestamp);
        var prompt = item.AddPromptVersion(direction.PromptText, settings, providerProfile.Id, timestamp);
        await _repository.SaveAsync(project, cancellationToken);
        return prompt;
    }

    private static GenerationSettings CreateDefaultGenerationSettings()
    {
        return new GenerationSettings(1024, 1024, "standard", "png");
    }

    private static GenerationSettings CreateGenerationSettings(PromptDirectionRecommendation recommendation)
    {
        return new GenerationSettings(
            recommendation.Width,
            recommendation.Height,
            recommendation.QualityBand,
            recommendation.OutputFormat);
    }

    private static (SeriesItem Item, PromptDirection Direction) ResolvePromptDirectionTarget(
        ImageProject project,
        Guid seriesItemId,
        Guid creativeBriefId,
        string directionKey)
    {
        var item = project.Series
            .SelectMany(series => series.Items)
            .SingleOrDefault(item => item.Id == seriesItemId)
            ?? throw new InvalidOperationException($"Series item not found: {seriesItemId}");
        var brief = project.Series
            .SelectMany(series => series.CreativeBriefs)
            .SingleOrDefault(brief => brief.Id == creativeBriefId)
            ?? throw new InvalidOperationException($"Creative brief not found: {creativeBriefId}");
        var direction = brief.PromptDirections.SingleOrDefault(direction =>
            direction.Key.Equals(directionKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Prompt direction not found: {directionKey}");

        return (item, direction);
    }

    private static ProviderProfile ResolveProviderProfile(
        ImageProject project,
        Guid? providerProfileId,
        DateTimeOffset timestamp)
    {
        if (providerProfileId is { } requestedProfileId && requestedProfileId != Guid.Empty)
        {
            return project.ProviderProfiles.SingleOrDefault(profile => profile.Id == requestedProfileId)
                ?? throw new InvalidOperationException($"Provider profile not found: {requestedProfileId}");
        }

        return project.ProviderProfiles.FirstOrDefault(profile => profile.Kind is ProviderKind.Fake)
            ?? project.AddProviderProfile("Fake provider", ProviderKind.Fake, timestamp);
    }

    private async Task<ImageProject> RequireProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await _repository.LoadAsync(projectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project not found: {projectId}");
    }

    private static void ValidateBoundedTextPlanningRequest(BriefPlanningRequest request)
    {
        var estimatedCharacters = TextPlanningExecutionPolicy.EstimateInputCharacters(request);
        if (estimatedCharacters > TextPlanningExecutionPolicy.DefaultMaxInputCharacters)
        {
            throw new InvalidOperationException(
                $"Brief direction planning exceeds the bounded local-direct default of {TextPlanningExecutionPolicy.DefaultMaxInputCharacters} characters. Summarize or trim the brief locally before provider dispatch.");
        }
    }

    private static void ValidateBoundedTextPlanningRequest(BlueprintPlanningRequest request)
    {
        var estimatedCharacters = TextPlanningExecutionPolicy.EstimateInputCharacters(request);
        if (estimatedCharacters > TextPlanningExecutionPolicy.DefaultMaxInputCharacters)
        {
            throw new InvalidOperationException(
                $"Blueprint planning exceeds the bounded local-direct default of {TextPlanningExecutionPolicy.DefaultMaxInputCharacters} characters. Summarize or trim the brief locally before provider dispatch.");
        }
    }
}
