using System.Text;
using System.Text.Json;
using ImageSeriesStudio.Core.Projects;

namespace ImageSeriesStudio.Infrastructure.Import;

public sealed class PhysicsPosterImportService
{
    public async Task<ImageProject> ImportAsync(
        string sourceRoot,
        int maxItems,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        if (maxItems <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxItems), "Max items must be positive.");
        }

        var root = Path.GetFullPath(sourceRoot);
        var manifestPath = Path.Combine(root, "manifest.json");
        var masterListPath = Path.Combine(root, "tables", "master-list.csv");

        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("Physics project manifest was not found.", manifestPath);
        }

        if (!File.Exists(masterListPath))
        {
            throw new FileNotFoundException("Physics project master list was not found.", masterListPath);
        }

        var manifest = await LoadManifestAsync(manifestPath, cancellationToken);
        var rows = await LoadRowsAsync(masterListPath, cancellationToken);
        var project = ImageProject.Create("Imported physics poster sample", timestamp);
        var providerProfile = project.AddProviderProfile("Imported OpenAI prompt", ProviderKind.OpenAI, timestamp);
        var settings = CreateGenerationSettings(manifest);
        var seriesByDomain = new Dictionary<string, ImageSeries>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows.Take(maxItems))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var promptRelativePath = RequireField(row, "prompt_file");
            var promptPath = ResolveInsideRoot(root, promptRelativePath);
            var promptText = await File.ReadAllTextAsync(promptPath, Encoding.UTF8, cancellationToken);
            var domainKey = RequireField(row, "domain_folder");
            var domainTitle = ReadField(row, "domain_cn", domainKey);
            var series = GetOrCreateSeries(project, seriesByDomain, domainKey, domainTitle, timestamp);
            var item = series.AddItem(
                ReadField(row, "title_cn", ReadField(row, "cn_name", Path.GetFileNameWithoutExtension(promptPath))),
                BuildBrief(row),
                timestamp);

            item.AddPromptVersion(promptText, settings, providerProfile.Id, timestamp);
        }

        return project;
    }

    private static async Task<PhysicsManifest> LoadManifestAsync(
        string manifestPath,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(manifestPath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        return new PhysicsManifest(
            ReadJsonString(root, "project", "physicist_chinese_poster_batch_tool"),
            ReadJsonString(root, "model_default", "gpt-image-2"),
            ReadJsonString(root, "production_size", "1024x1024"));
    }

    private static async Task<IReadOnlyList<IReadOnlyDictionary<string, string>>> LoadRowsAsync(
        string masterListPath,
        CancellationToken cancellationToken)
    {
        var lines = await File.ReadAllLinesAsync(masterListPath, Encoding.UTF8, cancellationToken);
        if (lines.Length == 0)
        {
            return [];
        }

        var headers = ParseCsvLine(lines[0]);
        return lines
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line =>
            {
                var values = ParseCsvLine(line);
                return headers
                    .Select((header, index) => new
                    {
                        Header = header,
                        Value = index < values.Count ? values[index] : string.Empty,
                    })
                    .ToDictionary(pair => pair.Header, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
            })
            .ToArray();
    }

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var builder = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    builder.Append('"');
                    index++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (character == ',' && !inQuotes)
            {
                values.Add(builder.ToString());
                builder.Clear();
            }
            else
            {
                builder.Append(character);
            }
        }

        values.Add(builder.ToString());
        return values;
    }

    private static ImageSeries GetOrCreateSeries(
        ImageProject project,
        IDictionary<string, ImageSeries> seriesByDomain,
        string domainKey,
        string domainTitle,
        DateTimeOffset timestamp)
    {
        if (seriesByDomain.TryGetValue(domainKey, out var existing))
        {
            return existing;
        }

        var series = project.AddSeries(domainTitle, $"Imported physics domain: {domainKey}", timestamp);
        seriesByDomain.Add(domainKey, series);
        return series;
    }

    private static GenerationSettings CreateGenerationSettings(PhysicsManifest manifest)
    {
        var size = manifest.ProductionSize.Split('x', StringSplitOptions.TrimEntries);
        var width = size.Length == 2 && int.TryParse(size[0], out var parsedWidth) ? parsedWidth : 1024;
        var height = size.Length == 2 && int.TryParse(size[1], out var parsedHeight) ? parsedHeight : 1024;

        return new GenerationSettings(width, height, "high", "png");
    }

    private static string BuildBrief(IReadOnlyDictionary<string, string> row)
    {
        return string.Join(
            Environment.NewLine,
            new[]
            {
                ReadField(row, "subtitle_cn", string.Empty),
                ReadField(row, "hook_cn", string.Empty),
                ReadField(row, "primary_contribution", string.Empty),
                ReadField(row, "scientific_caution", string.Empty),
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string ResolveInsideRoot(string root, string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Prompt path escapes source root: {relativePath}");
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Physics prompt file was not found.", fullPath);
        }

        return fullPath;
    }

    private static string RequireField(IReadOnlyDictionary<string, string> row, string key)
    {
        var value = ReadField(row, key, string.Empty);
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Required physics import field is missing: {key}")
            : value;
    }

    private static string ReadField(IReadOnlyDictionary<string, string> row, string key, string fallback)
    {
        return row.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : fallback;
    }

    private static string ReadJsonString(JsonElement root, string propertyName, string fallback)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind is JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;
    }

    private sealed record PhysicsManifest(
        string Project,
        string ModelDefault,
        string ProductionSize);
}
