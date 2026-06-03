using System.Net;
using ImageSeriesStudio.Infrastructure.OpenAI;
using Microsoft.Extensions.DependencyInjection;

namespace ImageSeriesStudio.Tests;

public sealed class OpenAiProviderConfigurationTests
{
    [Fact]
    public void Defaults_KeepRealApiDisabledAndValidateCleanly()
    {
        var options = new OpenAiProviderOptions();

        Assert.False(options.RealApiEnabled);
        Assert.Equal("https", options.BaseUri.Scheme);
        Assert.Equal("OPENAI_API_KEY", options.ApiKeySecretName);
        Assert.Empty(options.Validate());
    }

    [Fact]
    public async Task CheckReadinessAsync_BlocksRealCallsByDefault()
    {
        var readiness = await OpenAiProviderGuard.CheckReadinessAsync(
            new OpenAiProviderOptions(),
            new StaticSecretStore("test-key"),
            CancellationToken.None);

        Assert.False(readiness.CanCallRealApi);
        Assert.Contains(readiness.Errors, error => error.Contains("disabled", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CheckReadinessAsync_BlocksRealCallsWhenApiKeyIsMissing()
    {
        var readiness = await OpenAiProviderGuard.CheckReadinessAsync(
            new OpenAiProviderOptions { RealApiEnabled = true },
            new StaticSecretStore(null),
            CancellationToken.None);

        Assert.False(readiness.CanCallRealApi);
        Assert.Contains(readiness.Errors, error => error.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CheckReadinessAsync_AllowsRealCallsOnlyAfterExplicitOptInAndSecret()
    {
        var readiness = await OpenAiProviderGuard.CheckReadinessAsync(
            new OpenAiProviderOptions { RealApiEnabled = true },
            new StaticSecretStore("test-key"),
            CancellationToken.None);

        Assert.True(readiness.CanCallRealApi);
        Assert.Empty(readiness.Errors);
    }

    [Fact]
    public async Task EnsureCanCallRealApiAsync_ThrowsWhenGuardFails()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            OpenAiProviderGuard.EnsureCanCallRealApiAsync(
                new OpenAiProviderOptions(),
                new StaticSecretStore("test-key"),
                CancellationToken.None));

        Assert.Contains("not ready", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_RejectsInsecureEndpointAndBlankModels()
    {
        var errors = new OpenAiProviderOptions
        {
            BaseUri = new Uri("http://api.openai.example/v1/"),
            TextPlanningModel = " ",
            ImageGenerationModel = "",
            VisionReviewModel = "\t",
        }.Validate();

        Assert.Contains(errors, error => error.Contains("HTTPS", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("Text planning model", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("Image generation model", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("Vision review model", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EnvironmentSecretStore_ReadsProcessEnvironmentVariableWithoutPersistingSecrets()
    {
        const string variableName = "IMAGE_SERIES_STUDIO_TEST_OPENAI_API_KEY";
        var previousValue = Environment.GetEnvironmentVariable(variableName);

        try
        {
            Environment.SetEnvironmentVariable(variableName, "process-test-key");

            var secret = await new EnvironmentOpenAiSecretStore().GetSecretAsync(variableName, CancellationToken.None);

            Assert.Equal("process-test-key", secret);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, previousValue);
        }
    }

    [Fact]
    public async Task EnvironmentSecretStore_RespectsCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new EnvironmentOpenAiSecretStore().GetSecretAsync("OPENAI_API_KEY", cancellation.Token));
    }

    [Fact]
    public async Task DpapiSecretStore_RoundTripsSecretWithoutPlaintextOnDisk()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var directory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));

        try
        {
            var store = new DpapiOpenAiSecretStore(directory);

            await store.SetSecretAsync("OPENAI_API_KEY", "dpapi-test-secret", CancellationToken.None);

            var file = Assert.Single(Directory.GetFiles(directory, "*.dpapi"));
            var protectedBytes = await File.ReadAllBytesAsync(file);
            var protectedText = System.Text.Encoding.UTF8.GetString(protectedBytes);

            Assert.DoesNotContain("OPENAI_API_KEY", Path.GetFileName(file), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("dpapi-test-secret", protectedText, StringComparison.Ordinal);
            Assert.Equal("dpapi-test-secret", await store.GetSecretAsync("OPENAI_API_KEY", CancellationToken.None));

            await store.DeleteSecretAsync("OPENAI_API_KEY", CancellationToken.None);

            Assert.Null(await store.GetSecretAsync("OPENAI_API_KEY", CancellationToken.None));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task CompositeSecretStore_PrefersFirstConfiguredStoreAndFallsBack()
    {
        var preferred = new CompositeOpenAiSecretStore(
        [
            new StaticSecretStore("primary-secret"),
            new StaticSecretStore("fallback-secret"),
        ]);

        var fallback = new CompositeOpenAiSecretStore(
        [
            new StaticSecretStore(" "),
            new StaticSecretStore("fallback-secret"),
        ]);

        Assert.Equal("primary-secret", await preferred.GetSecretAsync("OPENAI_API_KEY", CancellationToken.None));
        Assert.Equal("fallback-secret", await fallback.GetSecretAsync("OPENAI_API_KEY", CancellationToken.None));
    }

    [Fact]
    public async Task CheckReadinessAsync_AllowsRealCallsWithDpapiSecret()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var directory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));

        try
        {
            var store = new DpapiOpenAiSecretStore(directory);
            await store.SetSecretAsync("OPENAI_API_KEY", "dpapi-test-secret", CancellationToken.None);

            var readiness = await OpenAiProviderGuard.CheckReadinessAsync(
                new OpenAiProviderOptions { RealApiEnabled = true },
                store,
                CancellationToken.None);

            Assert.True(readiness.CanCallRealApi);
            Assert.Empty(readiness.Errors);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AddOpenAiProviderHttpClient_RegistersNamedClientAndDoesNotRetryUnsafePost()
    {
        var services = new ServiceCollection();
        var handler = new CountingHandler();
        var options = new OpenAiProviderOptions
        {
            BaseUri = new Uri("https://api.openai.test/v1/"),
        };

        services
            .AddOpenAiProviderHttpClient(options)
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient(OpenAiHttpClientNames.Provider);

        using var response = await client.PostAsync("responses", new StringContent("{}"));

        Assert.Equal("https://api.openai.test/v1/", client.BaseAddress!.ToString());
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(1, handler.CallCount);
        Assert.Same(options, provider.GetRequiredService<OpenAiProviderOptions>());
        Assert.NotNull(provider.GetRequiredService<IOpenAiSecretStore>());
    }

    private sealed class StaticSecretStore(string? value) : IOpenAiSecretStore
    {
        public Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(value);
        }
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CallCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }
    }
}
