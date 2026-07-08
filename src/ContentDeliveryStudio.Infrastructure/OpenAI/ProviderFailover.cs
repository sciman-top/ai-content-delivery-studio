using System.Net;
using System.Text.RegularExpressions;
using ContentDeliveryStudio.Core.Documents;
using ContentDeliveryStudio.Core.Providers;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

public sealed class FailoverTextPlanningProvider : ITextPlanningProvider
{
    private readonly IReadOnlyList<ITextPlanningProvider> _providers;

    public FailoverTextPlanningProvider(IReadOnlyList<ITextPlanningProvider> providers)
    {
        _providers = ProviderFailoverPolicy.ValidateProviders(providers, nameof(providers));
        Capabilities = ProviderFailoverPolicy.CreateCapabilities(
            "failover-text",
            "Failover Text Planning Provider",
            _providers.Select(provider => provider.Capabilities),
            supportsTextPlanning: true,
            supportsImageGeneration: false,
            supportsVisionReview: false);
    }

    public IProviderCapabilities Capabilities { get; }

    public Task<SeriesPlanResult> CreatePlanAsync(PlanningRequest request, CancellationToken cancellationToken)
        => ExecuteWithFailoverAsync(provider => provider.CreatePlanAsync(request, cancellationToken), cancellationToken);

    public Task<BriefPlanningResult> CreatePromptDirectionsAsync(
        BriefPlanningRequest request,
        CancellationToken cancellationToken)
        => ExecuteWithFailoverAsync(
            provider => provider.CreatePromptDirectionsAsync(request, cancellationToken),
            cancellationToken);

    public Task<BlueprintPlanningResult> CreateDesignBlueprintsAsync(
        BlueprintPlanningRequest request,
        CancellationToken cancellationToken)
        => ExecuteWithFailoverAsync(
            provider => provider.CreateDesignBlueprintsAsync(request, cancellationToken),
            cancellationToken);

    public Task<DocumentIllustrationPlanningResult> CreateDocumentIllustrationPlanAsync(
        DocumentIllustrationPlanningRequest request,
        CancellationToken cancellationToken)
        => ExecuteWithFailoverAsync(
            provider => provider.CreateDocumentIllustrationPlanAsync(request, cancellationToken),
            cancellationToken);

    private async Task<T> ExecuteWithFailoverAsync<T>(
        Func<ITextPlanningProvider, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < _providers.Count; index++)
        {
            try
            {
                return await operation(_providers[index]);
            }
            catch (Exception exception) when (ShouldTryNextProvider(exception, cancellationToken, index))
            {
            }
        }

        throw new InvalidOperationException("Failover provider list was exhausted unexpectedly.");
    }

    private bool ShouldTryNextProvider(Exception exception, CancellationToken cancellationToken, int index)
    {
        return index < _providers.Count - 1
            && ProviderFailoverPolicy.IsFailoverEligible(exception, cancellationToken);
    }
}

public sealed class FailoverImageGenerationProvider : IImageGenerationProvider
{
    private readonly IReadOnlyList<IImageGenerationProvider> _providers;

    public FailoverImageGenerationProvider(IReadOnlyList<IImageGenerationProvider> providers)
    {
        _providers = ProviderFailoverPolicy.ValidateProviders(providers, nameof(providers));
        Capabilities = ProviderFailoverPolicy.CreateCapabilities(
            "failover-image",
            "Failover Image Generation Provider",
            _providers.Select(provider => provider.Capabilities),
            supportsTextPlanning: false,
            supportsImageGeneration: true,
            supportsVisionReview: false);
    }

    public IProviderCapabilities Capabilities { get; }

    public async Task<ImageGenerationResult> GenerateImageAsync(
        ImageGenerationRequest request,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < _providers.Count; index++)
        {
            try
            {
                return await _providers[index].GenerateImageAsync(request, cancellationToken);
            }
            catch (Exception exception) when (ShouldTryNextProvider(exception, cancellationToken, index))
            {
            }
        }

        throw new InvalidOperationException("Failover provider list was exhausted unexpectedly.");
    }

    private bool ShouldTryNextProvider(Exception exception, CancellationToken cancellationToken, int index)
    {
        return index < _providers.Count - 1
            && ProviderFailoverPolicy.IsFailoverEligible(exception, cancellationToken);
    }
}

