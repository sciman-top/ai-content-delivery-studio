namespace ContentDeliveryStudio.Application.Projects;

public static class LocalStudioDataPaths
{
    public const string DataRootEnvironmentVariable = "CONTENT_DELIVERY_STUDIO_DATA_ROOT";
    public const string DeliveryRootEnvironmentVariable = "CONTENT_DELIVERY_STUDIO_DELIVERY_ROOT";
    public const string WorkspaceAreaName = "workspace";
    public const string DeliveriesAreaName = "deliveries";
    internal const string CurrentStudioFolderName = "ContentDeliveryStudio";
    internal const string LegacyStudioFolderName = "ImageSeriesStudio";
    private static readonly AsyncLocal<string?> RootOverride = new();

    private static readonly IReadOnlyDictionary<FinalImageDeliveryCategory, string> FinalDeliveryCategorySlugs =
        new Dictionary<FinalImageDeliveryCategory, string>
        {
            [FinalImageDeliveryCategory.ImageSeries] = "image-series",
            [FinalImageDeliveryCategory.ImageEdits] = "image-edits",
            [FinalImageDeliveryCategory.ArticleFigureSets] = "article-figure-sets",
            [FinalImageDeliveryCategory.ScientificFigures] = "scientific-figures",
            [FinalImageDeliveryCategory.DocumentIllustrations] = "document-illustrations",
            [FinalImageDeliveryCategory.CoursewareVisuals] = "courseware-visuals",
            [FinalImageDeliveryCategory.PosterReportVisuals] = "poster-report-visuals",
        };

    public static string ResolveStudioRoot()
    {
        return ResolveStudioRoot(Environment.GetEnvironmentVariable(DataRootEnvironmentVariable));
    }

    internal static string ResolveStudioRoot(string? environmentRootOverride)
    {
        var overrideRoot = RootOverride.Value;
        if (string.IsNullOrWhiteSpace(overrideRoot))
        {
            overrideRoot = environmentRootOverride;
        }

        var root = string.IsNullOrWhiteSpace(overrideRoot)
            ? ResolveStudioRootPath(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
            : Path.GetFullPath(overrideRoot);

        return root;
    }

    internal static string ResolveStudioRootPath(string localAppDataRoot)
    {
        if (string.IsNullOrWhiteSpace(localAppDataRoot))
        {
            throw new ArgumentException("Local app data root cannot be empty.", nameof(localAppDataRoot));
        }

        var normalizedBaseRoot = Path.GetFullPath(localAppDataRoot);
        var currentRoot = Path.Combine(normalizedBaseRoot, CurrentStudioFolderName);
        if (Directory.Exists(currentRoot))
        {
            return currentRoot;
        }

        var legacyRoot = Path.Combine(normalizedBaseRoot, LegacyStudioFolderName);
        if (Directory.Exists(legacyRoot))
        {
            return legacyRoot;
        }

        return currentRoot;
    }

    public static string ResolveProjectDirectory(string areaName, Guid projectId)
    {
        return Path.Combine(
            ResolveStudioRoot(),
            NormalizeAreaName(areaName),
            projectId.ToString("N"));
    }

    public static string ResolveTimestampedProjectDirectory(
        string areaName,
        Guid projectId,
        DateTimeOffset timestamp)
    {
        return Path.Combine(
            ResolveProjectDirectory(areaName, projectId),
            timestamp.ToString("yyyyMMdd-HHmmss"));
    }

    public static string ResolveWorkspaceProjectDirectory(string categoryName, Guid projectId)
    {
        return Path.Combine(
            ResolveProjectDirectory(WorkspaceAreaName, projectId),
            NormalizeAreaName(categoryName));
    }

    public static string ResolveFinalDeliveryDirectory(Guid projectId, DateTimeOffset timestamp)
    {
        return Path.Combine(
            ResolveConfiguredDeliveryRoot(customRoot: null),
            projectId.ToString("N"),
            timestamp.ToString("yyyyMMdd-HHmmss"));
    }

    /// <summary>
    /// Returns the stable category root for approved final deliveries.
    /// Candidates, edits-in-progress, crops, and review evidence must not use this root.
    /// </summary>
    public static string ResolveFinalDeliveryCategoryRoot(
        FinalImageDeliveryCategory category,
        string? customRoot = null)
    {
        return Path.Combine(
            ResolveConfiguredDeliveryRoot(customRoot),
            GetFinalDeliveryCategorySlug(category));
    }

    public static string ResolveFinalDeliveryPackageDirectory(
        FinalImageDeliveryCategory category,
        Guid projectId,
        DateTimeOffset timestamp,
        string? customRoot = null)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        return Path.Combine(
            ResolveFinalDeliveryCategoryRoot(category, customRoot),
            projectId.ToString("N"),
            timestamp.ToUniversalTime().ToString("yyyyMMdd-HHmmss"));
    }

