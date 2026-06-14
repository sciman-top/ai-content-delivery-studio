namespace ImageSeriesStudio.Infrastructure.OpenAI;

public sealed record ProviderEnvironmentConfiguration(
    ProviderEndpointEnvironmentConfiguration Text,
    ProviderEndpointEnvironmentConfiguration Image)
{
    public static ProviderEnvironmentConfiguration FromValues(IReadOnlyDictionary<string, string?> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return new ProviderEnvironmentConfiguration(
            ProviderEndpointEnvironmentConfiguration.CreateText(values),
            ProviderEndpointEnvironmentConfiguration.CreateImage(values));
    }

    public static async Task<ProviderEnvironmentConfiguration> FromDotEnvFileAsync(
        string envPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(envPath))
        {
            throw new ArgumentException("Environment file path cannot be empty.", nameof(envPath));
        }

        if (!File.Exists(envPath))
        {
            throw new FileNotFoundException("Environment file was not found.", envPath);
        }

        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        var lines = await File.ReadAllLinesAsync(envPath, cancellationToken);
        foreach (var line in lines)
        {
            var parsed = ParseDotEnvLine(line);
            if (parsed is { } item)
            {
                values[item.Name] = item.Value;
            }
        }

        return FromValues(values);
    }

    public IReadOnlyList<string> Validate()
    {
        return Text.Validate("Text provider")
            .Concat(Image.Validate("Image provider"))
            .ToArray();
    }

    private static (string Name, string Value)? ParseDotEnvLine(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#'))
        {
            return null;
        }

        var separatorIndex = trimmed.IndexOf('=');
        if (separatorIndex <= 0)
        {
            return null;
        }

        var name = trimmed[..separatorIndex].Trim();
        var value = trimmed[(separatorIndex + 1)..].Trim();
        if ((value.StartsWith('"') && value.EndsWith('"'))
            || (value.StartsWith('\'') && value.EndsWith('\'')))
        {
            value = value[1..^1];
        }

        return (name, value);
    }
}

