using ImageSeriesStudio.Core.Documents;

namespace ImageSeriesStudio.Core.Projects;

public sealed class ImageProject
{
    private readonly List<DocumentBrief> _documentBriefs = [];
    private readonly List<IllustrationPlan> _illustrationPlans = [];
    private readonly List<ImageSeries> _series = [];
    private readonly List<ProviderProfile> _providerProfiles = [];

    private ImageProject()
    {
        Name = string.Empty;
    }

    private ImageProject(Guid id, string name, DateTimeOffset createdAt)
    {
        Id = id;
        Name = RequireText(name, nameof(name));
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<ImageSeries> Series => _series.AsReadOnly();

    public IReadOnlyCollection<ProviderProfile> ProviderProfiles => _providerProfiles.AsReadOnly();

    public IReadOnlyCollection<DocumentBrief> DocumentBriefs => _documentBriefs.AsReadOnly();

    public IReadOnlyCollection<IllustrationPlan> IllustrationPlans => _illustrationPlans.AsReadOnly();

    public static ImageProject Create(string name, DateTimeOffset createdAt)
    {
        return new ImageProject(Guid.NewGuid(), name, createdAt);
    }

    public ImageSeries AddSeries(string title, string description, DateTimeOffset timestamp)
    {
        var series = ImageSeries.Create(Id, title, description, timestamp);
        _series.Add(series);
        UpdatedAt = timestamp;
        return series;
    }

    public ProviderProfile AddProviderProfile(string displayName, ProviderKind kind, DateTimeOffset timestamp)
    {
        var profile = new ProviderProfile(Guid.NewGuid(), Id, RequireText(displayName, nameof(displayName)), kind, timestamp);
        _providerProfiles.Add(profile);
        UpdatedAt = timestamp;
        return profile;
    }

    public DocumentBrief AddDocumentBrief(DocumentBrief brief, DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(brief);

        if (brief.ProjectId != Id)
        {
            throw new ArgumentException("Document brief must belong to this project.", nameof(brief));
        }

        if (_documentBriefs.Any(existing => existing.Id == brief.Id))
        {
            throw new InvalidOperationException($"Document brief already exists: {brief.Id}");
        }

        _documentBriefs.Add(brief);
        UpdatedAt = timestamp;
        return brief;
    }

    public IllustrationPlan AddIllustrationPlan(IllustrationPlan plan, DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (plan.ProjectId != Id)
        {
            throw new ArgumentException("Illustration plan must belong to this project.", nameof(plan));
        }

        if (!_documentBriefs.Any(brief => brief.Id == plan.DocumentBriefId))
        {
            throw new InvalidOperationException($"Document brief not found for illustration plan: {plan.DocumentBriefId}");
        }

        if (plan.Targets.Any(target => target.DocumentBriefId != plan.DocumentBriefId))
        {
            throw new InvalidOperationException("Illustration plan targets must reference the plan document brief.");
        }

        if (_illustrationPlans.Any(existing => existing.Id == plan.Id))
        {
            throw new InvalidOperationException($"Illustration plan already exists: {plan.Id}");
        }

        _illustrationPlans.Add(plan);
        UpdatedAt = timestamp;
        return plan;
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }
}

public sealed class ImageSeries
{
    private readonly List<SeriesItem> _items = [];

    private ImageSeries()
    {
        Title = string.Empty;
        Description = string.Empty;
    }

    private ImageSeries(Guid id, Guid projectId, string title, string description, DateTimeOffset createdAt)
    {
        Id = id;
        ProjectId = projectId;
        Title = RequireText(title, nameof(title));
        Description = description.Trim();
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<SeriesItem> Items => _items.AsReadOnly();

    public static ImageSeries Create(Guid projectId, string title, string description, DateTimeOffset createdAt)
    {
        return new ImageSeries(Guid.NewGuid(), projectId, title, description, createdAt);
    }

    public SeriesItem AddItem(string title, string brief, DateTimeOffset timestamp)
    {
        var item = SeriesItem.Create(Id, title, brief, timestamp);
        _items.Add(item);
        UpdatedAt = timestamp;
        return item;
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }
}

public sealed class SeriesItem
{
    private readonly List<PromptVersion> _promptVersions = [];
    private readonly List<GenerationTask> _generationTasks = [];
    private readonly List<CandidateImage> _candidateImages = [];

