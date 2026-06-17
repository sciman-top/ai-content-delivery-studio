using System.Text.Json.Serialization;

namespace ContentDeliveryStudio.Core.Artifacts;

public sealed class OutputArtifact
{
    private OutputArtifact()
    {
        DisplayName = string.Empty;
        RelativePath = string.Empty;
        MimeType = string.Empty;
        Role = string.Empty;
        SourceAssetIds = [];
        EvidenceAnchorIds = [];
        Metadata = new Dictionary<string, string>();
    }

    [JsonConstructor]
    public OutputArtifact(
        Guid id,
        Guid projectId,
        OutputArtifactKind kind,
        OutputArtifactStatus status,
        string displayName,
        string relativePath,
        string mimeType,
        string role,
        IReadOnlyList<Guid> sourceAssetIds,
        IReadOnlyList<Guid> evidenceAnchorIds,
        IReadOnlyDictionary<string, string> metadata,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = RequireNonEmptyId(id, nameof(id));
        ProjectId = RequireNonEmptyId(projectId, nameof(projectId));
        Kind = RequireDefinedKind(kind);
        Status = RequireDefinedStatus(status);
        DisplayName = RequireText(displayName, nameof(displayName));
        RelativePath = RequireText(relativePath, nameof(relativePath));
        MimeType = RequireText(mimeType, nameof(mimeType));
        Role = RequireText(role, nameof(role));
        SourceAssetIds = NormalizeIds(sourceAssetIds, nameof(sourceAssetIds));
        EvidenceAnchorIds = NormalizeIds(evidenceAnchorIds, nameof(evidenceAnchorIds));
        Metadata = NormalizeMetadata(metadata);
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public OutputArtifactKind Kind { get; private set; }

    public OutputArtifactStatus Status { get; private set; }

    public string DisplayName { get; private set; }

    public string RelativePath { get; private set; }

    public string MimeType { get; private set; }

    public string Role { get; private set; }

    public IReadOnlyList<Guid> SourceAssetIds { get; private set; }

    public IReadOnlyList<Guid> EvidenceAnchorIds { get; private set; }

    public IReadOnlyDictionary<string, string> Metadata { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static OutputArtifact Create(
        Guid projectId,
        OutputArtifactKind kind,
        string displayName,
        string relativePath,
        string mimeType,
        string role,
        IReadOnlyList<Guid> sourceAssetIds,
        IReadOnlyList<Guid> evidenceAnchorIds,
        IReadOnlyDictionary<string, string> metadata,
        DateTimeOffset createdAt)
    {
        return new OutputArtifact(
            Guid.NewGuid(),
            projectId,
            kind,
            OutputArtifactStatus.Generated,
            displayName,
            relativePath,
            mimeType,
            role,
            sourceAssetIds,
            evidenceAnchorIds,
            metadata,
            createdAt,
            createdAt);
    }

    public static OutputArtifact Plan(
        Guid projectId,
        OutputArtifactKind kind,
        string displayName,
        string relativePath,
        string mimeType,
        string role,
        IReadOnlyList<Guid> sourceAssetIds,
        IReadOnlyList<Guid> evidenceAnchorIds,
        IReadOnlyDictionary<string, string> metadata,
        DateTimeOffset createdAt)
    {
        return new OutputArtifact(
            Guid.NewGuid(),
            projectId,
            kind,
            OutputArtifactStatus.Planned,
            displayName,
            relativePath,
            mimeType,
            role,
            sourceAssetIds,
            evidenceAnchorIds,
            metadata,
            createdAt,
            createdAt);
    }

    public void Approve(DateTimeOffset timestamp)
    {
        Status = OutputArtifactStatus.Approved;
        UpdatedAt = timestamp;
    }

    public void Reject(DateTimeOffset timestamp)
    {
        Status = OutputArtifactStatus.Rejected;
        UpdatedAt = timestamp;
    }

    public void MarkPackaged(DateTimeOffset timestamp)
    {
        Status = OutputArtifactStatus.Packaged;
        UpdatedAt = timestamp;
    }

    internal static Guid RequireNonEmptyId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value;
    }

    internal static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    internal static IReadOnlyList<Guid> NormalizeIds(IReadOnlyList<Guid> values, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Any(value => value == Guid.Empty))
        {
            throw new ArgumentException("Ids cannot contain empty values.", parameterName);
        }

        return values.Distinct().ToArray();
    }