public sealed class FailoverVisionReviewProvider : IVisionReviewProvider
{
    private readonly IReadOnlyList<IVisionReviewProvider> _providers;

    public FailoverVisionReviewProvider(IReadOnlyList<IVisionReviewProvider> providers)
    {
        _providers = ProviderFailoverPolicy.ValidateProviders(providers, nameof(providers));
        Capabilities = ProviderFailoverPolicy.CreateCapabilities(
            "failover-vision",
            "Failover Vision Review Provider",
            _providers.Select(provider => provider.Capabilities),
            supportsTextPlanning: false,
            supportsImageGeneration: false,
            supportsVisionReview: true);
    }

    public IProviderCapabilities Capabilities { get; }

    public async Task<VisionReviewResult> ReviewAsync(
        VisionReviewRequest request,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < _providers.Count; index++)
        {
            try
            {
                return await _providers[index].ReviewAsync(request, cancellationToken);
            }
            catch (Exception exception) when (ShouldTryNextProvider(exception, cancellationToken, index))
            {
            }
        }

        throw new InvalidOperationException("Failover provider list was exhausted unexpectedly.");
    }

    private bool ShouldTryNextProvider(Exception exception, CancellationToken cancellationToken, int index)
    {
        return index < _providers.Count - 1
            && ProviderFailoverPolicy.IsFailoverEligible(exception, cancellationToken);
    }
}

public static class ProviderFailoverPolicy
{
    private static readonly Regex StatusCodePattern = new(@"status\s+(?<status>\d{3})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static bool IsFailoverEligible(Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(exception);

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
        if (exception.StatusCode is { } statusCode)
        {
            return IsStatusEligible(statusCode);
        }

        var match = StatusCodePattern.Match(exception.Message);
        if (match.Success
            && int.TryParse(match.Groups["status"].Value, out var parsed))
        {
            return IsStatusEligible((HttpStatusCode)parsed);
        }

        return true;
    }

    private static bool IsStatusEligible(HttpStatusCode statusCode)
    {
        var status = (int)statusCode;
        return statusCode is HttpStatusCode.RequestTimeout
            || statusCode is (HttpStatusCode)429
            || status >= 500;
    }

    internal static IReadOnlyList<TProvider> ValidateProviders<TProvider>(
        IReadOnlyList<TProvider> providers,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(providers);
        if (providers.Count == 0)
        {
            throw new ArgumentException("At least one provider is required.", parameterName);
        }

        if (providers.Any(provider => provider is null))
        {
            throw new ArgumentException("Provider list cannot include null entries.", parameterName);
        }

        return providers;
    }

    internal static ProviderCapabilities CreateCapabilities(
        string providerId,
        string displayName,
        IEnumerable<IProviderCapabilities> capabilities,
        bool supportsTextPlanning,
        bool supportsImageGeneration,
        bool supportsVisionReview)
    {
        var capabilityList = capabilities.ToArray();
        var supportedSizes = capabilityList
            .SelectMany(capability => capability.SupportedSizes)
            .Distinct()
            .ToArray();
        var supportedQualities = capabilityList
            .SelectMany(capability => capability.SupportedQualities)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var supportedOutputFormats = capabilityList
            .SelectMany(capability => capability.SupportedOutputFormats)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var supportedBackgroundModes = capabilityList
            .SelectMany(capability => capability.SupportedBackgroundModes)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var costHints = capabilityList
            .SelectMany(capability => capability.CostHints)
            .ToArray();

        return new ProviderCapabilities(
            providerId,
            displayName,
            capabilityList.SelectMany(capability => capability.ModelIds).Distinct(StringComparer.Ordinal).ToArray(),
            supportsTextPlanning,
            supportsImageGeneration,
            supportsVisionReview,
            SupportsImageEditing: false,
            SupportsStreaming: capabilityList.Any(capability => capability.SupportsStreaming),
            supportedSizes,
            supportedQualities,
            supportedOutputFormats,
            supportedBackgroundModes,
            supportsReferenceImages: capabilityList.Any(capability => capability.SupportsReferenceImages),
            costHints);
    }
}