    private SeriesItem()
    {
        Title = string.Empty;
        Brief = string.Empty;
    }

    private SeriesItem(Guid id, Guid? seriesId, string title, string brief, DateTimeOffset createdAt)
    {
        Id = id;
        SeriesId = seriesId;
        Title = RequireText(title, nameof(title));
        Brief = brief.Trim();
        Status = SeriesItemStatus.Draft;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid? SeriesId { get; private set; }

    public string Title { get; private set; }

    public string Brief { get; private set; }

    public SeriesItemStatus Status { get; private set; }

    public string? RevisionReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<PromptVersion> PromptVersions => _promptVersions.AsReadOnly();

    public IReadOnlyCollection<GenerationTask> GenerationTasks => _generationTasks.AsReadOnly();

    public IReadOnlyCollection<CandidateImage> CandidateImages => _candidateImages.AsReadOnly();

    public static SeriesItem Create(string title, string brief, DateTimeOffset createdAt)
    {
        return new SeriesItem(Guid.NewGuid(), null, title, brief, createdAt);
    }

    public static SeriesItem Create(Guid seriesId, string title, string brief, DateTimeOffset createdAt)
    {
        return new SeriesItem(Guid.NewGuid(), seriesId, title, brief, createdAt);
    }

    public void MarkReady(DateTimeOffset timestamp)
    {
        TransitionTo(SeriesItemStatus.Ready, timestamp);
    }

    public void MarkGenerating(DateTimeOffset timestamp)
    {
        TransitionTo(SeriesItemStatus.Generating, timestamp);
    }

    public void MarkNeedsReview(DateTimeOffset timestamp)
    {
        TransitionTo(SeriesItemStatus.NeedsReview, timestamp);
    }

    public void Approve(DateTimeOffset timestamp)
    {
        TransitionTo(SeriesItemStatus.Approved, timestamp);
    }

    public void MarkDelivered(DateTimeOffset timestamp)
    {
        TransitionTo(SeriesItemStatus.Delivered, timestamp);
    }

    public void ReopenForRevision(string reason, DateTimeOffset timestamp)
    {
        if (Status is SeriesItemStatus.Delivered)
        {
            throw new InvalidSeriesItemStateTransitionException(Status, SeriesItemStatus.Draft);
        }

        RevisionReason = RequireText(reason, nameof(reason));
        Status = SeriesItemStatus.Draft;
        UpdatedAt = timestamp;
    }

    public PromptVersion AddPromptVersion(
        string promptText,
        GenerationSettings settings,
        Guid providerProfileId,
        DateTimeOffset timestamp)
    {
        var promptVersion = new PromptVersion(
            Guid.NewGuid(),
            Id,
            _promptVersions.Count + 1,
            RequireText(promptText, nameof(promptText)),
            settings,
            providerProfileId,
            timestamp);

        _promptVersions.Add(promptVersion);
        UpdatedAt = timestamp;
        return promptVersion;
    }

    private void TransitionTo(SeriesItemStatus nextStatus, DateTimeOffset timestamp)
    {
        if (!CanTransition(Status, nextStatus))
        {
            throw new InvalidSeriesItemStateTransitionException(Status, nextStatus);
        }

        Status = nextStatus;
        UpdatedAt = timestamp;
    }

    private static bool CanTransition(SeriesItemStatus currentStatus, SeriesItemStatus nextStatus)
    {
        return currentStatus switch
        {
            SeriesItemStatus.Draft => nextStatus is SeriesItemStatus.Ready,
            SeriesItemStatus.Ready => nextStatus is SeriesItemStatus.Generating,
            SeriesItemStatus.Generating => nextStatus is SeriesItemStatus.NeedsReview,
            SeriesItemStatus.NeedsReview => nextStatus is SeriesItemStatus.Approved,
            SeriesItemStatus.Approved => nextStatus is SeriesItemStatus.Delivered,
            SeriesItemStatus.Delivered => false,
            _ => false,
        };
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }
}

public sealed class InvalidSeriesItemStateTransitionException : InvalidOperationException
{
    public InvalidSeriesItemStateTransitionException(SeriesItemStatus currentStatus, SeriesItemStatus nextStatus)
        : base($"Invalid series item transition: {currentStatus} -> {nextStatus}.")
    {
        CurrentStatus = currentStatus;
        NextStatus = nextStatus;
    }