    internal static IReadOnlyDictionary<string, string> NormalizeMetadata(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in metadata)
        {
            var key = pair.Key?.Trim() ?? string.Empty;
            if (key.Length == 0)
            {
                continue;
            }

            normalized[key] = pair.Value?.Trim() ?? string.Empty;
        }

        return normalized;
    }

    private static OutputArtifactKind RequireDefinedKind(OutputArtifactKind kind)
    {
        return Enum.IsDefined(typeof(OutputArtifactKind), kind)
            ? kind
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, "Output artifact kind is not supported.");
    }

    private static OutputArtifactStatus RequireDefinedStatus(OutputArtifactStatus status)
    {
        return Enum.IsDefined(typeof(OutputArtifactStatus), status)
            ? status
            : throw new ArgumentOutOfRangeException(nameof(status), status, "Output artifact status is not supported.");
    }
}

public enum OutputArtifactKind
{
    Image = 0,
    Pdf = 1,
    Docx = 2,
    Markdown = 3,
    ReviewReport = 4,
    Manifest = 5,
    Archive = 6,
    SlideDeck = 7,
    Spreadsheet = 8,
    Text = 9,
}

public enum OutputArtifactStatus
{
    Planned = 0,
    Generated = 1,
    Approved = 2,
    Rejected = 3,
    Packaged = 4,
}

public sealed class ArtifactPackage
{
    private ArtifactPackage()
    {
        Name = string.Empty;
        OutputDirectory = string.Empty;
        Manifest = ArtifactManifest.Empty;
    }

