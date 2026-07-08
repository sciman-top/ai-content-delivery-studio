namespace ContentDeliveryStudio.Infrastructure.OpenAI;

public enum ProviderImageGenerationSurface
{
    Images = 0,
    Responses = 1,
}

public sealed record ProviderEnvironmentConfiguration(
    ProviderEndpointEnvironmentConfiguration Text,
    ProviderEndpointEnvironmentConfiguration Image,
    IReadOnlyList<ProviderEndpointEnvironmentConfiguration> TextFallbacks,
    IReadOnlyList<ProviderEndpointEnvironmentConfiguration> ImageFallbacks)
{
    public ProviderEnvironmentConfiguration(
        ProviderEndpointEnvironmentConfiguration text,
        ProviderEndpointEnvironmentConfiguration image)
        : this(text, image, [], [])
    {
    }

    public static ProviderEnvironmentConfiguration FromValues(IReadOnlyDictionary<string, string?> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return new ProviderEnvironmentConfiguration(
            ProviderEndpointEnvironmentConfiguration.CreateText(values),
            ProviderEndpointEnvironmentConfiguration.CreateImage(values),
            ProviderEndpointEnvironmentConfiguration.CreateTextFallbacks(values),
            ProviderEndpointEnvironmentConfiguration.CreateImageFallbacks(values));
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
            .Concat(TextFallbacks.SelectMany((fallback, index) => fallback.Validate($"Text provider fallback {index + 1}")))
            .Concat(ImageFallbacks.SelectMany((fallback, index) => fallback.Validate($"Image provider fallback {index + 1}")))
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
    string? ResponsesModel,
    string? ApiKeySecretName,
    IReadOnlyList<string> ApiKeySecretNames,
    bool UsesSharedTextApiKeyFallback,
    string? AppIdSecretName,
    string? AppSecretSecretName,
    int ConcurrencyPerKey,
    int TotalConcurrency,
    ProviderImageGenerationSurface ImageGenerationSurface)
{
    public static ProviderEndpointEnvironmentConfiguration CreateText(IReadOnlyDictionary<string, string?> values)
        => CreateText(values, "TEXT_PROVIDER");

    public static IReadOnlyList<ProviderEndpointEnvironmentConfiguration> CreateTextFallbacks(
        IReadOnlyDictionary<string, string?> values)
    {
        return GetFallbackIndexes(values, "TEXT_PROVIDER_FALLBACK")
            .Select(index => CreateText(values, $"TEXT_PROVIDER_FALLBACK_{index}"))
            .ToArray();
    }

    private static ProviderEndpointEnvironmentConfiguration CreateText(
        IReadOnlyDictionary<string, string?> values,
        string prefix)
    {
        var keyNames = GetPresentSecretNames(values, $"{prefix}_API_KEY");
        return new ProviderEndpointEnvironmentConfiguration(
            prefix,
            GetValue(values, $"{prefix}_KIND", "openai_compatible"),
            GetUri(values, $"{prefix}_BASE_URL"),
            GetValue(values, $"{prefix}_MODEL", string.Empty),
            ResponsesModel: null,
            keyNames.FirstOrDefault(),
            keyNames,
            UsesSharedTextApiKeyFallback: false,
            GetPresentSecretName(values, $"{prefix}_APP_ID"),
            GetPresentSecretName(values, $"{prefix}_APP_SECRET"),
            GetPositiveInt(values, $"{prefix}_CONCURRENCY_PER_KEY", 1),
            GetPositiveInt(values, $"{prefix}_TOTAL_CONCURRENCY", Math.Max(1, keyNames.Count)),
            ProviderImageGenerationSurface.Images);
    }

    public static ProviderEndpointEnvironmentConfiguration CreateImage(IReadOnlyDictionary<string, string?> values)
        => CreateImage(values, "IMAGE_PROVIDER", allowSharedTextFallback: true);

    public static IReadOnlyList<ProviderEndpointEnvironmentConfiguration> CreateImageFallbacks(
        IReadOnlyDictionary<string, string?> values)
    {
        return GetFallbackIndexes(values, "IMAGE_PROVIDER_FALLBACK")
            .Select(index => CreateImage(values, $"IMAGE_PROVIDER_FALLBACK_{index}", allowSharedTextFallback: false))
            .ToArray();
    }

    private static ProviderEndpointEnvironmentConfiguration CreateImage(
        IReadOnlyDictionary<string, string?> values,
        string prefix,
        bool allowSharedTextFallback)
    {
        var keyNames = GetNumberedSecretNames(values, $"{prefix}_API_KEY");
        if (keyNames.Count == 0)
        {
            keyNames = GetPresentSecretNames(values, $"{prefix}_API_KEY");
        }

        var usesSharedTextApiKeyFallback = false;
        if (allowSharedTextFallback && keyNames.Count == 0)
        {
            keyNames = GetPresentSecretNames(values, "TEXT_PROVIDER_API_KEY");
            usesSharedTextApiKeyFallback = keyNames.Count > 0;
        }

        var concurrencyPerKey = GetPositiveInt(values, $"{prefix}_CONCURRENCY_PER_KEY", 1);
        return new ProviderEndpointEnvironmentConfiguration(
            prefix,
            GetValue(values, $"{prefix}_KIND", "openai_compatible_image_only"),
            GetUri(values, $"{prefix}_BASE_URL"),
            GetValue(values, $"{prefix}_MODEL", string.Empty),
            GetPresentValue(values, $"{prefix}_RESPONSES_MODEL"),
            keyNames.FirstOrDefault(),
            keyNames,
            usesSharedTextApiKeyFallback,
            GetPresentSecretName(values, $"{prefix}_APP_ID"),
            GetPresentSecretName(values, $"{prefix}_APP_SECRET"),
            concurrencyPerKey,
            GetPositiveInt(values, $"{prefix}_TOTAL_CONCURRENCY", keyNames.Count * concurrencyPerKey),
            GetImageGenerationSurface(values, prefix));
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

        if (Prefix.StartsWith("IMAGE_PROVIDER", StringComparison.Ordinal)
            && ImageGenerationSurface is ProviderImageGenerationSurface.Responses
            && string.IsNullOrWhiteSpace(ResponsesModel))
        {
            errors.Add($"{displayName} responses image model is required when image surface is responses.");
        }

        return errors;
    }

    private static IReadOnlyList<int> GetFallbackIndexes(IReadOnlyDictionary<string, string?> values, string prefix)
    {
        var indexedPrefix = prefix + "_";
        return values.Keys
            .Where(key => key.StartsWith(indexedPrefix, StringComparison.Ordinal))
            .Select(key => key[indexedPrefix.Length..])
            .Select(suffix =>
            {
                var separatorIndex = suffix.IndexOf('_');
                return separatorIndex > 0 && int.TryParse(suffix[..separatorIndex], out var index)
                    ? index
                    : 0;
            })
            .Where(index => index > 0)
            .Distinct()
            .Order()
            .ToArray();
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

    private static string? GetPresentValue(IReadOnlyDictionary<string, string?> values, string name)
    {
        var value = GetValue(values, name, string.Empty);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static ProviderImageGenerationSurface GetImageGenerationSurface(
        IReadOnlyDictionary<string, string?> values,
        string prefix)
    {
        return GetValue(values, $"{prefix}_IMAGE_SURFACE", string.Empty).Trim().ToLowerInvariant() switch
        {
            "responses" or "response" => ProviderImageGenerationSurface.Responses,
            _ => ProviderImageGenerationSurface.Images,
        };
    }
}
