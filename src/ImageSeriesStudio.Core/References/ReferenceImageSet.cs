namespace ImageSeriesStudio.Core.References;

public sealed class ReferenceImageSet
{
    private readonly List<ReferenceImage> _images = [];

    private ReferenceImageSet()
    {
        Name = string.Empty;
    }

    private ReferenceImageSet(Guid id, Guid projectId, string name, DateTimeOffset createdAt)
    {
        Id = id;
        ProjectId = projectId;
        Name = RequireText(name, nameof(name));
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public string Name { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<ReferenceImage> Images => _images.AsReadOnly();

    public static ReferenceImageSet Create(Guid projectId, string name, DateTimeOffset createdAt)
    {
        return new ReferenceImageSet(Guid.NewGuid(), projectId, name, createdAt);
    }

    public ReferenceImage AddImage(
        string assetPath,
        ReferenceImageRole role,
        string description,
        DateTimeOffset timestamp)
    {
        var normalizedPath = NormalizeWorkspaceRelativePath(assetPath);
        if (_images.Any(image => image.AssetPath.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Reference image already exists in this set: {normalizedPath}");
        }

        var image = new ReferenceImage(
            Guid.NewGuid(),
            Id,
            normalizedPath,
            role,
            description.Trim(),
            timestamp);

        _images.Add(image);
        UpdatedAt = timestamp;
        return image;
    }

    private static string NormalizeWorkspaceRelativePath(string assetPath)
    {
        var trimmed = RequireText(assetPath, nameof(assetPath));
        if (Path.IsPathRooted(trimmed) || Uri.TryCreate(trimmed, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Reference image paths must be workspace-relative.", nameof(assetPath));
        }

        var segments = trimmed
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
        {
            throw new ArgumentException("Reference image paths cannot escape the workspace.", nameof(assetPath));
        }

        return string.Join("/", segments);
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

public sealed class ReferenceImage
{
    private ReferenceImage()
    {
        AssetPath = string.Empty;
        Description = string.Empty;
    }

    public ReferenceImage(
        Guid id,
        Guid referenceImageSetId,
        string assetPath,
        ReferenceImageRole role,
        string description,
        DateTimeOffset createdAt)
    {
        Id = id;
        ReferenceImageSetId = referenceImageSetId;
        AssetPath = assetPath;
        Role = role;
        Description = description;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ReferenceImageSetId { get; private set; }

    public string AssetPath { get; private set; }

    public ReferenceImageRole Role { get; private set; }

    public string Description { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}

public enum ReferenceImageRole
{
    Style = 0,
    Subject = 1,
    Composition = 2,
    Palette = 3,
    Mask = 4,
    Negative = 5,
}
