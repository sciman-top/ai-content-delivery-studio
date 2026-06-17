using ContentDeliveryStudio.Core.Generation;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Core.Styles;
using ContentDeliveryStudio.Application.Delivery;
using ContentDeliveryStudio.Application.RepairRouting;
using ContentDeliveryStudio.Application.Sources;

namespace ContentDeliveryStudio.Application.Projects;

public sealed class ProjectApplicationService
{
    private readonly ReviewRepairApplicationService _reviewRepairApplicationService;
    private readonly ProjectWorkspaceApplicationService _projectWorkspaceApplicationService;
    private readonly DeliveryApplicationService _deliveryApplicationService;
    private readonly DocumentIllustrationApplicationService _documentIllustrationApplicationService;
    private readonly SeriesWorkflowApplicationService _seriesWorkflowApplicationService;
    private readonly BriefWorkflowApplicationService _briefWorkflowApplicationService;
    private readonly GenerationWorkflowApplicationService _generationWorkflowApplicationService;
    private readonly ReviewWorkflowApplicationService _reviewWorkflowApplicationService;
    private readonly SourceIngestionWorkflowApplicationService _sourceIngestionWorkflowApplicationService;

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
        IImageEditProvider? imageEditProvider = null,
        SourceIngestionApplicationService? sourceIngestionApplicationService = null)
    {
        _projectWorkspaceApplicationService = new ProjectWorkspaceApplicationService(repository);
        _reviewRepairApplicationService = new ReviewRepairApplicationService(repository);
        _deliveryApplicationService = new DeliveryApplicationService(deliveryPackageWriter);
        _documentIllustrationApplicationService = new DocumentIllustrationApplicationService(repository, textPlanningProvider);
        _seriesWorkflowApplicationService = new SeriesWorkflowApplicationService(repository, textPlanningProvider);
        _briefWorkflowApplicationService = new BriefWorkflowApplicationService(repository, textPlanningProvider);
        _generationWorkflowApplicationService = new GenerationWorkflowApplicationService(repository, imageGenerationProvider, imageEditProvider);
        _reviewWorkflowApplicationService = new ReviewWorkflowApplicationService(repository, visionReviewProvider);
        _sourceIngestionWorkflowApplicationService = new SourceIngestionWorkflowApplicationService(sourceIngestionApplicationService);
    }

    public async Task<ImageProject> CreateProjectAsync(
        string name,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        return await _projectWorkspaceApplicationService.CreateProjectAsync(
            name,
            timestamp,
            cancellationToken);
    }

    public Task<ImageProject?> LoadProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return _projectWorkspaceApplicationService.LoadProjectAsync(projectId, cancellationToken);
    }

    public Task<IReadOnlyList<ProjectSummary>> ListProjectsAsync(CancellationToken cancellationToken)
    {
        return _projectWorkspaceApplicationService.ListProjectsAsync(cancellationToken);
    }

    public async Task<ImageSeries> AddSeriesAsync(
        Guid projectId,
        string title,
        string description,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        return await _seriesWorkflowApplicationService.AddSeriesAsync(
            projectId,
            title,
            description,
            timestamp,
            cancellationToken);
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
        return await _seriesWorkflowApplicationService.AddItemAsync(
            projectId,
            seriesId,
            title,
            brief,
            kind,
            timestamp,
            cancellationToken);
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
        return await _seriesWorkflowApplicationService.AddPromptVersionAsync(
            projectId,
            seriesItemId,
            promptText,
            settings,
            providerProfileId,
            timestamp,
            cancellationToken);
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
        return await _briefWorkflowApplicationService.CreateCreativeBriefAsync(
            projectId,
            seriesId,
            goal,
            audience,
            textPolicy,
            styleIntent,
            mustInclude,
            mustAvoid,
            timestamp,
            cancellationToken);
    }

    public async Task<CreativeBrief> CreatePromptDirectionsAsync(
        Guid projectId,
        Guid creativeBriefId,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        return await _briefWorkflowApplicationService.CreatePromptDirectionsAsync(
            projectId,
            creativeBriefId,
            timestamp,
            cancellationToken);
    }

    public async Task<CreativeBrief> CreateDesignBlueprintsAsync(
        Guid projectId,
        Guid creativeBriefId,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        return await _briefWorkflowApplicationService.CreateDesignBlueprintsAsync(
            projectId,
            creativeBriefId,
            timestamp,
            cancellationToken);
    }

    public async Task<DesignBlueprint> PromoteDesignBlueprintAsync(
        Guid projectId,
        Guid creativeBriefId,
        Guid blueprintId,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        return await _briefWorkflowApplicationService.PromoteDesignBlueprintAsync(
            projectId,
            creativeBriefId,
            blueprintId,
            timestamp,
            cancellationToken);
    }

    public async Task<PromptVersion> PromotePromptDirectionAsync(
        Guid projectId,
        Guid seriesItemId,
        Guid creativeBriefId,
        string directionKey,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        return await _briefWorkflowApplicationService.PromotePromptDirectionAsync(
            projectId,
            seriesItemId,
            creativeBriefId,
            directionKey,
            timestamp,
            cancellationToken);
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
        return await _briefWorkflowApplicationService.PromotePromptDirectionAsync(
            projectId,
            seriesItemId,
            creativeBriefId,
            directionKey,
            settings,
            timestamp,
            cancellationToken);
    }

    public async Task<ImageSeries> CreatePlanWithProviderAsync(
        Guid projectId,
        PlanningRequest request,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        return await _seriesWorkflowApplicationService.CreatePlanWithProviderAsync(
            projectId,
            request,
            timestamp,
            cancellationToken);
    }

    public async Task<DocumentIllustrationWorkflowResult> CreateDocumentIllustrationPlanWithProviderAsync(
        Guid projectId,
        DocumentIllustrationPlanningRequest request,
        bool approveAllTargets,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        return await _documentIllustrationApplicationService.CreateDocumentIllustrationPlanWithProviderAsync(
            projectId,
            request,
            approveAllTargets,
            timestamp,
            cancellationToken);
    }

    public async Task<SourceIngestionWorkflowResult> IngestSourceAsync(
        Guid projectId,
        SourceIngestionRequest request,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        return await _sourceIngestionWorkflowApplicationService.IngestSourceAsync(
            projectId,
            request,
            timestamp,
            cancellationToken);
    }

    public async Task<GenerationQueueRun> RunGenerationQueueAsync(
        Guid projectId,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        return await _generationWorkflowApplicationService.RunGenerationQueueAsync(
            projectId,
            outputDirectory,
            cancellationToken);
    }

    public async Task<ImageGenerationResult> RunImageEditAsync(
        ImageEditWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        return await _generationWorkflowApplicationService.RunImageEditAsync(
            request,
            cancellationToken);
    }

    public async Task<IReadOnlyList<VisionReviewResult>> RunVisionReviewAsync(
        Guid projectId,
        IReadOnlyList<ReviewCandidateInput> candidates,
        CancellationToken cancellationToken)
    {
        return await _reviewWorkflowApplicationService.RunVisionReviewAsync(
            projectId,
            candidates,
            cancellationToken);
    }

    public async Task<IReadOnlyList<StructuredReviewOutput>> RunStructuredVisionReviewAsync(
        Guid projectId,
        IReadOnlyList<ReviewCandidateInput> candidates,
        CancellationToken cancellationToken)
    {
        return await _reviewWorkflowApplicationService.RunStructuredVisionReviewAsync(
            projectId,
            candidates,
            cancellationToken);
    }

    public IReadOnlyList<ReviewOutcomeRoutingPlan> RouteReviewOutcomes(
        IReadOnlyList<StructuredReviewOutput> reviews)
    {
        return _reviewRepairApplicationService.RouteReviewOutcomes(reviews);
    }

    public async Task<RoutedRepairApplicationResult> ApplyRoutedRepairAsync(
        RoutedRepairApplicationRequest request,
        CancellationToken cancellationToken)
    {
        return await _reviewRepairApplicationService.ApplyRoutedRepairAsync(request, cancellationToken);
    }

    public async Task<RoutedRepairPatch> CreateRoutedRepairPatchAsync(
        RoutedRepairPatchRequest request,
        CancellationToken cancellationToken)
    {
        return await _reviewRepairApplicationService.CreateRoutedRepairPatchAsync(request, cancellationToken);
    }

    public async Task<RoutedRepairPatchApplicationResult> ApplyRoutedRepairPatchAsync(
        RoutedRepairPatchApplicationRequest request,
        CancellationToken cancellationToken)
    {
        return await _reviewRepairApplicationService.ApplyRoutedRepairPatchAsync(request, cancellationToken);
    }

    public async Task<DeliveryExportResult> ExportDeliveryPackageAsync(
        DeliveryExportRequest request,
        CancellationToken cancellationToken)
    {
        return await _deliveryApplicationService.ExportDeliveryPackageAsync(request, cancellationToken);
    }

    public async Task<FinalApprovalDecision> RecordFinalApprovalAsync(
        Guid projectId,
        FinalApprovalRequest request,
        DateTimeOffset decidedAt,
        CancellationToken cancellationToken)
    {
        return await _reviewWorkflowApplicationService.RecordFinalApprovalAsync(
            projectId,
            request,
            decidedAt,
            cancellationToken);
    }
}

public interface IProjectRepository
{
    Task SaveAsync(ImageProject project, CancellationToken cancellationToken);

    Task<ImageProject?> LoadAsync(Guid projectId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProjectSummary>> ListAsync(CancellationToken cancellationToken);

    Task SaveReviewResultAsync(Guid projectId, ReviewResult reviewResult, CancellationToken cancellationToken);

    Task<ReviewResult?> LoadLatestReviewResultAsync(Guid candidateImageId, CancellationToken cancellationToken);
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
    string PromptText,
    ReviewPrepArtifactContract? ReviewPrep = null);
