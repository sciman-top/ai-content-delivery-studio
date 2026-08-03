using System.Text.Json;
using System.Text.Json.Serialization;
using ContentDeliveryStudio.Application.Packs;

namespace ContentDeliveryStudio.Infrastructure.Packs;

public sealed class JsonPackPackageStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public async Task<PackPackage> ImportAsync(
        string packagePath,
        string appVersion,
        CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(RequireText(packagePath, nameof(packagePath)));
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Pack package file does not exist.", fullPath);
        }

        await using var stream = File.OpenRead(fullPath);
        var package = await JsonSerializer.DeserializeAsync<PackPackage>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Pack package file is empty.");

        if (!package.SchemaVersion.Equals(PackPackage.CurrentSchemaVersion, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unsupported pack package schema: {package.SchemaVersion}");
        }

        try
        {
            return package.ValidateForAppVersion(appVersion);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            throw new InvalidOperationException("Pack package failed validation.", ex);
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
