namespace ContentDeliveryStudio.Infrastructure.OpenAI;

public enum ProviderImageGenerationSurface
{
    Images = 0,
    Responses = 1,
}

public static class TextProviderRoutingModes
{
    public const string Fixed = "fixed";
    public const string Auto = "auto";

    public static IReadOnlyList<string> Names { get; } = [Fixed, Auto];
}

public static class TextProviderRecoveryModes
{
    public const string Disabled = "disabled";
    public const string PreferSol = "prefer-sol";

    public static IReadOnlyList<string> Names { get; } = [Disabled, PreferSol];
}

public static class TextProviderModelPresets
{
    public const string SolFamily = "sol";
    public const string TerraFamily = "terra";
    public const string LunaFamily = "luna";

    public const string SolXHigh = "sol-xhigh";
    public const string SolMedium = "sol-medium";
    public const string SolLow = "sol-low";
    public const string TerraXHigh = "terra-xhigh";
    public const string TerraHigh = "terra-high";
    public const string TerraMedium = "terra-medium";
    public const string LunaXHigh = "luna-xhigh";
    public const string LunaHigh = "luna-high";
    public const string LunaMedium = "luna-medium";

    public static IReadOnlyList<string> SetNames { get; } =
        [TextProviderModelPresetSets.SolOnly, TextProviderModelPresetSets.TerraOnly, TextProviderModelPresetSets.LunaOnly];

    public static IReadOnlyList<string> PreferredModelFamilies { get; } =
        [SolFamily, TerraFamily, LunaFamily];

    public static IReadOnlyList<string> Names { get; } =
        [
            SolXHigh,
            SolMedium,
            SolLow,
            TerraXHigh,
            TerraHigh,
            TerraMedium,
            LunaXHigh,
            LunaHigh,
            LunaMedium,
        ];

    public static bool TryResolve(string? name, out string model, out string reasoningEffort)
    {
        switch (name?.Trim().ToLowerInvariant())
        {
            case SolXHigh:
                model = "gpt-5.6-sol";
                reasoningEffort = "xhigh";
                return true;
            case SolMedium:
                model = "gpt-5.6-sol";
                reasoningEffort = "medium";
                return true;
            case SolLow:
                model = "gpt-5.6-sol";
                reasoningEffort = "low";
                return true;
            case TerraXHigh:
                model = "gpt-5.6-terra";
                reasoningEffort = "xhigh";
                return true;
            case TerraHigh:
                model = "gpt-5.6-terra";
                reasoningEffort = "high";
                return true;
            case TerraMedium:
                model = "gpt-5.6-terra";
                reasoningEffort = "medium";
                return true;
            case LunaXHigh:
                model = "gpt-5.6-luna";
                reasoningEffort = "xhigh";
                return true;
            case LunaHigh:
                model = "gpt-5.6-luna";
                reasoningEffort = "high";
                return true;
            case LunaMedium:
                model = "gpt-5.6-luna";
                reasoningEffort = "medium";
                return true;
            default:
                model = string.Empty;
                reasoningEffort = string.Empty;
                return false;
        }
    }

    public static bool TryResolveForPresetSet(
        string presetSet,
        OpenAiExecutionQualityTier qualityTier,
        out string preset,
        out string model,
        out string reasoningEffort)
    {
        preset = string.Empty;
        model = string.Empty;
        reasoningEffort = string.Empty;
        return TextProviderModelPresetSets.TryGetFamily(presetSet, out var family)
            && TryResolveForFamily(family, qualityTier, out preset, out model, out reasoningEffort);
    }

    public static bool TryGetPresetSet(string? preset, out string presetSet)
    {
        presetSet = preset?.Trim().ToLowerInvariant() switch
        {
            SolXHigh or SolMedium or SolLow => TextProviderModelPresetSets.SolOnly,
            TerraXHigh or TerraHigh or TerraMedium => TextProviderModelPresetSets.TerraOnly,
            LunaXHigh or LunaHigh or LunaMedium => TextProviderModelPresetSets.LunaOnly,
            _ => string.Empty,
        };

        return presetSet.Length > 0;
    }

    public static bool TryGetFamily(string model, out string family)
    {
        family = model?.Trim().ToLowerInvariant() switch
        {
            "gpt-5.6-sol" => SolFamily,
            "gpt-5.6-terra" => TerraFamily,
            "gpt-5.6-luna" => LunaFamily,
            _ => string.Empty,
        };

        return family.Length > 0;
    }

