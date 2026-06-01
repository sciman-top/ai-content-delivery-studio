using ImageSeriesStudio.Infrastructure.OpenAI;

namespace ImageSeriesStudio.Tests;

public sealed class OpenAiProviderSmokeTests
{
    [Fact]
    public async Task EvaluateAsync_DefaultsToDryRunWhenOptInVariableIsMissing()
    {
        var decision = await OpenAiSmokeTestGate.EvaluateAsync(
            new OpenAiProviderOptions(),
            new StaticSecretStore("test-key"),
            new DictionaryEnvironment(),
            new OpenAiSmokeTestOptions(),
            CancellationToken.None);

        Assert.False(decision.CanRunRealApiSmoke);
        Assert.True(decision.IsDryRun);
        Assert.Contains(decision.Reasons, reason => reason.Contains("not opted in", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(decision.Reasons, reason => reason.Contains("disabled", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EvaluateAsync_RequiresBothEnvironmentOptInAndRealApiReadiness()
    {
        var environment = new DictionaryEnvironment
        {
            [OpenAiSmokeTestGate.DefaultOptInEnvironmentVariable] = "1",
        };

        var decision = await OpenAiSmokeTestGate.EvaluateAsync(
            new OpenAiProviderOptions { RealApiEnabled = true },
            new StaticSecretStore("test-key"),
            environment,
            new OpenAiSmokeTestOptions(),
            CancellationToken.None);

        Assert.True(decision.CanRunRealApiSmoke);
        Assert.False(decision.IsDryRun);
        Assert.Empty(decision.Reasons);
    }

    [Fact]
    public async Task EvaluateAsync_BlocksWhenOptedInButApiKeyIsMissing()
    {
        var environment = new DictionaryEnvironment
        {
            [OpenAiSmokeTestGate.DefaultOptInEnvironmentVariable] = "1",
        };

        var decision = await OpenAiSmokeTestGate.EvaluateAsync(
            new OpenAiProviderOptions { RealApiEnabled = true },
            new StaticSecretStore(null),
            environment,
            new OpenAiSmokeTestOptions(),
            CancellationToken.None);

        Assert.False(decision.CanRunRealApiSmoke);
        Assert.True(decision.IsDryRun);
        Assert.Contains(decision.Reasons, reason => reason.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EvaluateAsync_UsesCustomOptInVariableAndValue()
    {
        var environment = new DictionaryEnvironment
        {
            ["LOCAL_OPENAI_SMOKE"] = "yes",
        };

        var decision = await OpenAiSmokeTestGate.EvaluateAsync(
            new OpenAiProviderOptions { RealApiEnabled = true },
            new StaticSecretStore("test-key"),
            environment,
            new OpenAiSmokeTestOptions("LOCAL_OPENAI_SMOKE", "yes"),
            CancellationToken.None);

        Assert.True(decision.CanRunRealApiSmoke);
        Assert.Equal("LOCAL_OPENAI_SMOKE", decision.OptInEnvironmentVariable);
    }

    [Fact]
    public void SystemOpenAiEnvironment_ReadsProcessEnvironmentWithoutPersistingSecrets()
    {
        const string variableName = "IMAGE_SERIES_STUDIO_TEST_SMOKE_OPT_IN";
        var previousValue = Environment.GetEnvironmentVariable(variableName);

        try
        {
            Environment.SetEnvironmentVariable(variableName, "1");

            Assert.Equal("1", new SystemOpenAiEnvironment().GetEnvironmentVariable(variableName));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, previousValue);
        }
    }

    private sealed class StaticSecretStore(string? value) : IOpenAiSecretStore
    {
        public Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(value);
        }
    }

    private sealed class DictionaryEnvironment : Dictionary<string, string>, IOpenAiEnvironment
    {
        public string? GetEnvironmentVariable(string variableName)
        {
            return TryGetValue(variableName, out var value) ? value : null;
        }
    }
}
