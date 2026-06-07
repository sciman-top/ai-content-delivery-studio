using System.Text.Json;
using ImageSeriesStudio.Application.ToolAdapters;

namespace ImageSeriesStudio.Infrastructure.ToolAdapters;

public sealed class ArtifactValidationToolAdapter : IToolAdapter
{
    private const string RollbackNote = "No rollback needed; additive validation output only.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public ArtifactValidationToolAdapter()
    {
        Descriptor = LocalToolRegistry.CreateBuiltIn().GetRequired("artifact-validation");
    }

    public ToolAdapterDescriptor Descriptor { get; }

    public async Task<ToolAdapterRunResult> RunAsync(
        ToolAdapterRunRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var manifestPath = ResolveManifestPath(request.Inputs);
        await using var manifestStream = File.OpenRead(manifestPath);
        using var manifest = await JsonDocument.ParseAsync(manifestStream, cancellationToken: cancellationToken);

        var manifestDirectory = Path.GetDirectoryName(manifestPath)
            ?? throw new InvalidOperationException($"Manifest directory could not be resolved: {manifestPath}");
        var checkedArtifacts = new List<ArtifactValidationEntry>();
        var missingPaths = new List<string>();
        var warnings = new List<string>();
        var itemCount = 0;

        if (manifest.RootElement.TryGetProperty("items", out var itemsElement)
            && itemsElement.ValueKind is JsonValueKind.Array)
        {
            foreach (var item in itemsElement.EnumerateArray())
            {
                itemCount++;
                ValidateArtifactPath(item, "imagePath", manifestDirectory, checkedArtifacts, missingPaths);
                ValidateArtifactPath(item, "promptPath", manifestDirectory, checkedArtifacts, missingPaths);
                ValidateArtifactPath(item, "metadataPath", manifestDirectory, checkedArtifacts, missingPaths, optional: true);
            }
        }

        if (itemCount == 0)
        {
            warnings.Add("Manifest contains no delivery items.");
        }

        if (missingPaths.Count > 0)
        {
            warnings.Add($"Missing artifact path count: {missingPaths.Count}.");
        }

        // V1 首个真实 operator 路线只允许生成新的验证产物，不回写原交付目录中的既有文件。
        var reportDirectory = Path.Combine(manifestDirectory, "validation");
        Directory.CreateDirectory(reportDirectory);

        var reportPath = Path.Combine(
            reportDirectory,
            $"{Path.GetFileNameWithoutExtension(manifestPath)}.validation.json");
        var report = new ArtifactValidationReport(
            Descriptor.Id,
            manifestPath,
            manifest.RootElement.TryGetProperty("projectName", out var projectName)
                ? projectName.GetString() ?? string.Empty
                : string.Empty,
            request.DryRun,
            DateTimeOffset.UtcNow,
            itemCount,
            checkedArtifacts,
            missingPaths.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            warnings,
            RollbackNote,
            IsValid: missingPaths.Count == 0);

        await File.WriteAllTextAsync(
            reportPath,
            JsonSerializer.Serialize(report, JsonOptions),
            cancellationToken);

        var summary = report.IsValid
            ? $"Artifact validation succeeded for {itemCount} item(s). {RollbackNote}"
            : $"Artifact validation found {missingPaths.Count} missing artifact path(s). {RollbackNote}";

        return ToolAdapterRunResult.Create(
            Descriptor.Id,
            request.DryRun,
            new Dictionary<string, string>
            {
                ["validationReportPath"] = reportPath,
            },
            warnings,
            summary);
    }

    private static string ResolveManifestPath(IReadOnlyDictionary<string, string> inputs)
    {
        if (!inputs.TryGetValue("manifestPath", out var manifestPath))
        {
            throw new InvalidOperationException("Artifact validation requires manifestPath.");
        }

        var fullPath = Path.GetFullPath(manifestPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Artifact validation manifest was not found.", fullPath);
        }

        return fullPath;
    }

    private static void ValidateArtifactPath(
        JsonElement item,
        string propertyName,
        string manifestDirectory,
        ICollection<ArtifactValidationEntry> checkedArtifacts,
        ICollection<string> missingPaths,
        bool optional = false)
    {
        if (!item.TryGetProperty(propertyName, out var propertyValue)
            || propertyValue.ValueKind is JsonValueKind.Null)
        {
            if (!optional)
            {
                missingPaths.Add(propertyName);
            }

            return;
        }

        var relativePath = propertyValue.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            if (!optional)
            {
                missingPaths.Add(propertyName);
            }

            return;
        }

        var resolvedPath = Path.GetFullPath(Path.Combine(manifestDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var exists = File.Exists(resolvedPath);
        checkedArtifacts.Add(new ArtifactValidationEntry(propertyName, relativePath, resolvedPath, exists));

        if (!exists)
        {
            missingPaths.Add(relativePath);
        }
    }
}

public sealed record ArtifactValidationReport(
    string ToolAdapterId,
    string ManifestPath,
    string ProjectName,
    bool DryRun,
    DateTimeOffset GeneratedAt,
    int ItemCount,
    IReadOnlyList<ArtifactValidationEntry> CheckedArtifacts,
    IReadOnlyList<string> MissingPaths,
    IReadOnlyList<string> Warnings,
    string RollbackNote,
    bool IsValid);

public sealed record ArtifactValidationEntry(
    string ArtifactField,
    string RelativePath,
    string ResolvedPath,
    bool Exists);