    [JsonConstructor]
    public ArtifactPackage(
        Guid id,
        Guid projectId,
        string name,
        string outputDirectory,
        ArtifactManifest manifest,
        DateTimeOffset createdAt)
    {
        Id = OutputArtifact.RequireNonEmptyId(id, nameof(id));
        ProjectId = OutputArtifact.RequireNonEmptyId(projectId, nameof(projectId));
        Name = OutputArtifact.RequireText(name, nameof(name));
        OutputDirectory = OutputArtifact.RequireText(outputDirectory, nameof(outputDirectory));
        Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        if (Manifest.ProjectId != ProjectId)
        {
            throw new ArgumentException("Artifact manifest must belong to this package project.", nameof(manifest));
        }

        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public string Name { get; private set; }

    public string OutputDirectory { get; private set; }

    public ArtifactManifest Manifest { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static ArtifactPackage Create(
        Guid projectId,
        string name,
        string outputDirectory,
        int version,
        IReadOnlyList<OutputArtifact> artifacts,
        DateTimeOffset createdAt)
    {
        return new ArtifactPackage(
            Guid.NewGuid(),
            projectId,
            name,
            outputDirectory,
            ArtifactManifest.Create(projectId, version, artifacts, createdAt),
            createdAt);
    }
}

public sealed class ArtifactManifest
{
    internal static ArtifactManifest Empty { get; } = new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        1,
        [new ArtifactManifestItem(
            Guid.NewGuid(),
            OutputArtifactKind.Manifest,
            "empty",
            "empty",
            "application/json",
            "empty",
            [],
            [],
            new Dictionary<string, string>())],
        DateTimeOffset.MinValue);

    [JsonConstructor]
    public ArtifactManifest(
        Guid id,
        Guid projectId,
        int version,
        IReadOnlyList<ArtifactManifestItem> items,
        DateTimeOffset createdAt)
    {
        Id = OutputArtifact.RequireNonEmptyId(id, nameof(id));
        ProjectId = OutputArtifact.RequireNonEmptyId(projectId, nameof(projectId));
        Version = RequirePositiveVersion(version, nameof(version));
        Items = NormalizeItems(items);
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public int Version { get; private set; }

    public IReadOnlyList<ArtifactManifestItem> Items { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static ArtifactManifest Create(
        Guid projectId,
        int version,
        IReadOnlyList<OutputArtifact> artifacts,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(artifacts);

        if (artifacts.Count == 0)
        {
            throw new ArgumentException("At least one output artifact is required.", nameof(artifacts));
        }

        if (artifacts.Any(artifact => artifact.ProjectId != projectId))
        {
            throw new ArgumentException("All output artifacts must belong to the manifest project.", nameof(artifacts));
        }

        return new ArtifactManifest(
            Guid.NewGuid(),
            projectId,
            version,
            artifacts.Select(ArtifactManifestItem.FromArtifact).ToArray(),
            createdAt);
    }

    private static int RequirePositiveVersion(int version, string parameterName)
    {
        if (version < 1)
        {
            throw new ArgumentOutOfRangeException(parameterName, version, "Version must be positive.");
        }

        return version;
    }

    private static IReadOnlyList<ArtifactManifestItem> NormalizeItems(IReadOnlyList<ArtifactManifestItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
        {
            throw new ArgumentException("At least one manifest item is required.", nameof(items));
        }

        var duplicateId = items
            .GroupBy(item => item.OutputArtifactId)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicateId is not null)
        {
            throw new ArgumentException($"Duplicate output artifact id in manifest: {duplicateId}", nameof(items));
        }

        return items.ToArray();
    }
}

public sealed class ArtifactManifestItem
{
    [JsonConstructor]
    public ArtifactManifestItem(
        Guid outputArtifactId,
        OutputArtifactKind kind,
        string displayName,
        string relativePath,
        string mimeType,
        string role,
        IReadOnlyList<Guid> sourceAssetIds,
        IReadOnlyList<Guid> evidenceAnchorIds,
        IReadOnlyDictionary<string, string> metadata)
    {
        OutputArtifactId = OutputArtifact.RequireNonEmptyId(outputArtifactId, nameof(outputArtifactId));
        Kind = kind;
        DisplayName = OutputArtifact.RequireText(displayName, nameof(displayName));
        RelativePath = OutputArtifact.RequireText(relativePath, nameof(relativePath));
        MimeType = OutputArtifact.RequireText(mimeType, nameof(mimeType));
        Role = OutputArtifact.RequireText(role, nameof(role));
        SourceAssetIds = OutputArtifact.NormalizeIds(sourceAssetIds, nameof(sourceAssetIds));
        EvidenceAnchorIds = OutputArtifact.NormalizeIds(evidenceAnchorIds, nameof(evidenceAnchorIds));
        Metadata = OutputArtifact.NormalizeMetadata(metadata);
    }

    public Guid OutputArtifactId { get; private set; }

    public OutputArtifactKind Kind { get; private set; }

    public string DisplayName { get; private set; }

    public string RelativePath { get; private set; }

    public string MimeType { get; private set; }

    public string Role { get; private set; }

    public IReadOnlyList<Guid> SourceAssetIds { get; private set; }

    public IReadOnlyList<Guid> EvidenceAnchorIds { get; private set; }

    public IReadOnlyDictionary<string, string> Metadata { get; private set; }

    public static ArtifactManifestItem FromArtifact(OutputArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        return new ArtifactManifestItem(
            artifact.Id,
            artifact.Kind,
            artifact.DisplayName,
            artifact.RelativePath,
            artifact.MimeType,
            artifact.Role,
            artifact.SourceAssetIds,
            artifact.EvidenceAnchorIds,
            artifact.Metadata);
    }
}
