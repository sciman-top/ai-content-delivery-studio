using System.Net;
using ContentDeliveryStudio.App.Services;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Infrastructure.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ContentDeliveryStudio.Tests;

public sealed class OpenAiProviderConfigurationTests
{
    [Fact]
    public void OpenAiProviderOptions_DefaultsToGpt56SolWithMediumReasoning()
    {
        var options = new OpenAiProviderOptions();

        Assert.Equal("gpt-5.6-sol", options.TextPlanningModel);
        Assert.Equal("gpt-5.6-sol", options.VisionReviewModel);
        Assert.Equal("medium", options.ReasoningEffort);
        Assert.Empty(options.Validate());
    }

    [Fact]
    public async Task DotEnvSecretStore_ReadsDotEnvValuesWithoutPersistingSecrets()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
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
        Assert.False(configuration.Image.UsesSharedTextApiKeyFallback);
        Assert.Equal(10, configuration.Image.ConcurrencyPerKey);
        Assert.Equal(40, configuration.Image.TotalConcurrency);
        Assert.Equal("IMAGE_PROVIDER_APP_ID", configuration.Image.AppIdSecretName);
        Assert.Equal("IMAGE_PROVIDER_APP_SECRET", configuration.Image.AppSecretSecretName);
    }

    [Theory]
    [InlineData(TextProviderModelPresets.AstraHigh, "gpt-6-astra", "high")]
    [InlineData(TextProviderModelPresets.AstraMedium, "gpt-6-astra", "medium")]
    [InlineData(TextProviderModelPresets.AstraLow, "gpt-6-astra", "low")]
    [InlineData(TextProviderModelPresets.SolHigh, "gpt-5.6-sol", "high")]
    [InlineData(TextProviderModelPresets.SolMedium, "gpt-5.6-sol", "medium")]
    [InlineData(TextProviderModelPresets.SolLow, "gpt-5.6-sol", "low")]
    [InlineData(TextProviderModelPresets.TerraMax, "gpt-5.6-terra", "max")]
    [InlineData(TextProviderModelPresets.TerraXHigh, "gpt-5.6-terra", "xhigh")]
    [InlineData(TextProviderModelPresets.TerraHigh, "gpt-5.6-terra", "high")]
    [InlineData(TextProviderModelPresets.LunaMax, "gpt-5.6-luna", "max")]
    [InlineData(TextProviderModelPresets.LunaXHigh, "gpt-5.6-luna", "xhigh")]
    [InlineData(TextProviderModelPresets.LunaHigh, "gpt-5.6-luna", "high")]
    [InlineData(TextProviderModelPresets.GlmFlashMax, "glm-5.3-flash", "max")]
    [InlineData(TextProviderModelPresets.GlmFlashHigh, "glm-5.3-flash", "high")]
    [InlineData(TextProviderModelPresets.GlmFlashLow, "glm-5.3-flash", "low")]
    [InlineData(TextProviderModelPresets.DeepSeekV41FlashMax, "deepseek-v4.1-flash", "max")]
    [InlineData(TextProviderModelPresets.DeepSeekV41FlashHigh, "deepseek-v4.1-flash", "high")]
    public void ProviderEnvironmentConfiguration_ResolvesSupportedTextProviderPreset(
        string preset,
        string expectedModel,
        string expectedReasoningEffort)
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-text",
                ["TEXT_PROVIDER_PRESET"] = preset,
                ["TEXT_PROVIDER_MODEL"] = "legacy-model",
                ["TEXT_PROVIDER_REASONING_EFFORT"] = "low",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "gpt-image-2",
            });

        Assert.Empty(configuration.Validate());
        Assert.Equal(preset, configuration.Text.ModelPreset);
        Assert.Equal(expectedModel, configuration.Text.Model);
        Assert.Equal(expectedReasoningEffort, configuration.Text.ReasoningEffort);
    }

    [Fact]
    public void ProviderEnvironmentConfiguration_RejectsUnknownTextProviderPreset()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-text",
                ["TEXT_PROVIDER_PRESET"] = "unknown-tier",
                ["TEXT_PROVIDER_MODEL"] = "gpt-5.6-sol",
                ["TEXT_PROVIDER_REASONING_EFFORT"] = "xhigh",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "gpt-image-2",
            });

        Assert.Contains(
                configuration.Validate(),
            error => error.Contains("unknown-tier", StringComparison.Ordinal)
                && error.Contains(TextProviderModelPresets.LunaHigh, StringComparison.Ordinal));
    }

    [Fact]
    public void ProviderEnvironmentConfiguration_LoadsAutoRoutingAndProjectsItToRuntimeOptions()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-text",
                ["TEXT_PROVIDER_ROUTING_MODE"] = TextProviderRoutingModes.Auto,
                ["TEXT_PROVIDER_PRESET"] = TextProviderModelPresets.SolHigh,
                ["IMAGE_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "gpt-image-2",
            });

        var options = OpenAiProviderOptions.FromTextProviderEnvironment(configuration);

        Assert.Empty(configuration.Validate());
        Assert.Equal(TextProviderRoutingModes.Auto, configuration.Text.RoutingMode);
        Assert.Equal(OpenAiTextRoutingMode.Auto, options.TextRoutingMode);
    }

    [Fact]
    public void ProviderEnvironmentConfiguration_ProjectsOptInPreferredSolRecoveryForAutoRouting()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-text",
                ["TEXT_PROVIDER_ROUTING_MODE"] = TextProviderRoutingModes.Auto,
                ["TEXT_PROVIDER_PRESET_SET"] = TextProviderModelPresetSets.TerraOnly,
                ["TEXT_PROVIDER_QUALITY_TIER"] = "deep",
                ["TEXT_PROVIDER_RECOVERY_MODE"] = TextProviderRecoveryModes.PreferSol,
                ["IMAGE_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "gpt-image-2",
            });

        var options = OpenAiProviderOptions.FromTextProviderEnvironment(configuration);

        Assert.Empty(configuration.Validate());
        Assert.Equal(OpenAiPresetRecoveryMode.PreferSol, options.PresetRecoveryMode);
    }

    [Fact]
    public void ProviderEnvironmentConfiguration_RejectsPreferredSolRecoveryForFixedRouting()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-text",
                ["TEXT_PROVIDER_ROUTING_MODE"] = TextProviderRoutingModes.Fixed,
                ["TEXT_PROVIDER_RECOVERY_MODE"] = TextProviderRecoveryModes.PreferSol,
                ["IMAGE_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "gpt-image-2",
            });

        Assert.Contains(
            configuration.Validate(),
            error => error.Contains("requires routing mode 'auto'", StringComparison.Ordinal));
    }

    [Fact]
    public void ProviderEnvironmentConfiguration_ResolvesOneFamilyOnlyPresetSetAndQualityTier()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-text",
                ["TEXT_PROVIDER_PRESET_SET"] = TextProviderModelPresetSets.TerraOnly,
                ["TEXT_PROVIDER_QUALITY_TIER"] = "balanced",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "gpt-image-2",
            });

        Assert.Empty(configuration.Validate());
        Assert.Equal(TextProviderModelPresetSets.TerraOnly, configuration.Text.ModelPresetSet);
        Assert.Equal("balanced", configuration.Text.QualityTier);
        Assert.Equal(TextProviderModelPresets.TerraXHigh, configuration.Text.ModelPreset);
        Assert.Equal(
            TextProviderModelPresetSets.TerraOnly,
            OpenAiProviderOptions.FromTextProviderEnvironment(configuration).InitialPresetSet);
        Assert.Equal("gpt-5.6-terra", configuration.Text.Model);
        Assert.Equal("xhigh", configuration.Text.ReasoningEffort);
    }

    [Fact]
    public void ProviderEnvironmentConfiguration_ResolvesAstraOnlyPresetSetAndQualityTier()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-text",
                ["TEXT_PROVIDER_PRESET_SET"] = TextProviderModelPresetSets.AstraOnly,
                ["TEXT_PROVIDER_QUALITY_TIER"] = "fast",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "gpt-image-2",
            });

        Assert.Empty(configuration.Validate());
        Assert.Equal(TextProviderModelPresetSets.AstraOnly, configuration.Text.ModelPresetSet);
        Assert.Equal("fast", configuration.Text.QualityTier);
        Assert.Equal(TextProviderModelPresets.AstraLow, configuration.Text.ModelPreset);
        Assert.Equal(
            TextProviderModelPresetSets.AstraOnly,
            OpenAiProviderOptions.FromTextProviderEnvironment(configuration).InitialPresetSet);
        Assert.Equal("gpt-6-astra", configuration.Text.Model);
        Assert.Equal("low", configuration.Text.ReasoningEffort);
    }

    [Fact]
    public void ProviderEnvironmentConfiguration_RejectsModelThatMixesAOneFamilyOnlyPresetSet()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-text",
                ["TEXT_PROVIDER_PRESET_SET"] = TextProviderModelPresetSets.SolOnly,
                ["TEXT_PROVIDER_QUALITY_TIER"] = "deep",
                ["TEXT_PROVIDER_MODEL"] = "gpt-5.6-terra",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "gpt-image-2",
            });

        Assert.Contains(
            configuration.Validate(),
            error => error.Contains("mixes preset set", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ProviderEnvironmentConfiguration_RejectsSameFamilyEffortThatDoesNotMatchDeepSeekTier()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-text",
                ["TEXT_PROVIDER_PRESET_SET"] = TextProviderModelPresetSets.DeepSeekV41Only,
                ["TEXT_PROVIDER_QUALITY_TIER"] = "deep",
                ["TEXT_PROVIDER_MODEL"] = "deepseek-v4.1-flash",
                ["TEXT_PROVIDER_REASONING_EFFORT"] = "high",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "gpt-image-2",
            });

        Assert.Contains(
            configuration.Validate(),
            error => error.Contains("requires reasoning effort 'max'", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TextProviderModelPresets_RepeatsDeepSeekHighEffortAcrossBalancedAndFastTiers()
    {
        Assert.True(TextProviderModelPresets.TryGetQualityTierForModel(
            "deepseek-v4.1-flash",
            "max",
            out var maxTier));
        Assert.True(TextProviderModelPresets.TryGetQualityTierForModel(
            "deepseek-v4.1-flash",
            "high",
            out var highTier));

        Assert.Equal(OpenAiExecutionQualityTier.Deep, maxTier);
        Assert.Equal(OpenAiExecutionQualityTier.Fast, highTier);

        foreach (var tier in new[]
                 {
                     OpenAiExecutionQualityTier.Balanced,
                     OpenAiExecutionQualityTier.Fast,
                 })
        {
            Assert.True(TextProviderModelPresets.TryResolveForFamily(
                TextProviderModelPresets.DeepSeekV41Family,
                tier,
                out var preset,
                out var model,
                out var reasoningEffort));
            Assert.Equal(TextProviderModelPresets.DeepSeekV41FlashHigh, preset);
            Assert.Equal("deepseek-v4.1-flash", model);
            Assert.Equal("high", reasoningEffort);
        }
    }

    [Fact]
    public void ProviderEnvironmentConfiguration_RejectsUnknownTextRoutingMode()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-text",
                ["TEXT_PROVIDER_MODEL"] = "gpt-5.6-sol",
                ["TEXT_PROVIDER_ROUTING_MODE"] = "adaptive-magic",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "gpt-image-2",
            });

        Assert.Contains(
            configuration.Validate(),
            error => error.Contains("adaptive-magic", StringComparison.Ordinal)
                && error.Contains(TextProviderRoutingModes.Fixed, StringComparison.Ordinal));
        Assert.Throws<InvalidOperationException>(() =>
            OpenAiProviderOptions.FromTextProviderEnvironment(configuration));
    }

    [Fact]
    public void ProviderEnvironmentConfiguration_DefaultsPrimaryAndFallbackRoutingToFixed()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://primary.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-primary",
                ["TEXT_PROVIDER_MODEL"] = "gpt-5.6-sol",
                ["TEXT_PROVIDER_FALLBACK_1_BASE_URL"] = "https://backup.example/v1",
                ["TEXT_PROVIDER_FALLBACK_1_API_KEY"] = "sk-backup",
                ["TEXT_PROVIDER_FALLBACK_1_MODEL"] = "gpt-5.6-sol",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://primary.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "gpt-image-2",
            });

        Assert.Equal(TextProviderRoutingModes.Fixed, configuration.Text.RoutingMode);
        Assert.Equal(TextProviderRoutingModes.Fixed, Assert.Single(configuration.TextFallbacks).RoutingMode);
    }

    [Fact]
    public void ProviderEnvironmentConfiguration_FallsBackToTextApiKeyWhenImageKeyIsMissing()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://text.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-shared",
                ["TEXT_PROVIDER_MODEL"] = "gpt-5.5",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://image.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "gpt-image-2",
            });

        Assert.Empty(configuration.Validate());
        Assert.Equal("TEXT_PROVIDER_API_KEY", configuration.Image.ApiKeySecretName);
        Assert.Equal(["TEXT_PROVIDER_API_KEY"], configuration.Image.ApiKeySecretNames);
        Assert.True(configuration.Image.UsesSharedTextApiKeyFallback);
        Assert.Equal(1, configuration.Image.TotalConcurrency);
    }

    [Fact]
    public void ProviderEnvironmentConfiguration_PrefersExplicitImageKeyOverSharedTextKey()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://text.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-shared",
                ["TEXT_PROVIDER_MODEL"] = "gpt-5.5",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://image.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "gpt-image-2",
                ["IMAGE_PROVIDER_API_KEY_1"] = "sk-image-1",
            });

        Assert.Equal("IMAGE_PROVIDER_API_KEY_1", configuration.Image.ApiKeySecretName);
        Assert.Equal(["IMAGE_PROVIDER_API_KEY_1"], configuration.Image.ApiKeySecretNames);
        Assert.False(configuration.Image.UsesSharedTextApiKeyFallback);
    }

    [Fact]
    public void ProviderEnvironmentConfiguration_LoadsTextAndImageFallbackProfiles()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://primary.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-primary",
                ["TEXT_PROVIDER_MODEL"] = "gpt-5.5",
                ["TEXT_PROVIDER_FALLBACK_1_BASE_URL"] = "https://backup.example/v1",
                ["TEXT_PROVIDER_FALLBACK_1_API_KEY"] = "sk-backup",
                ["TEXT_PROVIDER_FALLBACK_1_MODEL"] = "gpt-5.5",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://primary.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "gpt-image-2",
                ["IMAGE_PROVIDER_IMAGE_SURFACE"] = "responses",
                ["IMAGE_PROVIDER_RESPONSES_MODEL"] = "gpt-5.5",
                ["IMAGE_PROVIDER_API_KEY_1"] = "sk-primary-image",
                ["IMAGE_PROVIDER_FALLBACK_1_BASE_URL"] = "https://backup.example/v1",
                ["IMAGE_PROVIDER_FALLBACK_1_MODEL"] = "gpt-image-2",
                ["IMAGE_PROVIDER_FALLBACK_1_IMAGE_SURFACE"] = "images",
                ["IMAGE_PROVIDER_FALLBACK_1_API_KEY_1"] = "sk-backup-image",
            });

        Assert.Empty(configuration.Validate());
        Assert.Single(configuration.TextFallbacks);
        Assert.Single(configuration.ImageFallbacks);
        Assert.Equal("TEXT_PROVIDER_FALLBACK_1", configuration.TextFallbacks[0].Prefix);
        Assert.Equal("https://backup.example/v1", configuration.TextFallbacks[0].BaseUri!.ToString().TrimEnd('/'));
        Assert.Equal("TEXT_PROVIDER_FALLBACK_1_API_KEY", configuration.TextFallbacks[0].ApiKeySecretName);
        Assert.Equal(ProviderImageGenerationSurface.Responses, configuration.Image.ImageGenerationSurface);
        Assert.Equal("gpt-5.5", configuration.Image.ResponsesModel);
        Assert.Equal(ProviderImageGenerationSurface.Images, configuration.ImageFallbacks[0].ImageGenerationSurface);
        Assert.Equal("IMAGE_PROVIDER_FALLBACK_1_API_KEY_1", configuration.ImageFallbacks[0].ApiKeySecretName);
    }

    [Fact]
    public async Task ProviderEnvironmentConfiguration_LoadsProviderProfilesFromDotEnvFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
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
        Assert.False(imageOptions.UsesSharedTextApiKeyFallback);
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
    public void OpenAiProviderOptions_UsesSharedTextKeyForImageProviderWhenNoImageKeyIsConfigured()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-shared",
                ["TEXT_PROVIDER_MODEL"] = "gpt-5.5",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "gpt-image-2",
            });

        var imageOptions = OpenAiProviderOptions.FromImageProviderEnvironment(configuration, realApiEnabled: true);

        Assert.Equal("TEXT_PROVIDER_API_KEY", imageOptions.ApiKeySecretName);
        Assert.True(imageOptions.UsesSharedTextApiKeyFallback);
    }

    [Fact]
    public void OpenAiProviderOptions_DefaultsStatefulImageGenerationToFailClosed()
    {
        var options = new OpenAiProviderOptions();

        Assert.Null(options.ImageGenerationResponsesModel);
        Assert.False(options.ImageGenerationAllowsResponsesState);
        Assert.False(options.ImageGenerationUsesResponsesByDefault);
    }

    [Fact]
    public void OpenAiProviderOptions_FromImageProviderEnvironment_EnablesStatefulImageGenerationOnlyWhenResponsesModelExists()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-shared",
                ["TEXT_PROVIDER_MODEL"] = "gpt-5.5",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "gpt-image-2",
                ["IMAGE_PROVIDER_RESPONSES_MODEL"] = "gpt-5.5",
            });

        var imageOptions = OpenAiProviderOptions.FromImageProviderEnvironment(configuration, realApiEnabled: true);

        Assert.Equal("gpt-5.5", imageOptions.ImageGenerationResponsesModel);
        Assert.True(imageOptions.ImageGenerationAllowsResponsesState);
    }

    [Fact]
    public void OpenAiProviderOptions_FromImageProviderEnvironment_UsesResponsesByDefaultWhenConfigured()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-shared",
                ["TEXT_PROVIDER_MODEL"] = "gpt-5.5",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "gpt-image-2",
                ["IMAGE_PROVIDER_RESPONSES_MODEL"] = "gpt-5.5",
                ["IMAGE_PROVIDER_IMAGE_SURFACE"] = "responses",
            });

        var imageOptions = OpenAiProviderOptions.FromImageProviderEnvironment(configuration, realApiEnabled: true);

        Assert.True(imageOptions.ImageGenerationUsesResponsesByDefault);
        Assert.True(imageOptions.ImageGenerationAllowsResponsesState);
        Assert.Equal("gpt-5.5", imageOptions.ImageGenerationResponsesModel);
    }

    [Fact]
    public void OpenAiProviderOptions_RejectsEnabledStatefulImageGenerationWithoutResponsesModel()
    {
        var options = new OpenAiProviderOptions
        {
            ImageGenerationAllowsResponsesState = true,
            ImageGenerationResponsesModel = null,
        };

        var errors = options.Validate();

        Assert.Contains(errors, error => error.Contains("Responses image generation model", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void OpenAiProviderOptions_DefaultsVisionReviewToStatelessBoundedRequests()
    {
        var options = new OpenAiProviderOptions();

        Assert.Equal(VisionReviewExecutionPolicy.DefaultBatchItemLimit, options.VisionReviewBatchItemLimit);
        Assert.Equal(VisionReviewExecutionPolicy.DefaultHighRiskBatchItemLimit, options.HighRiskVisionReviewBatchItemLimit);
        Assert.False(options.VisionReviewUsesStoredResponses);
        Assert.False(options.VisionReviewAllowsPreviousResponseId);
    }

    [Fact]
    public void OpenAiProviderOptions_RejectsPreviousResponseIdsWithoutStoredResponses()
    {
        var options = new OpenAiProviderOptions
        {
            VisionReviewUsesStoredResponses = false,
            VisionReviewAllowsPreviousResponseId = true,
        };

        var errors = options.Validate();

        Assert.Contains(errors, error => error.Contains("previous response", StringComparison.OrdinalIgnoreCase));
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
    public void OpenAiProviderGuard_AllowsImageGenerationWhenImageProviderFallsBackToTextKey()
    {
        var fallbackImageOptions = new OpenAiProviderOptions
        {
            ApiKeySecretName = "TEXT_PROVIDER_API_KEY",
            UsesSharedTextApiKeyFallback = true,
            AllowedOperations = OpenAiProviderOperation.ImageGeneration,
        };

        OpenAiProviderGuard.EnsureAllowsOperation(fallbackImageOptions, OpenAiProviderOperation.ImageGeneration);
    }

    [Fact]
    public void Defaults_KeepRealApiDisabledAndValidateCleanly()
    {
        var options = new OpenAiProviderOptions();

        Assert.False(options.RealApiEnabled);
        Assert.Equal(OpenAiGatewayDefaults.CockpitLocalApiBaseUri, options.BaseUri);
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

        var directory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));

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
            Assert.DoesNotContain(
                Directory.GetFiles(directory),
                path => path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase));

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

        var directory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));

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
        // The standard resilience pipeline owns the timeouts, so the factory
        // forces the raw client timeout to infinite; the bounded values are
        // asserted directly against the shared configuration routine.
        Assert.Equal(Timeout.InfiniteTimeSpan, client.Timeout);
        var configured = new Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions();
        OpenAiServiceCollectionExtensions.ConfigureScientificReviewResilience(configured);
        Assert.Equal(OpenAiServiceCollectionExtensions.ScientificReviewAttemptTimeout, configured.AttemptTimeout.Timeout);
        Assert.Equal(OpenAiServiceCollectionExtensions.ScientificReviewTotalTimeout, configured.TotalRequestTimeout.Timeout);
        Assert.Equal(TimeSpan.FromMinutes(5), configured.CircuitBreaker.SamplingDuration);
        Assert.Same(options, provider.GetRequiredService<OpenAiProviderOptions>());
        Assert.NotNull(provider.GetRequiredService<IOpenAiSecretStore>());
        Assert.NotNull(provider.GetRequiredService<OpenAiSdkClientFactory>());
    }

    [Fact]
    public async Task LiveRuntimeComposition_ConsumesResilienceNamedClientForScientificReview()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
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
                ]);

            var services = new ServiceCollection();
            services.AddContentDeliveryStudioProviderRuntime(
                new ProviderRuntimeRegistrationOptions(ProviderMode: "live", EnvPath: envPath));

            using var provider = services.BuildServiceProvider();

            // The live scientific review singleton must be the provider wired to
            // the application's resilience named client, and that client must
            // carry the explicit bounded timeouts.
            var scientificReview = provider.GetRequiredService<OpenAiScientificReviewProvider>();
            Assert.NotNull(scientificReview);
            Assert.IsType<OpenAiScientificReviewProvider>(
                provider.GetRequiredService<IScientificSemanticReviewProvider>());
            var namedClient = provider.GetRequiredService<IHttpClientFactory>()
                .CreateClient(OpenAiHttpClientNames.Provider);
            Assert.Equal("https://text.example/v1/", namedClient.BaseAddress!.ToString());
            Assert.Equal(Timeout.InfiniteTimeSpan, namedClient.Timeout);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
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
