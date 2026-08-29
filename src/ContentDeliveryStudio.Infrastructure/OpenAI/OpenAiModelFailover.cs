using System.Net;
using System.Text.Json;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

public sealed record OpenAiModelAvailabilityResult(
    string Model,
    bool IsAvailable,
    HttpStatusCode? StatusCode,
    string Detail);

public interface IOpenAiModelAvailabilityProbe
{
    Task<OpenAiModelAvailabilityResult> ProbeAsync(
        OpenAiProviderOptions options,
        string model,
        CancellationToken cancellationToken);
}

/// <summary>
/// Performs a non-generating model-catalog probe against the configured
/// OpenAI-compatible gateway. It is intentionally separate from provider
/// calls so a model switch does not create an extra completion or image.
/// </summary>
public sealed class OpenAiModelAvailabilityProbe : IOpenAiModelAvailabilityProbe
{
    public static readonly TimeSpan AvailableCacheDuration = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan UnavailableCacheDuration = TimeSpan.FromSeconds(5);

    private readonly HttpClient _httpClient;
    private readonly IOpenAiSecretStore _secretStore;
    private readonly TimeSpan _availableCacheDuration;
    private readonly TimeSpan _unavailableCacheDuration;
    private readonly TimeProvider _timeProvider;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<Task<OpenAiModelAvailabilityResult>>> _inFlight = new(StringComparer.Ordinal);

    public OpenAiModelAvailabilityProbe(
        HttpClient httpClient,
        IOpenAiSecretStore secretStore,
        TimeSpan? availableCacheDuration = null,
        TimeSpan? unavailableCacheDuration = null,
        TimeProvider? timeProvider = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _secretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));
        _availableCacheDuration = ValidateDuration(availableCacheDuration ?? AvailableCacheDuration, nameof(availableCacheDuration));
        _unavailableCacheDuration = ValidateDuration(unavailableCacheDuration ?? UnavailableCacheDuration, nameof(unavailableCacheDuration));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<OpenAiModelAvailabilityResult> ProbeAsync(
        OpenAiProviderOptions options,
        string model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(model))
        {
            return new OpenAiModelAvailabilityResult(model, false, null, "Model name is empty.");
        }

        var cacheKey = CreateCacheKey(options, model);
        var now = _timeProvider.GetUtcNow();
        if (_cache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > now)
        {
            return cached.Result;
        }

        var pending = new Lazy<Task<OpenAiModelAvailabilityResult>>(
            () => ProbeUncachedAsync(options, model, CancellationToken.None),
            LazyThreadSafetyMode.ExecutionAndPublication);
        var shared = _inFlight.GetOrAdd(cacheKey, pending);
        return await AwaitAndCacheAsync(cacheKey, shared, cancellationToken);
    }

    private async Task<OpenAiModelAvailabilityResult> ProbeUncachedAsync(
        OpenAiProviderOptions options,
        string model,
        CancellationToken cancellationToken)
    {
        try
        {
            var credentials = await ProviderRequestAuthentication.ResolveAsync(
                _secretStore,
                options.ApiKeySecretName,
                options.AppIdSecretName,
                options.AppSecretSecretName,
                cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Get, BuildModelsUri(options.BaseUri));
            ProviderRequestAuthentication.Apply(request, credentials);
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new OpenAiModelAvailabilityResult(
                    model,
                    false,
                    response.StatusCode,
                    $"HTTP {(int)response.StatusCode}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var available = EnumerateModelIds(document.RootElement)
                .Any(candidate => string.Equals(candidate, model, StringComparison.OrdinalIgnoreCase));
            return new OpenAiModelAvailabilityResult(
                model,
                available,
                response.StatusCode,
                available ? "Model is present in the gateway catalog." : "Model is absent from the gateway catalog.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new OpenAiModelAvailabilityResult(model, false, null, exception.GetType().Name);
        }
    }

    private async Task<OpenAiModelAvailabilityResult> AwaitAndCacheAsync(
        string cacheKey,
        Lazy<Task<OpenAiModelAvailabilityResult>> pending,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await pending.Value.WaitAsync(cancellationToken);
            var duration = result.IsAvailable ? _availableCacheDuration : _unavailableCacheDuration;
            _cache[cacheKey] = new CacheEntry(result, _timeProvider.GetUtcNow().Add(duration));
            return result;
        }
        finally
        {
            if (pending.IsValueCreated)
            {
                _ = pending.Value.ContinueWith(
                    completed =>
                    {
                        _ = completed.Exception;
                        _inFlight.TryRemove(
                            new KeyValuePair<string, Lazy<Task<OpenAiModelAvailabilityResult>>>(cacheKey, pending));
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
        }
    }

    private static string CreateCacheKey(OpenAiProviderOptions options, string model) =>
        $"{options.BaseUri.AbsoluteUri}|{options.ApiKeySecretName}|{model.Trim().ToLowerInvariant()}";

    private static TimeSpan ValidateDuration(TimeSpan duration, string parameterName)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Cache duration must be positive.");
        }

        return duration;
    }

    private sealed record CacheEntry(OpenAiModelAvailabilityResult Result, DateTimeOffset ExpiresAt);

    private static IEnumerable<string> EnumerateModelIds(JsonElement root)
    {
        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in data.EnumerateArray())
            {
                if (item.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                {
                    yield return id.GetString()!;
                }
            }
        }

        if (root.TryGetProperty("models", out var models) && models.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in models.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    yield return item.GetString()!;
                }
                else if (item.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                {
                    yield return id.GetString()!;
                }
            }
        }
    }

    private static Uri BuildModelsUri(Uri baseUri)
    {
        var value = baseUri.ToString().TrimEnd('/');
        return value.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
            ? new Uri(value + "/models")
            : new Uri(value + "/v1/models");
    }
}

