using System.Net;
using System.Net.Http.Json;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

public sealed record OpenAiPresetRecoveryOptions(
    TimeSpan ProbeInterval,
    TimeSpan InitialFailureBackoff,
    TimeSpan MaximumFailureBackoff,
    int RequiredSuccessfulCanaries,
    TimeSpan CanaryTimeout)
{
    public static OpenAiPresetRecoveryOptions Default { get; } = new(
        ProbeInterval: TimeSpan.FromMinutes(10),
        InitialFailureBackoff: TimeSpan.FromMinutes(10),
        MaximumFailureBackoff: TimeSpan.FromMinutes(60),
        RequiredSuccessfulCanaries: 2,
        CanaryTimeout: TimeSpan.FromSeconds(45));

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (ProbeInterval <= TimeSpan.Zero)
        {
            errors.Add("Recovery probe interval must be positive.");
        }

        if (InitialFailureBackoff <= TimeSpan.Zero)
        {
            errors.Add("Recovery initial failure backoff must be positive.");
        }

        if (MaximumFailureBackoff < InitialFailureBackoff)
        {
            errors.Add("Recovery maximum failure backoff cannot be shorter than the initial failure backoff.");
        }

        if (RequiredSuccessfulCanaries <= 0)
        {
            errors.Add("Recovery requires at least one successful canary.");
        }

        if (CanaryTimeout <= TimeSpan.Zero)
        {
            errors.Add("Recovery canary timeout must be positive.");
        }

        return errors;
    }
}

public enum OpenAiPresetRecoveryDecisionKind
{
    Disabled = 0,
    AlreadyPreferred = 1,
    CoolingDown = 2,
    CandidateUnavailable = 3,
    CandidateUnhealthy = 4,
    CandidateStabilizing = 5,
    Promoted = 6,
    ConcurrentChange = 7,
    DeferredByCapacity = 8,
}

public sealed record OpenAiPresetRecoveryDecision(
    OpenAiPresetRecoveryDecisionKind Kind,
    string? ActivePresetSet,
    string? CandidatePresetSet,
    int SuccessfulCanaries,
    DateTimeOffset? NextEligibleProbeAt = null);

public sealed record OpenAiPresetRecoveryCanaryResult(
    string Model,
    string ReasoningEffort,
    bool IsSuccess,
    HttpStatusCode? StatusCode);

public sealed record OpenAiPresetRecoveryCanaryRequest(
    string Model,
    string ReasoningEffort,
    OpenAiExecutionQualityTier QualityTier);

public interface IOpenAiPresetRecoveryCanary
{
    Task<OpenAiPresetRecoveryCanaryResult> ProbeAsync(
        OpenAiProviderOptions options,
        OpenAiPresetRecoveryCanaryRequest request,
        CancellationToken cancellationToken);
}

public interface IOpenAiPresetRecoveryController
{
    TimeSpan PollInterval { get; }

    Task<OpenAiPresetRecoveryDecision> TryRecoverAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Low-frequency, opt-in recovery controller. It promotes the preferred Sol-only
/// set only after the configured number of complete-tier Responses canaries
/// succeed, and never writes the operator's environment configuration.
/// </summary>
public sealed class OpenAiPresetRecoveryController : IOpenAiPresetRecoveryController
{
    private static readonly OpenAiExecutionQualityTier[] RequiredTiers =
    [
        OpenAiExecutionQualityTier.Deep,
        OpenAiExecutionQualityTier.Balanced,
        OpenAiExecutionQualityTier.Fast,
    ];

    private readonly OpenAiProviderOptions _options;
    private readonly IOpenAiActivePresetSetState _activePresetSetState;
    private readonly IOpenAiModelAvailabilityProbe _availabilityProbe;
    private readonly IOpenAiPresetRecoveryCanary _canary;
    private readonly IOpenAiExecutionSlotScheduler _executionSlots;
    private readonly OpenAiPresetRecoveryOptions _recoveryOptions;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTimeOffset _nextEligibleProbeAt = DateTimeOffset.MinValue;
    private int _successfulCanaries;
    private int _consecutiveFailures;

