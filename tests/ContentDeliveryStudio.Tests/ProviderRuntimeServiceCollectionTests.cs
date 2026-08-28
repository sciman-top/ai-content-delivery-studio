using ContentDeliveryStudio.App.Services;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Infrastructure.Fakes;
using ContentDeliveryStudio.Infrastructure.OpenAI;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;
using Microsoft.Extensions.DependencyInjection;

namespace ContentDeliveryStudio.Tests;

public sealed class ProviderRuntimeServiceCollectionTests
{
    [Fact]
    public void AddContentDeliveryStudioProviderRuntime_UsesFakeProvidersByDefault()
    {
        var services = new ServiceCollection();

        services.AddContentDeliveryStudioProviderRuntime(new ProviderRuntimeRegistrationOptions());

        using var provider = services.BuildServiceProvider();
        Assert.IsType<FakeTextPlanningProvider>(provider.GetRequiredService<ITextPlanningProvider>());
        Assert.IsType<FakeImageGenerationProvider>(provider.GetRequiredService<IImageGenerationProvider>());
        Assert.IsType<FakeImageGenerationProvider>(provider.GetRequiredService<IImageEditProvider>());
        Assert.IsType<FakeVisionReviewProvider>(provider.GetRequiredService<IVisionReviewProvider>());
        Assert.IsType<FakeScientificUnderstandingProvider>(
            provider.GetRequiredService<IScientificUnderstandingProvider>());
        Assert.IsType<FakeScientificSemanticReviewProvider>(
            provider.GetRequiredService<IScientificSemanticReviewProvider>());
        Assert.IsType<FakeScientificVisualReviewProvider>(
            provider.GetRequiredService<IScientificVisualReviewProvider>());
    }

    [Fact]
    public void AddContentDeliveryStudioProviderRuntime_UsesLiveFailoverProvidersWhenOptedIn()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        var envPath = Path.Combine(directory, ".env");
        Directory.CreateDirectory(directory);