    public SeriesItemStatus CurrentStatus { get; }

    public SeriesItemStatus NextStatus { get; }
}

public enum SeriesItemStatus
{
    Draft = 0,
    Ready = 1,
    Generating = 2,
    NeedsReview = 3,
    Approved = 4,
    Delivered = 5,
}

public sealed class PromptVersion
{
    private PromptVersion()
    {
        PromptText = string.Empty;
        Settings = new GenerationSettings(0, 0, string.Empty, string.Empty);
    }

    public PromptVersion(
        Guid id,
        Guid seriesItemId,
        int versionNumber,
        string promptText,
        GenerationSettings settings,
        Guid providerProfileId,
        DateTimeOffset createdAt)
    {
        Id = id;
        SeriesItemId = seriesItemId;
        VersionNumber = versionNumber;
        PromptText = promptText;
        Settings = settings;
        ProviderProfileId = providerProfileId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid SeriesItemId { get; private set; }

    public int VersionNumber { get; private set; }

    public string PromptText { get; private set; }

    public GenerationSettings Settings { get; private set; }

    public Guid ProviderProfileId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}

public sealed class GenerationSettings
{
    private GenerationSettings()
    {
        Quality = string.Empty;
        OutputFormat = string.Empty;
    }

    public GenerationSettings(int width, int height, string quality, string outputFormat, int? seed = null)
    {
        Width = width;
        Height = height;
        Quality = quality;
        OutputFormat = outputFormat;
        Seed = seed;
    }

    public int Width { get; private set; }

    public int Height { get; private set; }

    public string Quality { get; private set; }

    public string OutputFormat { get; private set; }

    public int? Seed { get; private set; }
}

public sealed class GenerationTask
{
    private GenerationTask()
    {
    }

    public GenerationTask(
        Guid id,
        Guid seriesItemId,
        Guid promptVersionId,
        Guid providerProfileId,
        GenerationTaskStatus status,
        int attemptCount,
        int maxRetries,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        SeriesItemId = seriesItemId;
        PromptVersionId = promptVersionId;
        ProviderProfileId = providerProfileId;
        Status = status;
        AttemptCount = attemptCount;
        MaxRetries = maxRetries;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public Guid SeriesItemId { get; private set; }

    public Guid PromptVersionId { get; private set; }

    public Guid ProviderProfileId { get; private set; }

    public GenerationTaskStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public int MaxRetries { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }
}

public enum GenerationTaskStatus
{
    Queued = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    Cancelled = 4,
}

public sealed class CandidateImage
{
    private CandidateImage()
    {
        AssetPath = string.Empty;
        MetadataPath = string.Empty;
    }

