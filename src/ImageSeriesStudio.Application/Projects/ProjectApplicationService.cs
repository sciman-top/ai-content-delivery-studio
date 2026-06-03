using ImageSeriesStudio.Core.Generation;
using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Core.Styles;
using ImageSeriesStudio.Application.Delivery;

namespace ImageSeriesStudio.Application.Projects;

public sealed class ProjectApplicationService
{
    private readonly IProjectRepository _repository;
    private readonly ITextPlanningProvider? _textPlanningProvider;
    private readonly IImageGenerationProvider? _imageGenerationProvider;
    private readonly IImageEditProvider? _imageEditProvider;
    private readonly IVisionReviewProvider? _visionReviewProvider;
    private readonly IDeliveryPackageWriter? _deliveryPackageWriter;

    public ProjectApplicationService(IProjectRepository repository)
        : this(repository, textPlanningProvider: null, imageGenerationProvider: null, visionReviewProvider: null, deliveryPackageWriter: null)
    {
    }

    public ProjectApplicationService(
        IProjectRepository repository,
        ITextPlanningProvider? textPlanningProvider)
        : this(repository, textPlanningProvider, imageGenerationProvider: null, visionReviewProvider: null, deliveryPackageWriter: null)
    {
    }

    public ProjectApplicationService(
        IProjectRepository repository,
        ITextPlanningProvider? textPlanningProvider,
        IImageGenerationProvider? imageGenerationProvider)
        : this(repository, textPlanningProvider, imageGenerationProvider, visionReviewProvider: null, deliveryPackageWriter: null)
    {
    }

    public ProjectApplicationService(
        IProjectRepository repository,
        ITextPlanningProvider? textPlanningProvider,
        IImageGenerationProvider? imageGenerationProvider,
        IVisionReviewProvider? visionReviewProvider)
        : this(repository, textPlanningProvider, imageGenerationProvider, visionReviewProvider, deliveryPackageWriter: null)
    {
    }

