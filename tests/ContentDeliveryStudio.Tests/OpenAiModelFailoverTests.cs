using System.Net;
using ContentDeliveryStudio.Infrastructure.OpenAI;

namespace ContentDeliveryStudio.Tests;

public sealed class OpenAiModelFailoverTests
{
    [Theory]
    [InlineData("sol", OpenAiExecutionQualityTier.Deep, "gpt-5.6-sol", "high", "sol-high")]
    [InlineData("sol", OpenAiExecutionQualityTier.Balanced, "gpt-5.6-sol", "medium", "sol-medium")]
    [InlineData("sol", OpenAiExecutionQualityTier.Fast, "gpt-5.6-sol", "low", "sol-low")]
    [InlineData("terra", OpenAiExecutionQualityTier.Deep, "gpt-5.6-terra", "max", "terra-max")]
    [InlineData("terra", OpenAiExecutionQualityTier.Balanced, "gpt-5.6-terra", "xhigh", "terra-xhigh")]
    [InlineData("terra", OpenAiExecutionQualityTier.Fast, "gpt-5.6-terra", "high", "terra-high")]
    [InlineData("luna", OpenAiExecutionQualityTier.Deep, "gpt-5.6-luna", "max", "luna-max")]
    [InlineData("luna", OpenAiExecutionQualityTier.Balanced, "gpt-5.6-luna", "xhigh", "luna-xhigh")]
    [InlineData("luna", OpenAiExecutionQualityTier.Fast, "gpt-5.6-luna", "high", "luna-high")]
    [InlineData("glm-5.3-flash", OpenAiExecutionQualityTier.Deep, "glm-5.3-flash", "max", "glm-5.3-flash-max")]
    [InlineData("glm-5.3-flash", OpenAiExecutionQualityTier.Balanced, "glm-5.3-flash", "high", "glm-5.3-flash-high")]
    [InlineData("glm-5.3-flash", OpenAiExecutionQualityTier.Fast, "glm-5.3-flash", "low", "glm-5.3-flash-low")]
    [InlineData("deepseek-v4", OpenAiExecutionQualityTier.Deep, "deepseek-v4-pro", "max", "deepseek-v4-pro-max")]
    [InlineData("deepseek-v4", OpenAiExecutionQualityTier.Balanced, "deepseek-v4-flash", "max", "deepseek-v4-flash-max")]
    [InlineData("deepseek-v4", OpenAiExecutionQualityTier.Fast, "deepseek-v4-flash", "high", "deepseek-v4-flash-high")]
    public void FallbackRoutes_SwitchModelFamilyInConfiguredPriorityOrder(
        string family,
        OpenAiExecutionQualityTier qualityTier,
        string expectedModel,
        string expectedReasoningEffort,
        string expectedPreset)
    {
        var route = new OpenAiTaskModelRoute(
            expectedPreset,
            expectedModel,
            expectedReasoningEffort,
            "test",
            qualityTier);

        var fallbackRoutes = OpenAiModelFailoverPolicy.GetFallbackRoutes(route);

        Assert.Equal(
            TextProviderModelPresets.PreferredModelFamilies
                .Where(candidate => candidate != family)
                .ToArray(),
            fallbackRoutes
                .Select(item => TextProviderModelPresets.TryGetFamily(item.Model, out var itemFamily) ? itemFamily : string.Empty));
        Assert.All(fallbackRoutes, fallback =>
        {
            Assert.Equal(qualityTier, fallback.QualityTier);
            Assert.Equal(
                qualityTier,
                TextProviderModelPresets.TryGetQualityTier(fallback.Preset, out var resolvedTier)
                    ? resolvedTier
                    : throw new Xunit.Sdk.XunitException("Fallback preset did not resolve."));
        });
    }

