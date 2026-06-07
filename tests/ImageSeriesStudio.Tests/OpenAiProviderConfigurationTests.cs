using System.Net;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Infrastructure.OpenAI;
using Microsoft.Extensions.DependencyInjection;

namespace ImageSeriesStudio.Tests;

#pragma warning disable OPENAI001 // Tests intentionally verify ADR 0009 SDK adoption boundary details.
public sealed class OpenAiProviderConfigurationTests
{
    [Fact]
    public async Task DotEnvSecretStore_ReadsDotEnvValuesWithoutPersistingSecrets()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var envPath = Path.Combine(directory, ".env");
        Directory.CreateDirectory(directory);

        try
        {
            await File.WriteAllLinesAsync(
                envPath,
                [
                    "# local provider secrets",
                    "TEXT_PROVIDER_API_KEY=sk-text-test",
                    "IMAGE_PROVIDER_APP_SECRET=\"as-image-test\"",
                    "EMPTY_VALUE=",
                ]);

            var store = new DotEnvSecretStore(envPath);

            Assert.Equal("sk-text-test", await store.GetSecretAsync("TEXT_PROVIDER_API_KEY", CancellationToken.None));
            Assert.Equal("as-image-test", await store.GetSecretAsync("IMAGE_PROVIDER_APP_SECRET", CancellationToken.None));
            Assert.Null(await store.GetSecretAsync("EMPTY_VALUE", CancellationToken.None));
            Assert.Null(await store.GetSecretAsync("MISSING_SECRET", CancellationToken.None));
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
    public void ProviderEnvironmentConfiguration_LoadsSeparatedTextAndImageProviderProfiles()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_KIND"] = "openai_compatible",
                ["TEXT_PROVIDER_BASE_URL"] = "https://text.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-text",
                ["TEXT_PROVIDER_MODEL"] = "gpt-5.5",
                ["IMAGE_PROVIDER_KIND"] = "openai_compatible_image_only",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://image.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "image-model",
                ["IMAGE_PROVIDER_API_KEY_1"] = "sk-image-1",
                ["IMAGE_PROVIDER_API_KEY_2"] = "sk-image-2",
                ["IMAGE_PROVIDER_API_KEY_3"] = "sk-image-3",
                ["IMAGE_PROVIDER_API_KEY_4"] = "sk-image-4",
                ["IMAGE_PROVIDER_APP_ID"] = "app-id",
                ["IMAGE_PROVIDER_APP_SECRET"] = "as-secret",
                ["IMAGE_PROVIDER_CONCURRENCY_PER_KEY"] = "10",
                ["IMAGE_PROVIDER_TOTAL_CONCURRENCY"] = "40",
            });

        Assert.Empty(configuration.Validate());
        Assert.Equal("gpt-5.5", configuration.Text.Model);
        Assert.Equal("TEXT_PROVIDER_API_KEY", configuration.Text.ApiKeySecretName);
        Assert.Equal(4, configuration.Image.ApiKeySecretNames.Count);
        Assert.Equal(10, configuration.Image.ConcurrencyPerKey);
        Assert.Equal(40, configuration.Image.TotalConcurrency);
        Assert.Equal("IMAGE_PROVIDER_APP_ID", configuration.Image.AppIdSecretName);
        Assert.Equal("IMAGE_PROVIDER_APP_SECRET", configuration.Image.AppSecretSecretName);
    }

    [Fact]
    public async Task ProviderEnvironmentConfiguration_LoadsProviderProfilesFromDotEnvFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var envPath = Path.Combine(directory, ".env");
        Directory.CreateDirectory(directory);

        try
        {
            await File.WriteAllLinesAsync(
                envPath,
                [
                    "TEXT_PROVIDER_BASE_URL=https://text.example/v1",
                    "TEXT_PROVIDER_API_KEY=sk-text",
                    "TEXT_PROVIDER_MODEL=gpt-5.5",
                    "IMAGE_PROVIDER_BASE_URL=https://image.example/v1",
                    "IMAGE_PROVIDER_MODEL=image-model",
                    "IMAGE_PROVIDER_API_KEY_1=sk-image-1",
                    "IMAGE_PROVIDER_API_KEY_2=sk-image-2",
                    "IMAGE_PROVIDER_API_KEY_3=sk-image-3",
                    "IMAGE_PROVIDER_API_KEY_4=sk-image-4",
                    "IMAGE_PROVIDER_CONCURRENCY_PER_KEY=10",
                    "IMAGE_PROVIDER_TOTAL_CONCURRENCY=40",
                ]);

            var configuration = await ProviderEnvironmentConfiguration.FromDotEnvFileAsync(
                envPath,
                CancellationToken.None);

            Assert.Empty(configuration.Validate());
            Assert.Equal("gpt-5.5", configuration.Text.Model);
            Assert.Equal(4, configuration.Image.ApiKeySecretNames.Count);
            Assert.Equal(40, configuration.Image.TotalConcurrency);
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
    public void ProviderEnvironmentConfiguration_ReportsMissingModelsAndConcurrencyMismatch()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://text.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-text",
                ["TEXT_PROVIDER_MODEL"] = "",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://image.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "image-model",
                ["IMAGE_PROVIDER_API_KEY_1"] = "sk-image-1",
                ["IMAGE_PROVIDER_API_KEY_2"] = "sk-image-2",
                ["IMAGE_PROVIDER_CONCURRENCY_PER_KEY"] = "10",
                ["IMAGE_PROVIDER_TOTAL_CONCURRENCY"] = "30",
            });

        var errors = configuration.Validate();

        Assert.Contains(errors, error => error.Contains("Text provider model", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("total concurrency", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void OpenAiProviderOptions_CreatesSeparateTextAndImageOptionsFromProviderEnvironment()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://text.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-text",
                ["TEXT_PROVIDER_MODEL"] = "gpt-5.5",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://image.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "image-model",
                ["IMAGE_PROVIDER_API_KEY_1"] = "sk-image-1",
                ["IMAGE_PROVIDER_API_KEY_2"] = "sk-image-2",
                ["IMAGE_PROVIDER_CONCURRENCY_PER_KEY"] = "10",
                ["IMAGE_PROVIDER_TOTAL_CONCURRENCY"] = "20",
            });

        var textOptions = OpenAiProviderOptions.FromTextProviderEnvironment(configuration, realApiEnabled: true);
        var imageOptions = OpenAiProviderOptions.FromImageProviderEnvironment(configuration, realApiEnabled: true);

        Assert.Equal("https://text.example/v1", textOptions.BaseUri.ToString().TrimEnd('/'));
        Assert.Equal("TEXT_PROVIDER_API_KEY", textOptions.ApiKeySecretName);
        Assert.Equal("gpt-5.5", textOptions.TextPlanningModel);
        Assert.Equal("https://image.example/v1", imageOptions.BaseUri.ToString().TrimEnd('/'));
        Assert.Equal("IMAGE_PROVIDER_API_KEY_1", imageOptions.ApiKeySecretName);
        Assert.Equal("image-model", imageOptions.ImageGenerationModel);
        Assert.True(textOptions.RealApiEnabled);
        Assert.True(imageOptions.RealApiEnabled);
        Assert.True(textOptions.AllowedOperations.HasFlag(OpenAiProviderOperation.TextPlanning));
        Assert.True(textOptions.AllowedOperations.HasFlag(OpenAiProviderOperation.VisionReview));
        Assert.False(textOptions.AllowedOperations.HasFlag(OpenAiProviderOperation.ImageGeneration));
        Assert.True(imageOptions.AllowedOperations.HasFlag(OpenAiProviderOperation.ImageGeneration));
        Assert.False(imageOptions.AllowedOperations.HasFlag(OpenAiProviderOperation.TextPlanning));
        Assert.False(imageOptions.AllowedOperations.HasFlag(OpenAiProviderOperation.VisionReview));
    }

    [Fact]
    public void OpenAiProviders_FailClosedWhenOptionsAreUsedForWrongOperation()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://text.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-text",
                ["TEXT_PROVIDER_MODEL"] = "gpt-5.5",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://image.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "image-model",
                ["IMAGE_PROVIDER_API_KEY_1"] = "sk-image",
            });
        var textOptions = OpenAiProviderOptions.FromTextProviderEnvironment(configuration, realApiEnabled: true);
        var imageOptions = OpenAiProviderOptions.FromImageProviderEnvironment(configuration, realApiEnabled: true);

        var textException = Assert.Throws<InvalidOperationException>(() =>
            new OpenAiTextPlanningProvider(new HttpClient(), imageOptions, new StaticSecretStore("image-secret")));
        var visionException = Assert.Throws<InvalidOperationException>(() =>
            new OpenAiVisionReviewProvider(new HttpClient(), imageOptions, new StaticSecretStore("image-secret")));
        var imageException = Assert.Throws<InvalidOperationException>(() =>
            new OpenAiImageGenerationProvider(new HttpClient(), textOptions, new StaticSecretStore("text-secret")));

        Assert.Contains("TextPlanning", textException.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("VisionReview", visionException.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ImageGeneration", imageException.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OpenAiProviderGuard_RejectsPurposePrefixedSecretNamesForWrongOperation()
    {
        var imageKeyOptions = new OpenAiProviderOptions
        {
            ApiKeySecretName = "IMAGE_PROVIDER_API_KEY_1",
            AllowedOperations = OpenAiProviderOperation.All,
        };
        var textKeyOptions = new OpenAiProviderOptions
        {
            ApiKeySecretName = "TEXT_PROVIDER_API_KEY",
            AllowedOperations = OpenAiProviderOperation.All,
        };

        var textException = Assert.Throws<InvalidOperationException>(() =>
            OpenAiProviderGuard.EnsureAllowsOperation(imageKeyOptions, OpenAiProviderOperation.TextPlanning));
        var visionException = Assert.Throws<InvalidOperationException>(() =>
            OpenAiProviderGuard.EnsureAllowsOperation(imageKeyOptions, OpenAiProviderOperation.VisionReview));
        var imageException = Assert.Throws<InvalidOperationException>(() =>
            OpenAiProviderGuard.EnsureAllowsOperation(textKeyOptions, OpenAiProviderOperation.ImageGeneration));

        Assert.Contains("IMAGE_PROVIDER_API_KEY", textException.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("IMAGE_PROVIDER_API_KEY", visionException.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TEXT_PROVIDER_API_KEY", imageException.Message, StringComparison.OrdinalIgnoreCase);
    }

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
        Assert.NotNull(provider.GetRequiredService<OpenAiSdkClientFactory>());
    }

    [Fact]
    public async Task OpenAiSdkClientFactory_CreatesResponsesClientWithCustomEndpointAfterGuard()
    {
        var options = new OpenAiProviderOptions
        {
            BaseUri = new Uri("https://sdk-text.example/v1/"),
            ApiKeySecretName = "TEXT_PROVIDER_API_KEY",
            TextPlanningModel = "gpt-5.5",
            ImageGenerationModel = string.Empty,
            AllowedOperations = OpenAiProviderOperation.TextPlanning | OpenAiProviderOperation.VisionReview,
            RealApiEnabled = true,
        };
        var factory = new OpenAiSdkClientFactory(new StaticSecretStore("sdk-text-key"));

        var client = await factory.CreateResponsesClientAsync(
            options,
            OpenAiProviderOperation.TextPlanning,
            CancellationToken.None);

        Assert.Equal("https://sdk-text.example/v1/", client.Endpoint.ToString());
    }

    [Fact]
    public async Task OpenAiSdkClientFactory_CreatesImageClientWithCustomEndpointAndModelAfterGuard()
    {
        var options = new OpenAiProviderOptions
        {
            BaseUri = new Uri("https://sdk-image.example/v1/"),
            ApiKeySecretName = "IMAGE_PROVIDER_API_KEY_1",
            TextPlanningModel = string.Empty,
            ImageGenerationModel = "gpt-image-test",
            VisionReviewModel = string.Empty,
            AllowedOperations = OpenAiProviderOperation.ImageGeneration,
            RealApiEnabled = true,
        };
        var factory = new OpenAiSdkClientFactory(new StaticSecretStore("sdk-image-key"));

        var client = await factory.CreateImageClientAsync(options, CancellationToken.None);

        Assert.Equal("https://sdk-image.example/v1/", client.Endpoint.ToString());
        Assert.Equal("gpt-image-test", client.Model);
    }

    [Fact]
    public async Task OpenAiSdkClientFactory_UsesExistingReadinessAndRoleGuards()
    {
        var options = new OpenAiProviderOptions
        {
            ApiKeySecretName = "IMAGE_PROVIDER_API_KEY_1",
            AllowedOperations = OpenAiProviderOperation.All,
            RealApiEnabled = true,
        };
        var factory = new OpenAiSdkClientFactory(new StaticSecretStore("sdk-image-key"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            factory.CreateResponsesClientAsync(options, OpenAiProviderOperation.TextPlanning, CancellationToken.None));

        Assert.Contains("IMAGE_PROVIDER_API_KEY", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OpenAiSdkResponseOptionsFactory_UsesLockedTextPlanningDefaults()
    {
        var options = new OpenAiProviderOptions
        {
            TextPlanningModel = "gpt-5.5",
        };
        var request = new PlanningRequest(
            "Create a three-part poster series",
            "Physics teachers",
            3,
            "Clean educational diagrams");

        var sdkOptions = OpenAiSdkResponseOptionsFactory.CreateTextPlanningOptions(options, request);

        Assert.Equal("gpt-5.5", sdkOptions.Model);
        Assert.Equal(OpenAiTextPlanningRequestMapper.Instructions, sdkOptions.Instructions);
        Assert.False(sdkOptions.StoredOutputEnabled);
        Assert.Single(sdkOptions.InputItems);
        Assert.NotNull(sdkOptions.TextOptions.TextFormat);
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
#pragma warning restore OPENAI001