public sealed record ProviderEndpointEnvironmentConfiguration(
    string Prefix,
    string Kind,
    Uri? BaseUri,
    string Model,
    string? ApiKeySecretName,
    IReadOnlyList<string> ApiKeySecretNames,
    bool UsesSharedTextApiKeyFallback,
    string? AppIdSecretName,
    string? AppSecretSecretName,
    int ConcurrencyPerKey,
    int TotalConcurrency)
{
    public static ProviderEndpointEnvironmentConfiguration CreateText(IReadOnlyDictionary<string, string?> values)
    {
        var keyNames = GetPresentSecretNames(values, "TEXT_PROVIDER_API_KEY");
        return new ProviderEndpointEnvironmentConfiguration(
            "TEXT_PROVIDER",
            GetValue(values, "TEXT_PROVIDER_KIND", "openai_compatible"),
            GetUri(values, "TEXT_PROVIDER_BASE_URL"),
            GetValue(values, "TEXT_PROVIDER_MODEL", string.Empty),
            keyNames.FirstOrDefault(),
            keyNames,
            UsesSharedTextApiKeyFallback: false,
            GetPresentSecretName(values, "TEXT_PROVIDER_APP_ID"),
            GetPresentSecretName(values, "TEXT_PROVIDER_APP_SECRET"),
            GetPositiveInt(values, "TEXT_PROVIDER_CONCURRENCY_PER_KEY", 1),
            GetPositiveInt(values, "TEXT_PROVIDER_TOTAL_CONCURRENCY", Math.Max(1, keyNames.Count)));
    }

    public static ProviderEndpointEnvironmentConfiguration CreateImage(IReadOnlyDictionary<string, string?> values)
    {
        var keyNames = GetNumberedSecretNames(values, "IMAGE_PROVIDER_API_KEY");
        if (keyNames.Count == 0)
        {
            keyNames = GetPresentSecretNames(values, "IMAGE_PROVIDER_API_KEY");
        }

        var usesSharedTextApiKeyFallback = false;
        if (keyNames.Count == 0)
        {
            keyNames = GetPresentSecretNames(values, "TEXT_PROVIDER_API_KEY");
            usesSharedTextApiKeyFallback = keyNames.Count > 0;
        }

        var concurrencyPerKey = GetPositiveInt(values, "IMAGE_PROVIDER_CONCURRENCY_PER_KEY", 1);
        return new ProviderEndpointEnvironmentConfiguration(
            "IMAGE_PROVIDER",
            GetValue(values, "IMAGE_PROVIDER_KIND", "openai_compatible_image_only"),
            GetUri(values, "IMAGE_PROVIDER_BASE_URL"),
            GetValue(values, "IMAGE_PROVIDER_MODEL", string.Empty),
            keyNames.FirstOrDefault(),
            keyNames,
            usesSharedTextApiKeyFallback,
            GetPresentSecretName(values, "IMAGE_PROVIDER_APP_ID"),
            GetPresentSecretName(values, "IMAGE_PROVIDER_APP_SECRET"),
            concurrencyPerKey,
            GetPositiveInt(values, "IMAGE_PROVIDER_TOTAL_CONCURRENCY", keyNames.Count * concurrencyPerKey));
    }

    public IReadOnlyList<string> Validate(string displayName)
    {
        var errors = new List<string>();

        if (BaseUri is null)
        {
            errors.Add($"{displayName} base URL is required.");
        }
        else if (BaseUri.Scheme is not ("http" or "https"))
        {
            errors.Add($"{displayName} base URL must be HTTP or HTTPS.");
        }

        if (string.IsNullOrWhiteSpace(Model))
        {
            errors.Add($"{displayName} model is required.");
        }

        if (ApiKeySecretNames.Count == 0)
        {
            errors.Add($"{displayName} API key is required.");
        }

        if (ConcurrencyPerKey <= 0)
        {
            errors.Add($"{displayName} concurrency per key must be positive.");
        }

        if (TotalConcurrency <= 0)
        {
            errors.Add($"{displayName} total concurrency must be positive.");
        }

        var expectedTotalConcurrency = ApiKeySecretNames.Count * ConcurrencyPerKey;
        if (ApiKeySecretNames.Count > 0 && TotalConcurrency != expectedTotalConcurrency)
        {
            errors.Add(
                $"{displayName} total concurrency must equal API key count multiplied by concurrency per key ({expectedTotalConcurrency}).");
        }

        if (AppIdSecretName is null ^ AppSecretSecretName is null)
        {
            errors.Add($"{displayName} app id and app secret must be configured together.");
        }

        return errors;
    }

    private static IReadOnlyList<string> GetNumberedSecretNames(IReadOnlyDictionary<string, string?> values, string baseName)
    {
        return values
            .Where(pair => pair.Key.StartsWith(baseName + "_", StringComparison.Ordinal)
                && int.TryParse(pair.Key[(baseName.Length + 1)..], out _)
                && !string.IsNullOrWhiteSpace(pair.Value))
            .OrderBy(pair => int.Parse(pair.Key[(baseName.Length + 1)..]))
            .Select(pair => pair.Key)
            .ToArray();
    }

    private static IReadOnlyList<string> GetPresentSecretNames(IReadOnlyDictionary<string, string?> values, string name)
    {
        return string.IsNullOrWhiteSpace(GetValue(values, name, string.Empty))
            ? []
            : [name];
    }

    private static string? GetPresentSecretName(IReadOnlyDictionary<string, string?> values, string name)
    {
        return string.IsNullOrWhiteSpace(GetValue(values, name, string.Empty))
            ? null
            : name;
    }

    private static Uri? GetUri(IReadOnlyDictionary<string, string?> values, string name)
    {
        return Uri.TryCreate(GetValue(values, name, string.Empty), UriKind.Absolute, out var uri)
            ? uri
            : null;
    }

    private static int GetPositiveInt(IReadOnlyDictionary<string, string?> values, string name, int defaultValue)
    {
        return int.TryParse(GetValue(values, name, string.Empty), out var parsed) && parsed > 0
            ? parsed
            : defaultValue;
    }

    private static string GetValue(IReadOnlyDictionary<string, string?> values, string name, string defaultValue)
    {
        return values.TryGetValue(name, out var value) && value is not null
            ? value.Trim()
            : defaultValue;
    }
}