    [Fact]
    public async Task Execute_RemembersSuccessfulFallbackAndChecksSolBeforeLunaWhenTerraFails()
    {
        var probe = new RecordingAvailabilityProbe(
            ("gpt-5.6-terra", true),
            ("gpt-5.6-sol", true));
        var state = new OpenAiActivePresetSetState();
        var route = new OpenAiTaskModelRoute(
            TextProviderModelPresets.SolMedium,
            "gpt-5.6-sol",
            "medium",
            "test",
            OpenAiExecutionQualityTier.Balanced,
            TextProviderModelPresetSets.SolOnly);
        var options = new OpenAiProviderOptions
        {
            BaseUri = new Uri("http://127.0.0.1:45335/v1/"),
            TextRoutingMode = OpenAiTextRoutingMode.Auto,
        };
        var dispatchedRoutes = new List<OpenAiTaskModelRoute>();

        var firstResult = await OpenAiModelFailoverPolicy.ExecuteAsync(
            options,
            route,
            probe,
            candidateRoute =>
            {
                dispatchedRoutes.Add(candidateRoute);
                return candidateRoute.Model == "gpt-5.6-sol"
                    ? Task.FromException<string>(new HttpRequestException("status 503"))
                    : Task.FromResult("terra-success");
            },
            CancellationToken.None,
            activePresetSetState: state);

        var secondResult = await OpenAiModelFailoverPolicy.ExecuteAsync(
            options,
            route,
            probe,
            candidateRoute =>
            {
                dispatchedRoutes.Add(candidateRoute);
                return candidateRoute.Model == "gpt-5.6-terra"
                    ? Task.FromException<string>(new HttpRequestException("status 503"))
                    : Task.FromResult("sol-recovered");
            },
            CancellationToken.None,
            activePresetSetState: state);

        Assert.Equal("terra-success", firstResult);
        Assert.Equal("sol-recovered", secondResult);
        Assert.Equal(
            new[] { "gpt-5.6-sol", "gpt-5.6-terra", "gpt-5.6-terra", "gpt-5.6-sol" },
            dispatchedRoutes.Select(item => item.Model));
        Assert.Equal(
            new[] { "medium", "xhigh", "xhigh", "medium" },
            dispatchedRoutes.Select(item => item.ReasoningEffort));
        Assert.Equal(new[] { "gpt-5.6-terra", "gpt-5.6-sol" }, probe.ProbedModels);
        Assert.Equal(TextProviderModelPresetSets.SolOnly, state.GetActivePresetSet(options));
    }

    [Fact]
    public async Task Execute_DoesNotLetAnOlderSuccessfulRequestOverwriteAFallbackSwitch()
    {
        var state = new OpenAiActivePresetSetState();
        var options = new OpenAiProviderOptions
        {
            BaseUri = new Uri("http://127.0.0.1:45335/v1/"),
            TextRoutingMode = OpenAiTextRoutingMode.Auto,
        };
        var route = new OpenAiTaskModelRoute(
            TextProviderModelPresets.SolMedium,
            "gpt-5.6-sol",
            "medium",
            "test",
            OpenAiExecutionQualityTier.Balanced,
            TextProviderModelPresetSets.SolOnly);
        var solRequestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSolRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var probe = new RecordingAvailabilityProbe(("gpt-5.6-terra", true));

        var olderSolRequest = OpenAiModelFailoverPolicy.ExecuteAsync(
            options,
            route,
            probe,
            async candidateRoute =>
            {
                Assert.Equal("gpt-5.6-sol", candidateRoute.Model);
                solRequestStarted.TrySetResult();
                await releaseSolRequest.Task;
                return "late-sol-success";
            },
            CancellationToken.None,
            activePresetSetState: state);

        await solRequestStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var fallbackResult = await OpenAiModelFailoverPolicy.ExecuteAsync(
            options,
            route,
            probe,
            candidateRoute => candidateRoute.Model == "gpt-5.6-sol"
                ? Task.FromException<string>(new HttpRequestException("status 503"))
                : Task.FromResult("terra-success"),
            CancellationToken.None,
            activePresetSetState: state);

        releaseSolRequest.TrySetResult();

        Assert.Equal("terra-success", fallbackResult);
        Assert.Equal("late-sol-success", await olderSolRequest);
        Assert.Equal(TextProviderModelPresetSets.TerraOnly, state.GetActivePresetSet(options));
    }

    [Fact]
    public async Task Execute_UsesConfiguredInitialPresetSetBeforeTheDefaultSolSet()
    {
        var options = new OpenAiProviderOptions
        {
            BaseUri = new Uri("http://127.0.0.1:45335/v1/"),
            InitialPresetSet = TextProviderModelPresetSets.TerraOnly,
        };
        var route = new OpenAiTaskModelRoute(
            TextProviderModelPresets.SolLow,
            "gpt-5.6-sol",
            "low",
            "test",
            OpenAiExecutionQualityTier.Fast,
            TextProviderModelPresetSets.SolOnly);

        var model = await OpenAiModelFailoverPolicy.ExecuteAsync(
            options,
            route,
            availabilityProbe: null,
            operation: candidateRoute => Task.FromResult(candidateRoute.Model),
            CancellationToken.None,
            activePresetSetState: new OpenAiActivePresetSetState());

        Assert.Equal("gpt-5.6-terra", model);
    }

