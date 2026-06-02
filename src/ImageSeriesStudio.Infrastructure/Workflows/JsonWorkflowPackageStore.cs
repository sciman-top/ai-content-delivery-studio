using System.Text.Json;
using System.Text.Json.Serialization;
using ImageSeriesStudio.Application.Workflows;

namespace ImageSeriesStudio.Infrastructure.Workflows;

public sealed class JsonWorkflowPackageStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public async Task ExportAsync(
        WorkflowPackage package,
        string packagePath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(package);
        var validated = WorkflowPackage.Create(
            package.Name,
            package.ExportedAt,
            package.StyleGuides,
            package.GenerationRecipes,
            package.ReferenceImageSets,
            package.ParameterExperiments);
        var fullPath = Path.GetFullPath(RequireText(packagePath, nameof(packagePath)));
        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException("Workflow package path must include a directory.", nameof(packagePath));
        }

        Directory.CreateDirectory(directory);
        await using var stream = File.Create(fullPath);
        await JsonSerializer.SerializeAsync(stream, validated, JsonOptions, cancellationToken);
    }

    public async Task<WorkflowPackage> ImportAsync(
        string packagePath,
        CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(RequireText(packagePath, nameof(packagePath)));
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Workflow package file does not exist.", fullPath);
        }

        await using var stream = File.OpenRead(fullPath);
        var package = await JsonSerializer.DeserializeAsync<WorkflowPackage>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Workflow package file is empty.");

        if (!package.SchemaVersion.Equals(WorkflowPackage.CurrentSchemaVersion, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unsupported workflow package schema: {package.SchemaVersion}");
        }

        try
        {
            return WorkflowPackage.Create(
                package.Name,
                package.ExportedAt,
                package.StyleGuides,
                package.GenerationRecipes,
                package.ReferenceImageSets,
                package.ParameterExperiments);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            throw new InvalidOperationException("Workflow package failed validation.", ex);
        }
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