    public OpenAiPresetRecoveryController(
        OpenAiProviderOptions options,
        IOpenAiActivePresetSetState activePresetSetState,
        IOpenAiModelAvailabilityProbe availabilityProbe,
        IOpenAiPresetRecoveryCanary canary,
        IOpenAiExecutionSlotScheduler executionSlots,
        OpenAiPresetRecoveryOptions? recoveryOptions = null,
        TimeProvider? timeProvider = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _activePresetSetState = activePresetSetState ?? throw new ArgumentNullException(nameof(activePresetSetState));
        _availabilityProbe = availabilityProbe ?? throw new ArgumentNullException(nameof(availabilityProbe));
        _canary = canary ?? throw new ArgumentNullException(nameof(canary));
        _executionSlots = executionSlots ?? throw new ArgumentNullException(nameof(executionSlots));
        _recoveryOptions = recoveryOptions ?? OpenAiPresetRecoveryOptions.Default;
        var validationErrors = _recoveryOptions.Validate();
        if (validationErrors.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", validationErrors), nameof(recoveryOptions));
        }

        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public TimeSpan PollInterval => _recoveryOptions.ProbeInterval;

    public async Task<OpenAiPresetRecoveryDecision> TryRecoverAsync(CancellationToken cancellationToken)
    {
        if (_options.TextRoutingMode is not OpenAiTextRoutingMode.Auto
            || _options.PresetRecoveryMode is not OpenAiPresetRecoveryMode.PreferSol)
        {
            return new OpenAiPresetRecoveryDecision(
                OpenAiPresetRecoveryDecisionKind.Disabled,
                _activePresetSetState.GetActivePresetSet(_options) ?? _options.InitialPresetSet,
                CandidatePresetSet: null,
                SuccessfulCanaries: 0);
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var snapshot = _activePresetSetState.GetSnapshot(_options);
            var activePresetSet = snapshot.PresetSet ?? _options.InitialPresetSet;
            if (string.Equals(activePresetSet, TextProviderModelPresetSets.SolOnly, StringComparison.Ordinal))
            {
                Reset();
                return new OpenAiPresetRecoveryDecision(
                    OpenAiPresetRecoveryDecisionKind.AlreadyPreferred,
                    activePresetSet,
                    TextProviderModelPresetSets.SolOnly,
                    SuccessfulCanaries: 0);
            }

            var now = _timeProvider.GetUtcNow();
            if (_nextEligibleProbeAt > now)
            {
                return new OpenAiPresetRecoveryDecision(
                    OpenAiPresetRecoveryDecisionKind.CoolingDown,
                    activePresetSet,
                    TextProviderModelPresetSets.SolOnly,
                    _successfulCanaries,
                    _nextEligibleProbeAt);
            }

            if (!TextProviderModelPresets.TryResolveForPresetSet(
                    TextProviderModelPresetSets.SolOnly,
                    OpenAiExecutionQualityTier.Deep,
                    out _,
                    out var solModel,
                    out _))
            {
                return RecordFailure(OpenAiPresetRecoveryDecisionKind.CandidateUnavailable, activePresetSet, now);
            }

            var availability = await _availabilityProbe.ProbeAsync(_options, solModel, cancellationToken);
            if (!availability.IsAvailable)
            {
                return RecordFailure(OpenAiPresetRecoveryDecisionKind.CandidateUnavailable, activePresetSet, now);
            }

            foreach (var tier in RequiredTiers)
            {
                if (!TextProviderModelPresets.TryResolveForPresetSet(
                        TextProviderModelPresetSets.SolOnly,
                        tier,
                        out _,
                        out var model,
                        out var effort))
                {
                    return RecordFailure(OpenAiPresetRecoveryDecisionKind.CandidateUnhealthy, activePresetSet, now);
                }

                try
                {
                    using var canaryCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    canaryCancellation.CancelAfter(_recoveryOptions.CanaryTimeout);
                    var result = await _executionSlots.ExecuteAsync(
                        tier,
                        () => _canary.ProbeAsync(
                            _options,
                            new OpenAiPresetRecoveryCanaryRequest(model, effort, tier),
                            canaryCancellation.Token),
                        canaryCancellation.Token);
                    if (!result.IsSuccess)
                    {
                        return RecordFailure(OpenAiPresetRecoveryDecisionKind.CandidateUnhealthy, activePresetSet, now);
                    }
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    return RecordFailure(OpenAiPresetRecoveryDecisionKind.CandidateUnhealthy, activePresetSet, now);
                }
                catch (OpenAiExecutionQueueFullException)
                {
                    _nextEligibleProbeAt = now.Add(_recoveryOptions.ProbeInterval);
                    return new OpenAiPresetRecoveryDecision(
                        OpenAiPresetRecoveryDecisionKind.DeferredByCapacity,
                        activePresetSet,
                        TextProviderModelPresetSets.SolOnly,
                        _successfulCanaries,
                        _nextEligibleProbeAt);
                }
            }

            _consecutiveFailures = 0;
            _successfulCanaries++;
            if (_successfulCanaries < _recoveryOptions.RequiredSuccessfulCanaries)
            {
                _nextEligibleProbeAt = now.Add(_recoveryOptions.ProbeInterval);
                return new OpenAiPresetRecoveryDecision(
                    OpenAiPresetRecoveryDecisionKind.CandidateStabilizing,
                    activePresetSet,
                    TextProviderModelPresetSets.SolOnly,
                    _successfulCanaries,
                    _nextEligibleProbeAt);
            }

            var successfulCanaries = _successfulCanaries;
            var switched = _activePresetSetState.TrySwitchActivePresetSet(
                _options,
                snapshot.Version,
                TextProviderModelPresetSets.SolOnly);
            Reset();
            return new OpenAiPresetRecoveryDecision(
                switched ? OpenAiPresetRecoveryDecisionKind.Promoted : OpenAiPresetRecoveryDecisionKind.ConcurrentChange,
                switched ? TextProviderModelPresetSets.SolOnly : activePresetSet,
                TextProviderModelPresetSets.SolOnly,
                successfulCanaries);
        }
        finally
        {
            _gate.Release();
        }
    }

    private OpenAiPresetRecoveryDecision RecordFailure(
        OpenAiPresetRecoveryDecisionKind kind,
        string? activePresetSet,
        DateTimeOffset now)
    {
        _successfulCanaries = 0;
        _consecutiveFailures++;
        var multiplier = 1L << Math.Min(_consecutiveFailures - 1, 10);
        var ticks = Math.Min(
            _recoveryOptions.InitialFailureBackoff.Ticks * multiplier,
            _recoveryOptions.MaximumFailureBackoff.Ticks);
        _nextEligibleProbeAt = now.Add(TimeSpan.FromTicks(ticks));
        return new OpenAiPresetRecoveryDecision(
            kind,
            activePresetSet,
            TextProviderModelPresetSets.SolOnly,
            _successfulCanaries,
            _nextEligibleProbeAt);
    }

    private void Reset()
    {
        _successfulCanaries = 0;
        _consecutiveFailures = 0;
        _nextEligibleProbeAt = DateTimeOffset.MinValue;
    }
}

public sealed class OpenAiResponsesRecoveryCanary : IOpenAiPresetRecoveryCanary
{
    private readonly HttpClient _httpClient;
    private readonly IOpenAiSecretStore _secretStore;