    public static bool TryResolveForFamily(
        string family,
        OpenAiExecutionQualityTier qualityTier,
        out string preset,
        out string model,
        out string resolvedReasoningEffort)
    {
        var normalizedFamily = family?.Trim().ToLowerInvariant();
        preset = normalizedFamily switch
        {
            SolFamily => qualityTier switch
            {
                OpenAiExecutionQualityTier.Deep => SolXHigh,
                OpenAiExecutionQualityTier.Balanced => SolMedium,
                OpenAiExecutionQualityTier.Fast => SolLow,
                _ => string.Empty,
            },
            TerraFamily => qualityTier switch
            {
                OpenAiExecutionQualityTier.Deep => TerraXHigh,
                OpenAiExecutionQualityTier.Balanced => TerraHigh,
                OpenAiExecutionQualityTier.Fast => TerraMedium,
                _ => string.Empty,
            },
            LunaFamily => qualityTier switch
            {
                OpenAiExecutionQualityTier.Deep => LunaXHigh,
                OpenAiExecutionQualityTier.Balanced => LunaHigh,
                OpenAiExecutionQualityTier.Fast => LunaMedium,
                _ => string.Empty,
            },
            _ => string.Empty,
        };

        if (preset.Length == 0 || !TryResolve(preset, out model, out resolvedReasoningEffort))
        {
            model = string.Empty;
            resolvedReasoningEffort = string.Empty;
            return false;
        }

        return true;
    }

    public static bool TryResolveForFamily(
        string family,
        string reasoningEffort,
        out string preset,
        out string model,
        out string resolvedReasoningEffort)
    {
        if (!TryGetQualityTierForModel(family, reasoningEffort, out var qualityTier))
        {
            preset = string.Empty;
            model = string.Empty;
            resolvedReasoningEffort = string.Empty;
            return false;
        }

        return TryResolveForFamily(family, qualityTier, out preset, out model, out resolvedReasoningEffort);
    }

    public static bool TryGetQualityTier(string preset, out OpenAiExecutionQualityTier qualityTier)
    {
        qualityTier = preset?.Trim().ToLowerInvariant() switch
        {
            SolXHigh or TerraXHigh or LunaXHigh => OpenAiExecutionQualityTier.Deep,
            SolMedium or TerraHigh or LunaHigh => OpenAiExecutionQualityTier.Balanced,
            SolLow or TerraMedium or LunaMedium => OpenAiExecutionQualityTier.Fast,
            _ => default,
        };

        return Names.Contains(preset?.Trim().ToLowerInvariant() ?? string.Empty, StringComparer.Ordinal);
    }

    public static bool TryGetQualityTierForModel(
        string model,
        string reasoningEffort,
        out OpenAiExecutionQualityTier qualityTier)
    {
        qualityTier = default;
        if (!TryGetFamily(model, out var family))
        {
            return false;
        }

        var normalizedEffort = reasoningEffort?.Trim().ToLowerInvariant();
        qualityTier = family switch
        {
            SolFamily when normalizedEffort is "xhigh" => OpenAiExecutionQualityTier.Deep,
            SolFamily when normalizedEffort is "medium" => OpenAiExecutionQualityTier.Balanced,
            SolFamily when normalizedEffort is "low" => OpenAiExecutionQualityTier.Fast,
            TerraFamily or LunaFamily when normalizedEffort is "xhigh" => OpenAiExecutionQualityTier.Deep,
            TerraFamily or LunaFamily when normalizedEffort is "high" => OpenAiExecutionQualityTier.Balanced,
            TerraFamily or LunaFamily when normalizedEffort is "medium" => OpenAiExecutionQualityTier.Fast,
            _ => default,
        };

        return family switch
        {
            SolFamily => normalizedEffort is "xhigh" or "medium" or "low",
            TerraFamily or LunaFamily => normalizedEffort is "xhigh" or "high" or "medium",
            _ => false,
        };
    }
}

public static class TextProviderModelPresetSets
{
    public const string SolOnly = "sol-only";
    public const string TerraOnly = "terra-only";
    public const string LunaOnly = "luna-only";

    public static IReadOnlyList<string> Names { get; } = [SolOnly, TerraOnly, LunaOnly];

    public static bool TryGetFamily(string? presetSet, out string family)
    {
        family = presetSet?.Trim().ToLowerInvariant() switch
        {
            SolOnly => TextProviderModelPresets.SolFamily,
            TerraOnly => TextProviderModelPresets.TerraFamily,
            LunaOnly => TextProviderModelPresets.LunaFamily,
            _ => string.Empty,
        };

        return family.Length > 0;
    }

    public static bool TryGetPresetSetForFamily(string? family, out string presetSet)
    {
        presetSet = family?.Trim().ToLowerInvariant() switch
        {
            TextProviderModelPresets.SolFamily => SolOnly,
            TextProviderModelPresets.TerraFamily => TerraOnly,
            TextProviderModelPresets.LunaFamily => LunaOnly,
            _ => string.Empty,
        };

        return presetSet.Length > 0;
    }