    [Theory]
    [InlineData(TextProviderModelPresetSets.SolOnly, OpenAiExecutionQualityTier.Deep, "gpt-5.6-sol", "high")]
    [InlineData(TextProviderModelPresetSets.SolOnly, OpenAiExecutionQualityTier.Balanced, "gpt-5.6-sol", "medium")]
    [InlineData(TextProviderModelPresetSets.SolOnly, OpenAiExecutionQualityTier.Fast, "gpt-5.6-sol", "low")]
    [InlineData(TextProviderModelPresetSets.TerraOnly, OpenAiExecutionQualityTier.Deep, "gpt-5.6-terra", "max")]
    [InlineData(TextProviderModelPresetSets.TerraOnly, OpenAiExecutionQualityTier.Balanced, "gpt-5.6-terra", "xhigh")]
    [InlineData(TextProviderModelPresetSets.TerraOnly, OpenAiExecutionQualityTier.Fast, "gpt-5.6-terra", "high")]
    [InlineData(TextProviderModelPresetSets.LunaOnly, OpenAiExecutionQualityTier.Deep, "gpt-5.6-luna", "max")]
    [InlineData(TextProviderModelPresetSets.LunaOnly, OpenAiExecutionQualityTier.Balanced, "gpt-5.6-luna", "xhigh")]
    [InlineData(TextProviderModelPresetSets.LunaOnly, OpenAiExecutionQualityTier.Fast, "gpt-5.6-luna", "high")]
    [InlineData(TextProviderModelPresetSets.GlmOnly, OpenAiExecutionQualityTier.Deep, "glm-5.3-flash", "max")]
    [InlineData(TextProviderModelPresetSets.GlmOnly, OpenAiExecutionQualityTier.Balanced, "glm-5.3-flash", "high")]
    [InlineData(TextProviderModelPresetSets.GlmOnly, OpenAiExecutionQualityTier.Fast, "glm-5.3-flash", "low")]
    [InlineData(TextProviderModelPresetSets.DeepSeekV4Only, OpenAiExecutionQualityTier.Deep, "deepseek-v4-pro", "max")]
    [InlineData(TextProviderModelPresetSets.DeepSeekV4Only, OpenAiExecutionQualityTier.Balanced, "deepseek-v4-flash", "max")]
    [InlineData(TextProviderModelPresetSets.DeepSeekV4Only, OpenAiExecutionQualityTier.Fast, "deepseek-v4-flash", "high")]
    public void PresetSet_MapsEveryTierToOneAndOnlyOneModelFamily(
        string presetSet,
        OpenAiExecutionQualityTier qualityTier,
        string expectedModel,
        string expectedReasoningEffort)
    {
        Assert.True(TextProviderModelPresets.TryResolveForPresetSet(
            presetSet,
            qualityTier,
            out var preset,
            out var model,
            out var reasoningEffort));

        Assert.Equal(expectedModel, model);
        Assert.Equal(expectedReasoningEffort, reasoningEffort);
        Assert.True(TextProviderModelPresets.TryGetPresetSet(preset, out var resolvedPresetSet));
        Assert.Equal(presetSet, resolvedPresetSet);
    }

    [Fact]
    public async Task Execute_UsesTheEntireActivePresetSetAcrossAllThreeTiers()
    {
        var state = new OpenAiActivePresetSetState();
        var options = new OpenAiProviderOptions
        {
            BaseUri = new Uri("http://127.0.0.1:45335/v1/"),
            TextRoutingMode = OpenAiTextRoutingMode.Auto,
        };
        state.MarkActivePresetSet(options, TextProviderModelPresetSets.TerraOnly);
        var dispatchedRoutes = new List<OpenAiTaskModelRoute>();

        foreach (var (preset, model, effort, tier) in new[]
                 {
                     (TextProviderModelPresets.SolHigh, "gpt-5.6-sol", "high", OpenAiExecutionQualityTier.Deep),
                     (TextProviderModelPresets.SolMedium, "gpt-5.6-sol", "medium", OpenAiExecutionQualityTier.Balanced),
                     (TextProviderModelPresets.SolLow, "gpt-5.6-sol", "low", OpenAiExecutionQualityTier.Fast),
                 })
        {
            await OpenAiModelFailoverPolicy.ExecuteAsync(
                options,
                new OpenAiTaskModelRoute(
                    preset,
                    model,
                    effort,
                    "test",
                    tier,
                    TextProviderModelPresetSets.SolOnly),
                availabilityProbe: null,
                operation: route =>
                {
                    dispatchedRoutes.Add(route);
                    return Task.FromResult(0);
                },
                CancellationToken.None,
                activePresetSetState: state);
        }

        Assert.Equal(
            ["gpt-5.6-terra", "gpt-5.6-terra", "gpt-5.6-terra"],
            dispatchedRoutes.Select(route => route.Model));
        Assert.Equal(["max", "xhigh", "high"], dispatchedRoutes.Select(route => route.ReasoningEffort));
        Assert.All(dispatchedRoutes, route => Assert.Equal(TextProviderModelPresetSets.TerraOnly, route.PresetSet));
    }

