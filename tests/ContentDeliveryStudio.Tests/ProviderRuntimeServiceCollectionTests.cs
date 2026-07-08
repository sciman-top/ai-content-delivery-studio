using ContentDeliveryStudio.App.Services;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Infrastructure.Fakes;
using ContentDeliveryStudio.Infrastructure.OpenAI;
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
            Assert.IsType<FakeImageGenerationProvider>(provider.GetRequiredService<IImageEditProvider>());
            Assert.IsType<FailoverVisionReviewProvider>(provider.GetRequiredService<IVisionReviewProvider>());
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
}