    public OpenAiResponsesRecoveryCanary(HttpClient httpClient, IOpenAiSecretStore secretStore)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _secretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));
    }

    public async Task<OpenAiPresetRecoveryCanaryResult> ProbeAsync(
        OpenAiProviderOptions options,
        OpenAiPresetRecoveryCanaryRequest canaryRequest,
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
            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                new Uri(options.BaseUri, OpenAiProviderRoutingPolicy.ForTextPlanning().RelativePath));
            ProviderRequestAuthentication.Apply(httpRequest, credentials);
            httpRequest.Content = JsonContent.Create(new Dictionary<string, object?>
            {
                ["model"] = canaryRequest.Model,
                ["input"] = "Reply with exactly OK.",
                ["store"] = false,
                ["reasoning"] = new Dictionary<string, object?>
                {
                    ["effort"] = canaryRequest.ReasoningEffort,
                },
            });

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            return new OpenAiPresetRecoveryCanaryResult(
                canaryRequest.Model,
                canaryRequest.ReasoningEffort,
                response.IsSuccessStatusCode,
                response.StatusCode);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            return new OpenAiPresetRecoveryCanaryResult(
                canaryRequest.Model,
                canaryRequest.ReasoningEffort,
                IsSuccess: false,
                exception.StatusCode);
        }
        catch (Exception)
        {
            return new OpenAiPresetRecoveryCanaryResult(
                canaryRequest.Model,
                canaryRequest.ReasoningEffort,
                IsSuccess: false,
                StatusCode: null);
        }
    }
}
