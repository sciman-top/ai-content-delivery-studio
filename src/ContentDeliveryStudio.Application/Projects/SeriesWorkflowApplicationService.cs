using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Providers;

namespace ContentDeliveryStudio.Application.Projects;

public sealed class SeriesWorkflowApplicationService
{
    private readonly IProjectRepository _repository;
    private readonly ITextPlanningProvider? _textPlanningProvider;

    public SeriesWorkflowApplicationService(
        IProjectRepository repository,
        ITextPlanningProvider? textPlanningProvider)
    {
        _repository = repository;
        _textPlanningProvider = textPlanningProvider;
    }

    public async Task<ImageSeries> AddSeriesAsync(
        Guid projectId,
        string title,
        string description,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var project = await RequireProjectAsync(projectId, cancellationToken);
        var series = project.AddSeries(title, description, timestamp);
        await _repository.SaveAsync(project, cancellationToken);
        return series;
    }

    public async Task<SeriesItem> AddItemAsync(
        Guid projectId,
        Guid seriesId,
        string title,
        string brief,
        SeriesItemKind kind,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var project = await RequireProjectAsync(projectId, cancellationToken);
        var series = project.Series.SingleOrDefault(series => series.Id == seriesId)
            ?? throw new InvalidOperationException($"Series not found: {seriesId}");
        var item = series.AddItem(title, brief, kind, timestamp);
        await _repository.SaveAsync(project, cancellationToken);
        return item;
    }

    public async Task<PromptVersion> AddPromptVersionAsync(
        Guid projectId,
        Guid seriesItemId,
        string promptText,
        GenerationSettings settings,
        Guid? providerProfileId,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var project = await RequireProjectAsync(projectId, cancellationToken);
        var item = project.Series
            .SelectMany(series => series.Items)
            .SingleOrDefault(item => item.Id == seriesItemId)
            ?? throw new InvalidOperationException($"Series item not found: {seriesItemId}");
        var providerProfile = ResolveProviderProfile(project, providerProfileId, timestamp);
        var prompt = item.AddPromptVersion(promptText, settings, providerProfile.Id, timestamp);

        await _repository.SaveAsync(project, cancellationToken);
        return prompt;
    }

    public async Task<ImageSeries> CreatePlanWithProviderAsync(
        Guid projectId,
        PlanningRequest request,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        ValidatePlanningRequest(request);

        if (_textPlanningProvider is null)
        {
            throw new InvalidOperationException("Text planning provider is not registered.");
        }

        var project = await RequireProjectAsync(projectId, cancellationToken);
        var plan = await _textPlanningProvider.CreatePlanAsync(request, cancellationToken);
        var providerProfile = ResolveProviderProfile(project, providerProfileId: null, timestamp);
        var seriesTitle = string.IsNullOrWhiteSpace(request.Goal) ? plan.Summary : request.Goal.Trim();
        var series = project.AddSeries(seriesTitle, plan.Summary, timestamp);

        foreach (var plannedItem in plan.Items)
        {
            var item = series.AddItem(plannedItem.Title, plannedItem.Brief, timestamp);
            item.AddPromptVersion(
                plannedItem.PromptDraft,
                CreateDefaultGenerationSettings(),
                providerProfile.Id,
                timestamp);
        }

        await _repository.SaveAsync(project, cancellationToken);
        return series;
    }

    private static void ValidatePlanningRequest(PlanningRequest request)
    {
        var descriptor = TextPlanningExecutionPolicy.CreateOperatorDescriptor();
        var estimatedCharacters = TextPlanningExecutionPolicy.EstimateInputCharacters(request);
        if (estimatedCharacters > descriptor.MaxInputCharacters)
        {
            throw new InvalidOperationException(
                $"Text planning request exceeds the bounded local-direct default of {descriptor.MaxInputCharacters} characters. Split or summarize the request locally before provider dispatch.");
        }
    }

    private static GenerationSettings CreateDefaultGenerationSettings()
    {
        return new GenerationSettings(1024, 1024, "standard", "png");
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
}