        try
        {
            File.WriteAllLines(
                envPath,
                [
                    "TEXT_PROVIDER_BASE_URL=https://input.example/v1",
                    "TEXT_PROVIDER_API_KEY=sk-input",
                    "TEXT_PROVIDER_MODEL=gpt-5.5",
                    "TEXT_PROVIDER_FALLBACK_1_BASE_URL=https://ciii.example/v1",
                    "TEXT_PROVIDER_FALLBACK_1_API_KEY=sk-ciii",
                    "TEXT_PROVIDER_FALLBACK_1_MODEL=gpt-5.5",
                    "IMAGE_PROVIDER_BASE_URL=https://input.example/v1",
                    "IMAGE_PROVIDER_MODEL=gpt-image-2",
                    "IMAGE_PROVIDER_IMAGE_SURFACE=responses",
                    "IMAGE_PROVIDER_RESPONSES_MODEL=gpt-5.5",
                    "IMAGE_PROVIDER_API_KEY_1=sk-input",
                    "IMAGE_PROVIDER_FALLBACK_1_BASE_URL=https://ciii.example/v1",
                    "IMAGE_PROVIDER_FALLBACK_1_MODEL=gpt-image-2",
                    "IMAGE_PROVIDER_FALLBACK_1_IMAGE_SURFACE=images",
                    "IMAGE_PROVIDER_FALLBACK_1_API_KEY_1=sk-ciii",
                ]);
            var services = new ServiceCollection();

            services.AddContentDeliveryStudioProviderRuntime(
                new ProviderRuntimeRegistrationOptions("live", envPath));

            using var provider = services.BuildServiceProvider();
            Assert.IsType<FailoverTextPlanningProvider>(provider.GetRequiredService<ITextPlanningProvider>());
            Assert.IsType<FailoverImageGenerationProvider>(provider.GetRequiredService<IImageGenerationProvider>());
            var imageEditProvider = Assert.IsType<OpenAiImageEditProvider>(
                provider.GetRequiredService<IImageEditProvider>());
            Assert.Equal("openai-image-edit", imageEditProvider.Capabilities.ProviderId);
            Assert.True(imageEditProvider.Capabilities.SupportsMaskEditing);
            Assert.IsType<FailoverVisionReviewProvider>(provider.GetRequiredService<IVisionReviewProvider>());
            Assert.IsType<OpenAiScientificUnderstandingProvider>(
                provider.GetRequiredService<IScientificUnderstandingProvider>());
            Assert.IsType<OpenAiScientificReviewProvider>(
                provider.GetRequiredService<IScientificSemanticReviewProvider>());
            Assert.IsType<OpenAiScientificReviewProvider>(
                provider.GetRequiredService<IScientificVisualReviewProvider>());
            Assert.IsType<JsonOpenAiScientificReviewCheckpointStore>(
                provider.GetRequiredService<IOpenAiScientificReviewCheckpointStore>());
            var scheduler = Assert.IsType<OpenAiExecutionSlotScheduler>(
                provider.GetRequiredService<IOpenAiExecutionSlotScheduler>());
            Assert.Equal(5, scheduler.TotalConcurrency);
            Assert.IsType<OpenAiActivePresetSetState>(provider.GetRequiredService<IOpenAiActivePresetSetState>());
            Assert.IsType<OpenAiModelAvailabilityProbe>(provider.GetRequiredService<IOpenAiModelAvailabilityProbe>());
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
    public void AddContentDeliveryStudioProviderRuntime_FailsClosedWhenLiveEnvFileIsMissing()
    {
        var services = new ServiceCollection();
        var missingEnvPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), ".env");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddContentDeliveryStudioProviderRuntime(
                new ProviderRuntimeRegistrationOptions("live", missingEnvPath)));

        Assert.Contains("live provider mode", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".env", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveSecretStore_DefaultsToDotEnvStore()
    {
        var envPath = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", ".env");

        var store = ProviderRuntimeServiceCollectionExtensions.ResolveSecretStore(
            new ProviderRuntimeRegistrationOptions(),
            envPath);

        var dotEnvStore = Assert.IsType<DotEnvSecretStore>(store);
        Assert.Equal(envPath, dotEnvStore.EnvPath);
    }

    [Fact]
    public void ResolveSecretStore_DpapiOptInChecksDpapiBeforeDotEnvFallback()
    {
        var envPath = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", ".env");

        var store = ProviderRuntimeServiceCollectionExtensions.ResolveSecretStore(
            new ProviderRuntimeRegistrationOptions(SecretStore: "dpapi"),
            envPath);

        var composite = Assert.IsType<CompositeOpenAiSecretStore>(store);
        Assert.Equal(2, composite.Stores.Count);
        Assert.IsType<DpapiOpenAiSecretStore>(composite.Stores[0]);
        var fallback = Assert.IsType<DotEnvSecretStore>(composite.Stores[1]);
        Assert.Equal(envPath, fallback.EnvPath);
    }

    [Fact]
    public void ResolveSecretStore_FailsClosedForUnknownStoreName()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProviderRuntimeServiceCollectionExtensions.ResolveSecretStore(
                new ProviderRuntimeRegistrationOptions(SecretStore: "keychain"),
                envPath: "unused.env"));

        Assert.Contains("PROVIDER_SECRET_STORE", exception.Message, StringComparison.Ordinal);
        Assert.Contains("dotenv", exception.Message, StringComparison.Ordinal);
        Assert.Contains("dpapi", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddContentDeliveryStudioProviderRuntime_DpapiSecretStoreOptInRegistersCompositeStore()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        var envPath = Path.Combine(directory, ".env");
        Directory.CreateDirectory(directory);

        try
        {
            File.WriteAllLines(
                envPath,
                [
                    "TEXT_PROVIDER_BASE_URL=https://input.example/v1",
                    "TEXT_PROVIDER_API_KEY=sk-input",
                    "TEXT_PROVIDER_MODEL=gpt-5.5",
                    "IMAGE_PROVIDER_BASE_URL=https://input.example/v1",
                    "IMAGE_PROVIDER_MODEL=gpt-image-2",
                    "IMAGE_PROVIDER_API_KEY_1=sk-input",
                ]);
            var services = new ServiceCollection();

            services.AddContentDeliveryStudioProviderRuntime(
                new ProviderRuntimeRegistrationOptions("live", envPath, SecretStore: "dpapi"));

            using var provider = services.BuildServiceProvider();
            var composite = Assert.IsType<CompositeOpenAiSecretStore>(
                provider.GetRequiredService<IOpenAiSecretStore>());
            Assert.IsType<DpapiOpenAiSecretStore>(composite.Stores[0]);
            // Single-endpoint profile resolves without the failover wrapper; the
            // assertion proves the dpapi store feeds the live provider pipeline.
            Assert.IsType<OpenAiTextPlanningProvider>(provider.GetRequiredService<ITextPlanningProvider>());
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