internal static class OpenAiModelFailoverPolicy
{
    public static IReadOnlyList<OpenAiTaskModelRoute> GetFallbackRoutes(OpenAiTaskModelRoute preferredRoute)
    {
        ArgumentNullException.ThrowIfNull(preferredRoute);

        if (!TextProviderModelPresets.TryGetFamily(preferredRoute.Model, out var currentFamily))
        {
            return [];
        }

        return TextProviderModelPresets.PreferredModelFamilies
            .Where(family => !string.Equals(family, currentFamily, StringComparison.Ordinal))
            .Select(family => TextProviderModelPresets.TryResolveForFamily(
                family,
                preferredRoute.QualityTier,
                out var preset,
                out var model,
                out var reasoningEffort)
                ? new OpenAiTaskModelRoute(
                    preset,
                    model,
                    reasoningEffort,
                    $"model-failover-{preferredRoute.QualityTier.ToString().ToLowerInvariant()}-from-{currentFamily}-to-{family}",
                    preferredRoute.QualityTier,
                    TextProviderModelPresetSets.TryGetPresetSetForFamily(family, out var presetSet)
                        ? presetSet
                        : string.Empty)
                : null)
            .Where(route => route is not null)
            .Select(route => route!)
            .ToArray();
    }

    public static async Task<TResult> ExecuteAsync<TResult>(
        OpenAiProviderOptions options,
        OpenAiTaskModelRoute preferredRoute,
        IOpenAiModelAvailabilityProbe? availabilityProbe,
        Func<OpenAiTaskModelRoute, Task<TResult>> operation,
        CancellationToken cancellationToken,
        IOpenAiExecutionSlotScheduler? executionSlotScheduler = null,
        IOpenAiActivePresetSetState? activePresetSetState = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(preferredRoute);
        ArgumentNullException.ThrowIfNull(operation);

        async Task<TResult> ExecuteCoreAsync()
        {
            var activePresetSetSnapshot = activePresetSetState?.GetSnapshot(options)
                ?? new OpenAiActivePresetSetSnapshot(null, Version: 0);
            var initialRoute = ResolveInitialRoute(options, preferredRoute, activePresetSetSnapshot);
            if (options.TextRoutingMode is OpenAiTextRoutingMode.Fixed
                || availabilityProbe is null)
            {
                return await operation(initialRoute);
            }

            var routes = new[] { initialRoute }
                .Concat(GetFallbackRoutes(initialRoute))
                .ToArray();
            Exception? lastException = null;
            for (var index = 0; index < routes.Length; index++)
            {
                var route = routes[index];
                if (index > 0)
                {
                    var availability = await availabilityProbe.ProbeAsync(
                        options,
                        route.Model,
                        cancellationToken);
                    if (!availability.IsAvailable)
                    {
                        continue;
                    }
                }

                try
                {
                    var result = await operation(route);
                    SwitchAfterSuccessfulFallback(
                        options,
                        initialRoute,
                        route,
                        activePresetSetSnapshot,
                        activePresetSetState);
                    return result;
                }
                catch (Exception exception) when (
                    index < routes.Length - 1
                    && IsEligible(exception, cancellationToken))
                {
                    lastException = exception;
                }
            }

            throw lastException
                ?? new InvalidOperationException(
                    $"No available fallback model was found for '{preferredRoute.Model}'.");
        }

        return executionSlotScheduler is null
            ? await ExecuteCoreAsync()
            : await executionSlotScheduler.ExecuteAsync(
                preferredRoute.QualityTier,
                ExecuteCoreAsync,
                cancellationToken);
    }

