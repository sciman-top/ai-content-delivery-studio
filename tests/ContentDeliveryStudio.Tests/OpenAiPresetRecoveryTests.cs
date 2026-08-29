using System.Net;
using ContentDeliveryStudio.Infrastructure.OpenAI;

namespace ContentDeliveryStudio.Tests;

public sealed class OpenAiPresetRecoveryTests
{
    [Fact]
    public async Task TryRecoverAsync_RequiresAutoPreferSolAndDoesNotProbeWhenDisabled()
    {
        var probe = new RecordingAvailabilityProbe(isAvailable: true);
        var canary = new RecordingCanary(success: true);
        var controller = CreateController(
            new OpenAiProviderOptions
            {
                TextRoutingMode = OpenAiTextRoutingMode.Fixed,
                PresetRecoveryMode = OpenAiPresetRecoveryMode.Disabled,
                InitialPresetSet = TextProviderModelPresetSets.TerraOnly,
            },
            new OpenAiActivePresetSetState(),
            probe,
            canary,
            new MutableTimeProvider(DateTimeOffset.Parse("2026-08-29T00:00:00Z")));

        var decision = await controller.TryRecoverAsync(CancellationToken.None);

        Assert.Equal(OpenAiPresetRecoveryDecisionKind.Disabled, decision.Kind);
        Assert.Empty(probe.Models);
        Assert.Empty(canary.Routes);
    }

    [Fact]
    public async Task TryRecoverAsync_PromotesSolOnlyAfterTwoSuccessfulCompleteTierCanaries()
    {
        var now = new MutableTimeProvider(DateTimeOffset.Parse("2026-08-29T00:00:00Z"));
        var options = CreateAutoRecoveryOptions();
        var state = new OpenAiActivePresetSetState();
        state.MarkActivePresetSet(options, TextProviderModelPresetSets.TerraOnly);
        var canary = new RecordingCanary(success: true);
        var controller = CreateController(options, state, new RecordingAvailabilityProbe(isAvailable: true), canary, now);

        var first = await controller.TryRecoverAsync(CancellationToken.None);
        now.Advance(TimeSpan.FromMinutes(10));
        var second = await controller.TryRecoverAsync(CancellationToken.None);

        Assert.Equal(OpenAiPresetRecoveryDecisionKind.CandidateStabilizing, first.Kind);
        Assert.Equal(1, first.SuccessfulCanaries);
        Assert.Equal(OpenAiPresetRecoveryDecisionKind.Promoted, second.Kind);
        Assert.Equal(TextProviderModelPresetSets.SolOnly, state.GetActivePresetSet(options));
        Assert.Equal(
            ["gpt-5.6-sol/xhigh", "gpt-5.6-sol/medium", "gpt-5.6-sol/low", "gpt-5.6-sol/xhigh", "gpt-5.6-sol/medium", "gpt-5.6-sol/low"],
            canary.Routes.Select(route => $"{route.Model}/{route.ReasoningEffort}"));
    }

    [Fact]
    public async Task TryRecoverAsync_BacksOffAfterAnUnhealthySolCanary()
    {
        var now = new MutableTimeProvider(DateTimeOffset.Parse("2026-08-29T00:00:00Z"));
        var options = CreateAutoRecoveryOptions();
        var state = new OpenAiActivePresetSetState();
        state.MarkActivePresetSet(options, TextProviderModelPresetSets.TerraOnly);
        var canary = new RecordingCanary(success: false);
        var controller = CreateController(options, state, new RecordingAvailabilityProbe(isAvailable: true), canary, now);

        var first = await controller.TryRecoverAsync(CancellationToken.None);
        var coolingDown = await controller.TryRecoverAsync(CancellationToken.None);
        now.Advance(TimeSpan.FromMinutes(10));
        var second = await controller.TryRecoverAsync(CancellationToken.None);

        Assert.Equal(OpenAiPresetRecoveryDecisionKind.CandidateUnhealthy, first.Kind);
        Assert.Equal(now.GetUtcNow().Subtract(TimeSpan.FromMinutes(10)).Add(TimeSpan.FromMinutes(10)), first.NextEligibleProbeAt);
        Assert.Equal(OpenAiPresetRecoveryDecisionKind.CoolingDown, coolingDown.Kind);
        Assert.Equal(OpenAiPresetRecoveryDecisionKind.CandidateUnhealthy, second.Kind);
        Assert.Equal(now.GetUtcNow().Add(TimeSpan.FromMinutes(20)), second.NextEligibleProbeAt);
        Assert.Equal(TextProviderModelPresetSets.TerraOnly, state.GetActivePresetSet(options));
    }