    public ProjectApplicationService(
        IProjectRepository repository,
        ITextPlanningProvider? textPlanningProvider,
        IImageGenerationProvider? imageGenerationProvider,
        IVisionReviewProvider? visionReviewProvider,
        IDeliveryPackageWriter? deliveryPackageWriter,
        IImageEditProvider? imageEditProvider = null)
    {
        _repository = repository;
        _textPlanningProvider = textPlanningProvider;
        _imageGenerationProvider = imageGenerationProvider;
        _imageEditProvider = imageEditProvider;
        _visionReviewProvider = visionReviewProvider;
        _deliveryPackageWriter = deliveryPackageWriter;
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
        return await AddItemAsync(
            projectId,
            seriesId,
            title,
            brief,
            SeriesItemKind.Standard,
            timestamp,
            cancellationToken);
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

        var result = await _textPlanningProvider.CreatePromptDirectionsAsync(
            new BriefPlanningRequest(
                brief.Goal,
                brief.Audience,
                brief.StyleIntent,
                brief.MustInclude,
                brief.MustAvoid,
                DirectionCount: 3),
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

        var result = await _textPlanningProvider.CreateDesignBlueprintsAsync(
            new BlueprintPlanningRequest(
                brief.Goal,
                brief.Audience,
                brief.StyleIntent,
                brief.MustInclude,
                brief.MustAvoid,
                brief.TextPolicy,
                CandidateCount: 3),
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

    public async Task<DocumentIllustrationWorkflowResult> CreateDocumentIllustrationPlanWithProviderAsync(
        Guid projectId,
        DocumentIllustrationPlanningRequest request,
        bool approveAllTargets,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        if (_textPlanningProvider is null)
        {
            throw new InvalidOperationException("Text planning provider is not registered.");
        }

        if (!_textPlanningProvider.Capabilities.ProviderId.StartsWith("fake", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Real document illustration planning requires explicit approval.");
        }

        if (request.DocumentFamily is DocumentFamily.ScholarlyDraft
            && request.KnownConstraints.Any(value => value.Contains("fake evidence", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Scholarly draft planning cannot request fake evidence imagery.");
        }

        var project = await RequireProjectAsync(projectId, cancellationToken);
        var providerResult = await _textPlanningProvider.CreateDocumentIllustrationPlanAsync(request, cancellationToken);
        var brief = RebindDocumentBrief(project.Id, providerResult.Brief, timestamp);
        var plan = RebindIllustrationPlan(project.Id, brief.Id, providerResult.Plan, timestamp);

        if (approveAllTargets)
        {
            foreach (var target in plan.Targets)
            {
                plan = plan.ApproveTarget(target.Id, timestamp);
            }
        }

        project.AddDocumentBrief(brief, timestamp);
        project.AddIllustrationPlan(plan, timestamp);

        var approvedTargets = plan.ApprovedTargets;
        ImageSeries? series = null;
        if (approvedTargets.Count > 0)
        {
            var providerProfile = ResolveProviderProfile(project, providerProfileId: null, timestamp);
            series = project.AddSeries(
                $"Document illustrations: {brief.Title}",
                plan.Summary,
                timestamp);

            foreach (var target in approvedTargets)
            {
                var item = series.AddItem(
                    target.Title,
                    BuildDocumentTargetBrief(target),
                    timestamp);
                item.AddPromptVersion(
                    BuildDocumentTargetPrompt(target),
                    CreateDefaultGenerationSettings(),
                    providerProfile.Id,
                    timestamp);
            }
        }

        await _repository.SaveAsync(project, cancellationToken);
        return new DocumentIllustrationWorkflowResult(
            brief.Id,
            plan.Id,
            series?.Id,
            approvedTargets.Count);
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

    public async Task<ImageGenerationResult> RunImageEditAsync(
        ImageEditWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        if (_imageEditProvider is null)
        {
            throw new InvalidOperationException("Image edit provider is not registered.");
        }

        if (!_imageEditProvider.Capabilities.ProviderId.StartsWith("fake", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Real image editing requires explicit approval.");
        }

        if (!_imageEditProvider.Capabilities.SupportsImageEditing)
        {
            throw new InvalidOperationException("Provider does not support image editing.");
        }

        _ = await RequireProjectAsync(request.ProjectId, cancellationToken);

        return await _imageEditProvider.EditImageAsync(
            new ImageEditRequest(
                request.SeriesItemId,
                request.SourceCandidateImageId,
                request.SourceImagePath,
                request.MaskImagePath,
                request.PromptText,
                request.Settings,
                request.OutputDirectory,
                request.OutputFileName,
                request.Recipe),
            cancellationToken);
    }

    public async Task<IReadOnlyList<VisionReviewResult>> RunVisionReviewAsync(
        Guid projectId,
        IReadOnlyList<ReviewCandidateInput> candidates,
        CancellationToken cancellationToken)
    {
        var reviews = await RunStructuredVisionReviewAsync(projectId, candidates, cancellationToken);

        return reviews
            .Select(review => new VisionReviewResult(
                review.CandidateImageId,
                review.Decision,
                review.Scores.ToDictionary(score => score.Name, score => score.Score),
                review.HardFailures,
                review.Comments,
                review.SuggestedFix))
            .ToArray();
    }

    public async Task<IReadOnlyList<StructuredReviewOutput>> RunStructuredVisionReviewAsync(
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

        var rubric = ReviewRubricTemplateCatalog
            .GetById(ReviewRubricTemplateCatalog.GeneralImage)
            .CreateRubric(projectId, DateTimeOffset.UtcNow);

        var results = new List<StructuredReviewOutput>();
        foreach (var candidate in candidates)
        {
            var result = await _visionReviewProvider.ReviewAsync(
                new VisionReviewRequest(
                    candidate.CandidateImageId,
                    candidate.AssetPath,
                    rubric,
                    candidate.PromptText),
                cancellationToken);
            results.Add(StructuredReviewOutput.FromProviderResult(result, rubric));
        }

        return results;
    }

    public async Task<DeliveryExportResult> ExportDeliveryPackageAsync(
        DeliveryExportRequest request,
        CancellationToken cancellationToken)
    {
        if (_deliveryPackageWriter is null)
        {
            throw new InvalidOperationException("Delivery package writer is not registered.");
        }

        return await _deliveryPackageWriter.WriteAsync(request, cancellationToken);
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

    private static DocumentBrief RebindDocumentBrief(
        Guid projectId,
        DocumentBrief source,
        DateTimeOffset timestamp)
    {
        return DocumentBrief.Create(
            projectId,
            source.SourceKind,
            source.SourceDisplayName,
            source.Title,
            source.DocumentFamily,
            source.Audience,
            source.Sections,
            source.KeyClaims,
            source.VisualOpportunities,
            source.KnownConstraints,
            source.StrictnessLevel,
            timestamp);
    }

    private static IllustrationPlan RebindIllustrationPlan(
        Guid projectId,
        Guid documentBriefId,
        IllustrationPlan source,
        DateTimeOffset timestamp)
    {
        var targets = source.Targets
            .Select(target => IllustrationTarget.Create(
                documentBriefId,
                target.Title,
                target.DocumentLocation,
                target.Purpose,
                target.MustShow,
                target.MustNotShow,
                target.SourceEvidence,
                target.SuggestedImageTypePresetId,
                target.SuggestedReviewRubricTemplateId,
                target.TextPolicy,
                target.StrictnessNotes,
                timestamp))
            .ToArray();

        return IllustrationPlan.Create(
            projectId,
            documentBriefId,
            source.Summary,
            targets,
            source.CoverageNotes,
            source.RiskNotes,
            timestamp);
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

    private static string BuildDocumentTargetBrief(IllustrationTarget target)
    {
        return string.Join(
            Environment.NewLine,
            [
                $"Purpose: {target.Purpose}",
                $"Location: {target.DocumentLocation}",
                "Must show:",
                FormatList(target.MustShow),
                "Must not show:",
                FormatList(target.MustNotShow),
                "Source evidence:",
                FormatList(target.SourceEvidence),
                "Strictness:",
                FormatList(target.StrictnessNotes),
                $"Text policy: {target.TextPolicy}",
            ]);
    }

    private static string BuildDocumentTargetPrompt(IllustrationTarget target)
    {
        return string.Join(
            Environment.NewLine,
            [
                $"Create a document illustration titled \"{target.Title}\" for {target.DocumentLocation}.",
                $"Purpose: {target.Purpose}.",
                "Must show:",
                FormatList(target.MustShow),
                "Must not show:",
                FormatList(target.MustNotShow),
                "Source evidence:",
                FormatList(target.SourceEvidence),
                $"Text policy: {target.TextPolicy}. Use deterministic post-render text when required for labels, captions, or callouts.",
                "Do not imply real experimental, clinical, archival, or field evidence unless the user provided that evidence explicitly.",
            ]);
    }

    private static string FormatList(IReadOnlyList<string> values)
    {
        return values.Count == 0
            ? "- None specified."
            : string.Join(Environment.NewLine, values.Select(value => $"- {value}"));
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

public sealed record DocumentIllustrationWorkflowResult(
    Guid DocumentBriefId,
    Guid IllustrationPlanId,
    Guid? SeriesId,
    int ApprovedTargetCount);

public sealed record ImageEditWorkflowRequest(
    Guid ProjectId,
    Guid SeriesItemId,
    Guid SourceCandidateImageId,
    string SourceImagePath,
    string? MaskImagePath,
    string PromptText,
    GenerationSettings Settings,
    string OutputDirectory,
    string OutputFileName = "",
    GenerationRecipe? Recipe = null);

public sealed record ReviewCandidateInput(
    Guid CandidateImageId,
    string ItemTitle,
    string AssetPath,
    string PromptText);