    private static OpenAiTaskModelRoute ResolveInitialRoute(
        OpenAiProviderOptions options,
        OpenAiTaskModelRoute preferredRoute,
        OpenAiActivePresetSetSnapshot activePresetSetSnapshot)
    {
        if (string.IsNullOrWhiteSpace(preferredRoute.PresetSet))
        {
            return preferredRoute;
        }

        var activePresetSet = activePresetSetSnapshot.PresetSet ?? options.InitialPresetSet;
        return activePresetSet is not null
            && TextProviderModelPresets.TryResolveForPresetSet(
                activePresetSet,
                preferredRoute.QualityTier,
                out var preset,
                out var model,
                out var reasoningEffort)
            ? new OpenAiTaskModelRoute(
                preset,
                model,
                reasoningEffort,
                preferredRoute.Reason,
                preferredRoute.QualityTier,
                activePresetSet)
            : preferredRoute;
    }

    private static void SwitchAfterSuccessfulFallback(
        OpenAiProviderOptions options,
        OpenAiTaskModelRoute initialRoute,
        OpenAiTaskModelRoute route,
        OpenAiActivePresetSetSnapshot activePresetSetSnapshot,
        IOpenAiActivePresetSetState? activePresetSetState)
    {
        if (activePresetSetState is not null
            && !string.Equals(route.PresetSet, initialRoute.PresetSet, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(route.PresetSet))
        {
            _ = activePresetSetState.TrySwitchActivePresetSet(
                options,
                activePresetSetSnapshot.Version,
                route.PresetSet);
        }
    }

    private static bool IsEligible(Exception exception, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        return exception switch
        {
            OperationCanceledException => true,
            TimeoutException => true,
            HttpRequestException httpRequestException => IsHttpFailureEligible(httpRequestException),
            _ => false,
        };
    }

    private static bool IsHttpFailureEligible(HttpRequestException exception)
    {
        var statusCode = exception.StatusCode;
        if (statusCode is null)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                exception.Message,
                "status\\s+(?<status>\\d{3})",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            if (match.Success && int.TryParse(match.Groups["status"].Value, out var parsed))
            {
                statusCode = (HttpStatusCode)parsed;
            }
        }

        if (statusCode is null)
        {
            return true;
        }

        var status = (int)statusCode.Value;
        return statusCode is HttpStatusCode.NotFound
            or HttpStatusCode.RequestTimeout
            or (HttpStatusCode)429
            || status >= 500;
    }
}