    [Fact]
    public async Task TryRecoverAsync_LeavesTheNewerActivePresetUntouchedWhenAnotherRequestSwitchesFirst()
    {
        var now = new MutableTimeProvider(DateTimeOffset.Parse("2026-08-29T00:00:00Z"));
        var options = CreateAutoRecoveryOptions();
        var state = new OpenAiActivePresetSetState();
        state.MarkActivePresetSet(options, TextProviderModelPresetSets.TerraOnly);
        var canary = new SwitchingCanary(state, options);
        var controller = CreateController(options, state, new RecordingAvailabilityProbe(isAvailable: true), canary, now,
            new OpenAiPresetRecoveryOptions(
                TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(60),
                RequiredSuccessfulCanaries: 1,
                CanaryTimeout: TimeSpan.FromSeconds(45)));

        var decision = await controller.TryRecoverAsync(CancellationToken.None);

        Assert.Equal(OpenAiPresetRecoveryDecisionKind.ConcurrentChange, decision.Kind);
        Assert.Equal(TextProviderModelPresetSets.LunaOnly, state.GetActivePresetSet(options));
    }

    private static OpenAiProviderOptions CreateAutoRecoveryOptions() => new()
    {
        BaseUri = new Uri("http://127.0.0.1:45335/v1/"),
        TextRoutingMode = OpenAiTextRoutingMode.Auto,
        PresetRecoveryMode = OpenAiPresetRecoveryMode.PreferSol,
        InitialPresetSet = TextProviderModelPresetSets.TerraOnly,
    };

    private static OpenAiPresetRecoveryController CreateController(
        OpenAiProviderOptions options,
        IOpenAiActivePresetSetState state,
        IOpenAiModelAvailabilityProbe probe,
        IOpenAiPresetRecoveryCanary canary,
        TimeProvider timeProvider,
        OpenAiPresetRecoveryOptions? recoveryOptions = null)
    {
        return new OpenAiPresetRecoveryController(
            options,
            state,
            probe,
            canary,
            new ImmediateExecutionSlotScheduler(),
            recoveryOptions,
            timeProvider);
    }

    private sealed class RecordingAvailabilityProbe(bool isAvailable) : IOpenAiModelAvailabilityProbe
    {
        public List<string> Models { get; } = [];

        public Task<OpenAiModelAvailabilityResult> ProbeAsync(
            OpenAiProviderOptions options,
            string model,
            CancellationToken cancellationToken)
        {
            Models.Add(model);
            return Task.FromResult(new OpenAiModelAvailabilityResult(model, isAvailable, HttpStatusCode.OK, "test"));
        }
    }

    private class RecordingCanary(bool success) : IOpenAiPresetRecoveryCanary
    {
        public List<OpenAiPresetRecoveryCanaryRequest> Routes { get; } = [];

        public virtual Task<OpenAiPresetRecoveryCanaryResult> ProbeAsync(
            OpenAiProviderOptions options,
            OpenAiPresetRecoveryCanaryRequest request,
            CancellationToken cancellationToken)
        {
            Routes.Add(request);
            return Task.FromResult(new OpenAiPresetRecoveryCanaryResult(
                request.Model,
                request.ReasoningEffort,
                success,
                success ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable));
        }
    }

    private sealed class SwitchingCanary : RecordingCanary
    {
        private readonly IOpenAiActivePresetSetState _state;
        private readonly OpenAiProviderOptions _options;
        private bool _switched;

        public SwitchingCanary(IOpenAiActivePresetSetState state, OpenAiProviderOptions options)
            : base(success: true)
        {
            _state = state;
            _options = options;
        }

        public override Task<OpenAiPresetRecoveryCanaryResult> ProbeAsync(
            OpenAiProviderOptions options,
            OpenAiPresetRecoveryCanaryRequest request,
            CancellationToken cancellationToken)
        {
            if (!_switched)
            {
                _state.MarkActivePresetSet(_options, TextProviderModelPresetSets.LunaOnly);
                _switched = true;
            }

            return base.ProbeAsync(options, request, cancellationToken);
        }
    }

    private sealed class ImmediateExecutionSlotScheduler : IOpenAiExecutionSlotScheduler
    {
        public Task<TResult> ExecuteAsync<TResult>(
            OpenAiExecutionQualityTier qualityTier,
            Func<Task<TResult>> operation,
            CancellationToken cancellationToken) => operation();
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now = _now.Add(duration);
    }
}