    public static string ResolveFinalImageDirectory(
        FinalImageDeliveryCategory category,
        Guid projectId,
        DateTimeOffset timestamp,
        string? customRoot = null)
    {
        return Path.Combine(
            ResolveFinalDeliveryPackageDirectory(category, projectId, timestamp, customRoot),
            "images");
    }

    public static string GetFinalDeliveryCategorySlug(FinalImageDeliveryCategory category)
    {
        return FinalDeliveryCategorySlugs.TryGetValue(category, out var slug)
            ? slug
            : throw new ArgumentOutOfRangeException(nameof(category), category, "Final delivery category is not supported.");
    }

    public static string ResolveDeliveryRoot()
    {
        return ResolveDeliveryRoot(Environment.GetEnvironmentVariable(DeliveryRootEnvironmentVariable));
    }

    private static string ResolveConfiguredDeliveryRoot(string? customRoot)
    {
        var resolvedRoot = string.IsNullOrWhiteSpace(customRoot)
            ? ResolveDeliveryRoot()
            : Path.GetFullPath(customRoot.Trim());
        var workspaceRoot = Path.GetFullPath(
            Path.Combine(ResolveStudioRoot(), WorkspaceAreaName));
        if (IsSameOrDescendantPath(resolvedRoot, workspaceRoot))
        {
            throw new ArgumentException(
                "Final delivery root must stay outside the temporary workspace.",
                nameof(customRoot));
        }

        return resolvedRoot;
    }

    internal static string ResolveDeliveryRoot(string? environmentRootOverride)
    {
        var rootOverride = RootOverride.Value;
        if (!string.IsNullOrWhiteSpace(rootOverride))
        {
            return Path.Combine(rootOverride, DeliveriesAreaName);
        }

        return string.IsNullOrWhiteSpace(environmentRootOverride)
            ? Path.Combine(ResolveStudioRoot(), DeliveriesAreaName)
            : Path.GetFullPath(environmentRootOverride);
    }

    public static IDisposable PushRootOverride(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Root path cannot be empty.", nameof(rootPath));
        }

        var previousRoot = RootOverride.Value;
        RootOverride.Value = Path.GetFullPath(rootPath);
        return new RootOverrideScope(previousRoot);
    }

    private sealed class RootOverrideScope(string? previousRoot) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            RootOverride.Value = previousRoot;
            _disposed = true;
        }
    }

    private static string NormalizeAreaName(string areaName)
    {
        if (string.IsNullOrWhiteSpace(areaName))
        {
            throw new ArgumentException("Area name cannot be empty.", nameof(areaName));
        }

        var normalizedAreaName = areaName.Trim();
        if (Path.IsPathRooted(normalizedAreaName))
        {
            throw new ArgumentException("Area name must stay within a safe app-local folder.", nameof(areaName));
        }

        if (normalizedAreaName.Contains(Path.DirectorySeparatorChar)
            || normalizedAreaName.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new ArgumentException("Area name must stay within a safe app-local folder.", nameof(areaName));
        }

        if (normalizedAreaName is "." or "..")
        {
            throw new ArgumentException("Area name must stay within a safe app-local folder.", nameof(areaName));
        }

        if (normalizedAreaName.Split('.', StringSplitOptions.RemoveEmptyEntries).Any(segment => segment == ".."))
        {
            throw new ArgumentException("Area name must stay within a safe app-local folder.", nameof(areaName));
        }

        return normalizedAreaName;
    }

    private static bool IsSameOrDescendantPath(string candidatePath, string parentPath)
    {
        var normalizedCandidate = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidatePath));
        var normalizedParent = Path.TrimEndingDirectorySeparator(Path.GetFullPath(parentPath));
        return string.Equals(normalizedCandidate, normalizedParent, StringComparison.OrdinalIgnoreCase)
            || normalizedCandidate.StartsWith(
                normalizedParent + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase)
            || normalizedCandidate.StartsWith(
                normalizedParent + Path.AltDirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Stable user-facing buckets for approved final visual delivery assets.
/// The enum is intentionally finite so a caller cannot turn a title or prompt into a path segment.
/// </summary>
public enum FinalImageDeliveryCategory
{
    ImageSeries = 0,
    ImageEdits = 1,
    ArticleFigureSets = 2,
    ScientificFigures = 3,
    DocumentIllustrations = 4,
    CoursewareVisuals = 5,
    PosterReportVisuals = 6,
}
