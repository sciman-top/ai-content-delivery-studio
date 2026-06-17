using System.Text.Json;
using ContentDeliveryStudio.Application.ToolAdapters;

namespace ContentDeliveryStudio.Infrastructure.ToolAdapters;

public sealed class ArtifactValidationToolAdapter : IToolAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };
    private static readonly ToolAdapterDescriptor BuiltInDescriptor =
        LocalToolRegistry.CreateBuiltIn().GetRequired("artifact-validation");

    public ToolAdapterDescriptor Descriptor => BuiltInDescriptor;

    public async Task<ToolAdapterRunResult> RunAsync(
        ToolAdapterRunRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var manifestPath = GetRequiredInput(request, "manifestPath");
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("Manifest file was not found.", manifestPath);
        }

        var reportPath = GetReportPath(manifestPath);
        var manifestJson = await File.ReadAllTextAsync(manifestPath, cancellationToken);

        using var document = JsonDocument.Parse(manifestJson);
        var errors = new List<string>();
        var warnings = new List<string>();

        var root = document.RootElement;
        if (root.ValueKind is not JsonValueKind.Object)
        {
            throw new InvalidOperationException("Manifest root must be a JSON object.");
        }

        var projectName = ReadRequiredString(root, "projectName", errors);
        var items = root.TryGetProperty("items", out var itemsElement) && itemsElement.ValueKind is JsonValueKind.Array
            ? itemsElement.EnumerateArray().ToArray()
            : [];
        if (!root.TryGetProperty("items", out var itemsProperty) || itemsProperty.ValueKind is not JsonValueKind.Array)
        {
            errors.Add("Manifest must contain an items array.");
        }

        for (var index = 0; index < items.Length; index++)
        {
            var item = items[index];
            if (item.ValueKind is not JsonValueKind.Object)
            {
                errors.Add($"items[{index}] must be a JSON object.");
                continue;
            }

            ReadRequiredString(item, "itemKey", errors, $"items[{index}].itemKey");
            ReadRequiredString(item, "title", errors, $"items[{index}].title");
            ReadRequiredString(item, "imagePath", errors, $"items[{index}].imagePath");
            ReadRequiredString(item, "promptPath", errors, $"items[{index}].promptPath");
        }

        if (items.Length == 0)
        {
            warnings.Add("Manifest contains no items.");
        }

        var report = new ArtifactValidationReport(
            manifestPath,
            reportPath,
            projectName,
            items.Length,
            errors.Count == 0,
            errors,
            warnings,
            [
                "Delete the generated diagnostics report if you want to discard this validation run.",
            ],
            DateTimeOffset.UtcNow);

        if (request.DryRun)
        {
            return ToolAdapterRunResult.Create(
                Descriptor.Id,
                dryRun: true,
                new Dictionary<string, string>
                {
                    ["validationReportPath"] = reportPath,
                },
                warnings,
                $"Artifact validation dry-run planned {items.Length} item(s).");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        await File.WriteAllTextAsync(
            reportPath,
            JsonSerializer.Serialize(report, JsonOptions),
            cancellationToken);

        return ToolAdapterRunResult.Create(
            Descriptor.Id,
            dryRun: false,
            new Dictionary<string, string>
            {
                ["validationReportPath"] = reportPath,
            },
            warnings,
            errors.Count == 0
                ? $"Artifact validation succeeded with {items.Length} item(s)."
                : $"Artifact validation completed with {errors.Count} error(s) and {warnings.Count} warning(s).");
    }

    private static string GetRequiredInput(ToolAdapterRunRequest request, string inputName)
    {
        if (!request.Inputs.TryGetValue(inputName, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Tool adapter input is missing: {inputName}");
        }

        return value.Trim();
    }

    private static string GetReportPath(string manifestPath)
    {
        var manifestDirectory = Path.GetDirectoryName(manifestPath)
            ?? throw new InvalidOperationException("Manifest path does not have a directory.");
        var rootDirectory = Directory.GetParent(manifestDirectory)?.FullName ?? manifestDirectory;
        return Path.Combine(
            rootDirectory,
            "diagnostics",
            $"{Path.GetFileNameWithoutExtension(manifestPath)}.validation.json");
    }

    private static string? ReadRequiredString(
        JsonElement element,
        string propertyName,
        List<string> errors,
        string? label = null)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is not JsonValueKind.String ||
            string.IsNullOrWhiteSpace(property.GetString()))
        {
            errors.Add($"{label ?? propertyName} is required.");
            return null;
        }

        return property.GetString()!.Trim();
    }
}

internal sealed record ArtifactValidationReport(
    string ManifestPath,
    string ValidationReportPath,
    string? ProjectName,
    int ItemCount,
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> RollbackNotes,
    DateTimeOffset ValidatedAt);