    public static bool TryParseQualityTier(string? value, out OpenAiExecutionQualityTier qualityTier)
    {
        qualityTier = value?.Trim().ToLowerInvariant() switch
        {
            "deep" => OpenAiExecutionQualityTier.Deep,
            "balanced" => OpenAiExecutionQualityTier.Balanced,
            "fast" => OpenAiExecutionQualityTier.Fast,
            _ => default,
        };

        return value?.Trim().ToLowerInvariant() is "deep" or "balanced" or "fast";
    }

    public static string GetQualityTierName(OpenAiExecutionQualityTier qualityTier) => qualityTier switch
    {
        OpenAiExecutionQualityTier.Deep => "deep",
        OpenAiExecutionQualityTier.Balanced => "balanced",
        OpenAiExecutionQualityTier.Fast => "fast",
        _ => throw new ArgumentOutOfRangeException(nameof(qualityTier), qualityTier, "Unknown quality tier."),
    };
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
    ProviderImageGenerationSurface ImageGenerationSurface,
    string ReasoningEffort = "medium",
    string? ModelPreset = null,
    string RoutingMode = TextProviderRoutingModes.Fixed,
    string? ModelPresetSet = null,
    string? QualityTier = null,
    string? ConfiguredModel = null,
    string? ConfiguredReasoningEffort = null,
    string RecoveryMode = TextProviderRecoveryModes.Disabled)
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
        var configuredPreset = GetPresentValue(values, $"{prefix}_PRESET");
        var configuredPresetSet = GetPresentValue(values, $"{prefix}_PRESET_SET");
        var qualityTier = GetPresentValue(values, $"{prefix}_QUALITY_TIER");
        if (configuredPresetSet is null
            && TextProviderModelPresetSets.TryGetFamily(configuredPreset, out _))
        {
            configuredPresetSet = configuredPreset;
            configuredPreset = null;
        }

        var hasResolvedPair = TextProviderModelPresets.TryResolve(
            configuredPreset,
            out var pairModel,
            out var pairReasoningEffort);
        var presetFamily = string.Empty;
        var parsedQualityTier = default(OpenAiExecutionQualityTier);
        var setPreset = string.Empty;
        var setModel = string.Empty;
        var setReasoningEffort = string.Empty;
        var hasResolvedSet = TextProviderModelPresetSets.TryGetFamily(configuredPresetSet, out presetFamily)
            && TextProviderModelPresetSets.TryParseQualityTier(qualityTier, out parsedQualityTier)
            && TextProviderModelPresets.TryResolveForPresetSet(
                configuredPresetSet!,
                parsedQualityTier,
                out setPreset,
                out setModel,
                out setReasoningEffort);
        // Keep an unknown legacy value visible to Validate() so startup fails
        // closed instead of silently accepting a typo as an unpinned model.
        var effectivePreset = hasResolvedPair
            ? configuredPreset
            : hasResolvedSet
                ? setPreset
                : configuredPreset;
        var effectiveModel = hasResolvedPair
            ? pairModel
            : hasResolvedSet
                ? setModel
                : GetValue(values, $"{prefix}_MODEL", string.Empty);
        var effectiveReasoningEffort = hasResolvedPair
            ? pairReasoningEffort
            : hasResolvedSet
                ? setReasoningEffort
                : GetValue(values, $"{prefix}_REASONING_EFFORT", "medium");
        var routingMode = GetValue(values, $"{prefix}_ROUTING_MODE", TextProviderRoutingModes.Fixed)
            .Trim()
            .ToLowerInvariant();
        var recoveryMode = GetValue(values, $"{prefix}_RECOVERY_MODE", TextProviderRecoveryModes.Disabled)
            .Trim()
            .ToLowerInvariant();
        return new ProviderEndpointEnvironmentConfiguration(
            prefix,
            GetValue(values, $"{prefix}_KIND", "openai_compatible"),
            GetUri(values, $"{prefix}_BASE_URL"),
            effectiveModel,
            ResponsesModel: null,
            keyNames.FirstOrDefault(),
            keyNames,
            UsesSharedTextApiKeyFallback: false,
            GetPresentSecretName(values, $"{prefix}_APP_ID"),
            GetPresentSecretName(values, $"{prefix}_APP_SECRET"),
            GetPositiveInt(values, $"{prefix}_CONCURRENCY_PER_KEY", 1),
            GetPositiveInt(values, $"{prefix}_TOTAL_CONCURRENCY", Math.Max(1, keyNames.Count)),
            ProviderImageGenerationSurface.Images,
            effectiveReasoningEffort,
            effectivePreset,
            routingMode,
            configuredPresetSet,
            qualityTier,
            GetPresentValue(values, $"{prefix}_MODEL"),
            GetPresentValue(values, $"{prefix}_REASONING_EFFORT"),
            recoveryMode);
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
            GetImageGenerationSurface(values, prefix),
            GetValue(values, $"{prefix}_REASONING_EFFORT", "medium"));
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

        if (ReasoningEffort is not ("none" or "low" or "medium" or "high" or "xhigh" or "max"))
        {
            errors.Add($"{displayName} reasoning effort is invalid.");
        }

        if (Prefix.StartsWith("TEXT_PROVIDER", StringComparison.Ordinal)
            && ModelPreset is not null
            && !TextProviderModelPresets.TryResolve(ModelPreset, out _, out _))
        {
            errors.Add(
                $"{displayName} preset '{ModelPreset}' is invalid. Allowed presets: {string.Join(", ", TextProviderModelPresets.Names)}.");
        }

        if (Prefix.StartsWith("TEXT_PROVIDER", StringComparison.Ordinal)
            && ModelPresetSet is not null)
        {
            if (!TextProviderModelPresetSets.TryGetFamily(ModelPresetSet, out var presetFamily))
            {
                errors.Add(
                    $"{displayName} preset set '{ModelPresetSet}' is invalid. Allowed preset sets: {string.Join(", ", TextProviderModelPresetSets.Names)}.");
            }
            else if (string.IsNullOrWhiteSpace(QualityTier)
                || !TextProviderModelPresetSets.TryParseQualityTier(QualityTier, out var qualityTier)
                || !TextProviderModelPresets.TryResolveForPresetSet(
                    ModelPresetSet,
                    qualityTier,
                    out _,
                    out var expectedModel,
                    out var expectedReasoningEffort))
            {
                errors.Add(
                    $"{displayName} preset set '{ModelPresetSet}' requires QUALITY_TIER=deep, balanced, or fast.");
            }
            else
            {
                if (ConfiguredModel is not null
                    && TextProviderModelPresets.TryGetFamily(ConfiguredModel, out var configuredFamily)
                    && !string.Equals(configuredFamily, presetFamily, StringComparison.Ordinal))
                {
                    errors.Add(
                        $"{displayName} mixes preset set '{ModelPresetSet}' with model '{ConfiguredModel}'. A preset set must be {presetFamily}-only.");
                }

                if (ConfiguredReasoningEffort is not null
                    && !string.Equals(
                        ConfiguredReasoningEffort.Trim(),
                        expectedReasoningEffort,
                        StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(
                        $"{displayName} quality tier '{QualityTier}' requires reasoning effort '{expectedReasoningEffort}'.");
                }

                if (ModelPreset is not null
                    && !string.Equals(ModelPreset, TextProviderModelPresets.TryResolveForPresetSet(
                        ModelPresetSet,
                        qualityTier,
                        out var expectedPreset,
                        out expectedModel,
                        out expectedReasoningEffort)
                        ? expectedPreset
                        : ModelPreset,
                        StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(
                        $"{displayName} preset '{ModelPreset}' does not match preset set '{ModelPresetSet}' and quality tier '{QualityTier}'.");
                }
            }
        }

        if (Prefix.StartsWith("TEXT_PROVIDER", StringComparison.Ordinal)
            && !TextProviderRoutingModes.Names.Contains(RoutingMode, StringComparer.Ordinal))
        {
            errors.Add(
                $"{displayName} routing mode '{RoutingMode}' is invalid. Allowed modes: {string.Join(", ", TextProviderRoutingModes.Names)}.");
        }

        if (Prefix.StartsWith("TEXT_PROVIDER", StringComparison.Ordinal)
            && !TextProviderRecoveryModes.Names.Contains(RecoveryMode, StringComparer.Ordinal))
        {
            errors.Add(
                $"{displayName} recovery mode '{RecoveryMode}' is invalid. Allowed modes: {string.Join(", ", TextProviderRecoveryModes.Names)}.");
        }
        else if (Prefix.StartsWith("TEXT_PROVIDER", StringComparison.Ordinal)
            && string.Equals(RecoveryMode, TextProviderRecoveryModes.PreferSol, StringComparison.Ordinal)
            && !string.Equals(RoutingMode, TextProviderRoutingModes.Auto, StringComparison.Ordinal))
        {
            errors.Add(
                $"{displayName} recovery mode '{TextProviderRecoveryModes.PreferSol}' requires routing mode '{TextProviderRoutingModes.Auto}'.");
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
