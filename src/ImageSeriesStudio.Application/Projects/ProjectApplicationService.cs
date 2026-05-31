using ImageSeriesStudio.Core.Generation;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;

namespace ImageSeriesStudio.Application.Projects;

public sealed class ProjectApplicationService
{
    private readonly IProjectRepository _repository;
    private readonly ITextPlanningProvider? _textPlanningProvider;
    private readonly IImageGenerationProvider? _imageGenerationProvider;
    private readonly IVisionReviewProvider? _visionReviewProvider;

    public ProjectApplicationService(IProjectRepository repository)
        : this(repository, textPlanningProvider: null, imageGenerationProvider: null, visionReviewProvider: null)
    {
    }

    public ProjectApplicationService(
        IProjectRepository repository,
        ITextPlanningProvider? textPlanningProvider)
        : this(repository, textPlanningProvider, imageGenerationProvider: null, visionReviewProvider: null)
    {
    }

    public ProjectApplicationService(
        IProjectRepository repository,
        ITextPlanningProvider? textPlanningProvider,
        IImageGenerationProvider? imageGenerationProvider)
        : this(repository, textPlanningProvider, imageGenerationProvider, visionReviewProvider: null)
    {
    }

    public ProjectApplicationService(
        IProjectRepository repository,
        ITextPlanningProvider? textPlanningProvider,
        IImageGenerationProvider? imageGenerationProvider,
        IVisionReviewProvider? visionReviewProvider)
    {
        _repository = repository;
        _textPlanningProvider = textPlanningProvider;
        _imageGenerationProvider = imageGenerationProvider;
        _visionReviewProvider = visionReviewProvider;
    }

    public async Task<ImageProject> CreateProjectAsync(
        string name,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var project = ImageProject.Create(name, timestamp);
        await _repository.SaveAsync(project, cancellationToken);
        return project;
    }

    public Task<ImageProject?> LoadProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return _repository.LoadAsync(projectId, cancellationToken);
    }

    public Task<IReadOnlyList<ProjectSummary>> ListProjectsAsync(CancellationToken cancellationToken)
    {
        return _repository.ListAsync(cancellationToken);
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
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var project = await RequireProjectAsync(projectId, cancellationToken);
        var series = project.Series.SingleOrDefault(series => series.Id == seriesId)
            ?? throw new InvalidOperationException($"Series not found: {seriesId}");
        var item = series.AddItem(title, brief, timestamp);
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

    public async Task<GenerationQueueRun> RunGenerationQueueAsync(
        Guid projectId,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        if (_imageGenerationProvider is null)
        {
            throw new InvalidOperationException("Image generation provider is not registered.");
        }

        if (!_imageGenerationProvider.Capabilities.ProviderId.StartsWith("fake", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Real image generation requires explicit approval.");
        }

        var project = await RequireProjectAsync(projectId, cancellationToken);
        var requests = CreateGenerationRequests(project, outputDirectory);
        var queue = new GenerationQueue(
            _imageGenerationProvider,
            new GenerationQueueOptions(MaxConcurrency: 1, MaxRetries: 0));

        return await queue.RunAsync(requests, cancellationToken);
    }

    public async Task<IReadOnlyList<VisionReviewResult>> RunVisionReviewAsync(
        Guid projectId,
        IReadOnlyList<ReviewCandidateInput> candidates,
        CancellationToken cancellationToken)
    {
        if (_visionReviewProvider is null)
        {
            throw new InvalidOperationException("Vision review provider is not registered.");
        }

        if (!_visionReviewProvider.Capabilities.ProviderId.StartsWith("fake", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Real vision review requires explicit approval.");
        }

        var rubric = new ReviewRubric(
            Guid.NewGuid(),
            projectId,
            "Default fake review",
            [new ReviewRubricDimension("match", "Candidate should match the prompt and item brief.", 1)],
            DateTimeOffset.UtcNow);

        var results = new List<VisionReviewResult>();
        foreach (var candidate in candidates)
        {
            var result = await _visionReviewProvider.ReviewAsync(
                new VisionReviewRequest(
                    candidate.CandidateImageId,
                    candidate.AssetPath,
                    rubric,
                    candidate.PromptText),
                cancellationToken);
            results.Add(result);
        }

        return results;
    }

    private static GenerationSettings CreateDefaultGenerationSettings()
    {
        return new GenerationSettings(1024, 1024, "standard", "png");
    }

    private static IReadOnlyList<ImageGenerationRequest> CreateGenerationRequests(
        ImageProject project,
        string outputDirectory)
    {
        var index = 0;
        return project.Series
            .SelectMany(series => series.Items)
            .Select(item => new
            {
                Item = item,
                Prompt = item.PromptVersions.OrderByDescending(prompt => prompt.VersionNumber).FirstOrDefault(),
            })
            .Where(value => value.Prompt is not null)
            .Select(value =>
            {
                index++;
                return new ImageGenerationRequest(
                    value.Item.Id,
                    value.Prompt!.Id,
                    value.Prompt.PromptText,
                    value.Prompt.Settings,
                    outputDirectory,
                    $"{index:000}-{SanitizeFileName(value.Item.Title)}.png");
            })
            .ToArray();
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(character => invalidChars.Contains(character) ? '-' : character).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "image" : sanitized.Trim();
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

public interface IProjectRepository
{
    Task SaveAsync(ImageProject project, CancellationToken cancellationToken);

    Task<ImageProject?> LoadAsync(Guid projectId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProjectSummary>> ListAsync(CancellationToken cancellationToken);
}

public sealed record ProjectSummary(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ReviewCandidateInput(
    Guid CandidateImageId,
    string ItemTitle,
    string AssetPath,
    string PromptText);