    [Fact]
    public async Task ExecutionSlotScheduler_EnforcesDeepOneBalancedTwoAndFastTwo()
    {
        var scheduler = new OpenAiExecutionSlotScheduler();
        var entered = 0;
        var maximum = 0;
        var allEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task RunAsync(OpenAiExecutionQualityTier tier)
        {
            await scheduler.ExecuteAsync(
                tier,
                async () =>
                {
                    var current = Interlocked.Increment(ref entered);
                    var observedMaximum = Volatile.Read(ref maximum);
                    while (current > observedMaximum)
                    {
                        Interlocked.CompareExchange(ref maximum, current, observedMaximum);
                        observedMaximum = Volatile.Read(ref maximum);
                    }

                    if (current == 5)
                    {
                        allEntered.TrySetResult();
                    }

                    await release.Task;
                    Interlocked.Decrement(ref entered);
                    return 0;
                },
                CancellationToken.None);
        }

        var tasks = new[]
        {
            RunAsync(OpenAiExecutionQualityTier.Deep),
            RunAsync(OpenAiExecutionQualityTier.Deep),
            RunAsync(OpenAiExecutionQualityTier.Balanced),
            RunAsync(OpenAiExecutionQualityTier.Balanced),
            RunAsync(OpenAiExecutionQualityTier.Fast),
            RunAsync(OpenAiExecutionQualityTier.Fast),
        };

        await allEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(5, maximum);
        Assert.Equal(5, entered);

        release.TrySetResult();
        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task ExecutionSlotScheduler_RejectsBeyondBoundedQueue()
    {
        var scheduler = new OpenAiExecutionSlotScheduler(
            new OpenAiExecutionSlotSchedulerOptions(MaxQueuedRequestsPerTier: 0));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = scheduler.ExecuteAsync(
            OpenAiExecutionQualityTier.Deep,
            async () =>
            {
                entered.TrySetResult();
                await release.Task;
                return 0;
            },
            CancellationToken.None);

        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await Assert.ThrowsAsync<OpenAiExecutionQueueFullException>(() =>
            scheduler.ExecuteAsync(
                OpenAiExecutionQualityTier.Deep,
                () => Task.FromResult(0),
                CancellationToken.None));

        release.TrySetResult();
        await first;
    }

    [Fact]
    public async Task AvailabilityProbe_CoalescesConcurrentCatalogRequestsAndCachesResult()
    {
        var firstRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var handler = new BlockingModelsHandler(firstRequest, release);
        using var httpClient = new HttpClient(handler);
        var probe = new OpenAiModelAvailabilityProbe(
            httpClient,
            new StaticSecretStore(),
            availableCacheDuration: TimeSpan.FromMinutes(1));
        var options = new OpenAiProviderOptions { BaseUri = new Uri("http://127.0.0.1:45335/v1/") };

        var probes = Enumerable.Range(0, 8)
            .Select(_ => probe.ProbeAsync(options, "gpt-5.6-terra", CancellationToken.None))
            .ToArray();

        await firstRequest.Task.WaitAsync(TimeSpan.FromSeconds(2));
        release.TrySetResult();
        var results = await Task.WhenAll(probes);
        var cachedResult = await probe.ProbeAsync(options, "gpt-5.6-terra", CancellationToken.None);

        Assert.All(results, result => Assert.True(result.IsAvailable));
        Assert.True(cachedResult.IsAvailable);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task Execute_ProbesEachFallbackBeforeDispatchAndUsesTheFirstAvailableFamily()
    {
        var probe = new RecordingAvailabilityProbe(
            ("gpt-5.6-terra", false),
            ("gpt-5.6-luna", true));
        var route = new OpenAiTaskModelRoute(
            TextProviderModelPresets.SolHigh,
            "gpt-5.6-sol",
            "xhigh",
            "test");
        var dispatchedModels = new List<string>();

        var result = await OpenAiModelFailoverPolicy.ExecuteAsync(
            new OpenAiProviderOptions { RealApiEnabled = false, TextRoutingMode = OpenAiTextRoutingMode.Auto },
            route,
            probe,
            candidateRoute =>
            {
                dispatchedModels.Add(candidateRoute.Model);
                return candidateRoute.Model == "gpt-5.6-sol"
                    ? Task.FromException<string>(new HttpRequestException("status 503"))
                    : Task.FromResult(candidateRoute.Model);
            },
            CancellationToken.None);

        Assert.Equal("gpt-5.6-luna", result);
        Assert.Equal(new[] { "gpt-5.6-sol", "gpt-5.6-luna" }, dispatchedModels);
        Assert.Equal(new[] { "gpt-5.6-terra", "gpt-5.6-luna" }, probe.ProbedModels);
    }

    [Fact]
    public async Task Execute_WhenCurrentTerraFails_ProbesSolBeforeLuna()
    {
        var probe = new RecordingAvailabilityProbe(
            ("gpt-5.6-sol", false),
            ("gpt-5.6-luna", true));
        var route = new OpenAiTaskModelRoute(
            TextProviderModelPresets.TerraHigh,
            "gpt-5.6-terra",
            "high",
            "test");
        var dispatchedModels = new List<string>();

        var result = await OpenAiModelFailoverPolicy.ExecuteAsync(
            new OpenAiProviderOptions { RealApiEnabled = false, TextRoutingMode = OpenAiTextRoutingMode.Auto },
            route,
            probe,
            candidateRoute =>
            {
                dispatchedModels.Add(candidateRoute.Model);
                return candidateRoute.Model == "gpt-5.6-terra"
                    ? Task.FromException<string>(new HttpRequestException("status 503"))
                    : Task.FromResult(candidateRoute.Model);
            },
            CancellationToken.None);

        Assert.Equal("gpt-5.6-luna", result);
        Assert.Equal(new[] { "gpt-5.6-terra", "gpt-5.6-luna" }, dispatchedModels);
        Assert.Equal(new[] { "gpt-5.6-sol", "gpt-5.6-luna" }, probe.ProbedModels);
    }

    [Fact]
    public async Task Execute_FixedModeDoesNotCrossModelFamilies()
    {
        var probe = new RecordingAvailabilityProbe(("gpt-5.6-terra", true));
        var route = new OpenAiTaskModelRoute(
            TextProviderModelPresets.SolHigh,
            "gpt-5.6-sol",
            "xhigh",
            "test");
        var dispatchedModels = new List<string>();

        await Assert.ThrowsAsync<HttpRequestException>(() => OpenAiModelFailoverPolicy.ExecuteAsync(
            new OpenAiProviderOptions { TextRoutingMode = OpenAiTextRoutingMode.Fixed },
            route,
            probe,
            candidateRoute =>
            {
                dispatchedModels.Add(candidateRoute.Model);
                return Task.FromException<string>(new HttpRequestException("status 503"));
            },
            CancellationToken.None));

        Assert.Equal(["gpt-5.6-sol"], dispatchedModels);
        Assert.Empty(probe.ProbedModels);
    }

    private sealed class RecordingAvailabilityProbe(
        params (string Model, bool Available)[] results) : IOpenAiModelAvailabilityProbe
    {
        private readonly IReadOnlyDictionary<string, bool> _results = results
            .ToDictionary(item => item.Model, item => item.Available, StringComparer.Ordinal);

        public List<string> ProbedModels { get; } = [];

        public Task<OpenAiModelAvailabilityResult> ProbeAsync(
            OpenAiProviderOptions options,
            string model,
            CancellationToken cancellationToken)
        {
            ProbedModels.Add(model);
            var available = _results.TryGetValue(model, out var value) && value;
            return Task.FromResult(
                new OpenAiModelAvailabilityResult(
                    model,
                    available,
                    available ? HttpStatusCode.OK : HttpStatusCode.NotFound,
                    "test"));
        }
    }

    private sealed class BlockingModelsHandler(
        TaskCompletionSource firstRequest,
        TaskCompletionSource release) : HttpMessageHandler
    {
        private int _callCount;

        public int CallCount => Volatile.Read(ref _callCount);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _callCount);
            firstRequest.TrySetResult();
            await release.Task.WaitAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":[{\"id\":\"gpt-5.6-terra\"}]}")
            };
        }
    }

    private sealed class StaticSecretStore : IOpenAiSecretStore
    {
        public Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<string?>("test-key");
        }
    }
}