    public CandidateImage(
        Guid id,
        Guid seriesItemId,
        Guid promptVersionId,
        Guid generationTaskId,
        Guid providerProfileId,
        CandidateImageStatus status,
        string assetPath,
        string metadataPath,
        DateTimeOffset createdAt)
    {
        Id = id;
        SeriesItemId = seriesItemId;
        PromptVersionId = promptVersionId;
        GenerationTaskId = generationTaskId;
        ProviderProfileId = providerProfileId;
        Status = status;
        AssetPath = assetPath;
        MetadataPath = metadataPath;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid SeriesItemId { get; private set; }

    public Guid PromptVersionId { get; private set; }

    public Guid GenerationTaskId { get; private set; }

    public Guid ProviderProfileId { get; private set; }

    public CandidateImageStatus Status { get; private set; }

    public string AssetPath { get; private set; }

    public string MetadataPath { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}

public enum CandidateImageStatus
{
    Generated = 0,
    ReviewPending = 1,
    Rejected = 2,
    Alternate = 3,
    Final = 4,
}

public sealed class ReviewRubric
{
    private ReviewRubric()
    {
        Name = string.Empty;
        Dimensions = [];
    }

    public ReviewRubric(
        Guid id,
        Guid projectId,
        string name,
        IReadOnlyList<ReviewRubricDimension> dimensions,
        DateTimeOffset createdAt)
    {
        Id = id;
        ProjectId = projectId;
        Name = name;
        Dimensions = dimensions;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public string Name { get; private set; }

    public IReadOnlyList<ReviewRubricDimension> Dimensions { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}

public sealed class ReviewRubricDimension
{
    private ReviewRubricDimension()
    {
        Name = string.Empty;
        Requirement = string.Empty;
    }

    public ReviewRubricDimension(string name, string requirement, int weight)
    {
        Name = name;
        Requirement = requirement;
        Weight = weight;
    }

    public string Name { get; private set; }

    public string Requirement { get; private set; }

    public int Weight { get; private set; }
}

public sealed class ReviewResult
{
    private ReviewResult()
    {
        Scores = new Dictionary<string, int>();
        HardFailures = [];
        Comments = string.Empty;
    }

    public ReviewResult(
        Guid id,
        Guid candidateImageId,
        ReviewDecision decision,
        IReadOnlyDictionary<string, int> scores,
        IReadOnlyList<string> hardFailures,
        string comments,
        string? suggestedFix,
        bool humanApproved,
        DateTimeOffset createdAt)
    {
        Id = id;
        CandidateImageId = candidateImageId;
        Decision = decision;
        Scores = scores;
        HardFailures = hardFailures;
        Comments = comments;
        SuggestedFix = suggestedFix;
        HumanApproved = humanApproved;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid CandidateImageId { get; private set; }

    public ReviewDecision Decision { get; private set; }

    public IReadOnlyDictionary<string, int> Scores { get; private set; }

    public IReadOnlyList<string> HardFailures { get; private set; }

    public string Comments { get; private set; }

    public string? SuggestedFix { get; private set; }

    public bool HumanApproved { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}

public enum ReviewDecision
{
    Pending = 0,
    Pass = 1,
    Fail = 2,
}

public sealed class DeliveryPackage
{
    private DeliveryPackage()
    {
        OutputPath = string.Empty;
        ManifestJsonPath = string.Empty;
        ManifestCsvPath = string.Empty;
    }

    public DeliveryPackage(
        Guid id,
        Guid projectId,
        int version,
        string outputPath,
        string manifestJsonPath,
        string manifestCsvPath,
        DateTimeOffset createdAt)
    {
        Id = id;
        ProjectId = projectId;
        Version = version;
        OutputPath = outputPath;
        ManifestJsonPath = manifestJsonPath;
        ManifestCsvPath = manifestCsvPath;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public int Version { get; private set; }

    public string OutputPath { get; private set; }

    public string ManifestJsonPath { get; private set; }

    public string ManifestCsvPath { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}

public sealed class ProviderProfile
{
    private ProviderProfile()
    {
        DisplayName = string.Empty;
    }

    public ProviderProfile(Guid id, Guid projectId, string displayName, ProviderKind kind, DateTimeOffset createdAt)
    {
        Id = id;
        ProjectId = projectId;
        DisplayName = displayName;
        Kind = kind;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public string DisplayName { get; private set; }

    public ProviderKind Kind { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}

public enum ProviderKind
{
    Fake = 0,
    OpenAI = 1,
}
