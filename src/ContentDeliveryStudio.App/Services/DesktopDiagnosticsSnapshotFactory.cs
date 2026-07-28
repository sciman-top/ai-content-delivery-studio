using System.Reflection;
using System.Runtime.InteropServices;
using ContentDeliveryStudio.Application.Diagnostics;
using ContentDeliveryStudio.Core.Providers;

namespace ContentDeliveryStudio.App.Services;

public interface IDesktopDiagnosticsSnapshotFactory
{
    Task<DesktopDiagnosticsSnapshots> CreateAsync(CancellationToken cancellationToken);
}

public sealed class DesktopDiagnosticsSnapshotFactory : IDesktopDiagnosticsSnapshotFactory
{
    private readonly IProviderCenterConfigurationService _configurationService;
    private readonly IReadOnlyList<IProviderCapabilities> _providerCapabilities;

    public DesktopDiagnosticsSnapshotFactory(
        IProviderCenterConfigurationService configurationService,
        ITextPlanningProvider textPlanningProvider,
        IImageGenerationProvider imageGenerationProvider,
        IImageEditProvider imageEditProvider,
        IVisionReviewProvider visionReviewProvider)
    {
        _configurationService = configurationService;
        _providerCapabilities =
        [
            textPlanningProvider.Capabilities,
            imageGenerationProvider.Capabilities,
            imageEditProvider.Capabilities,
            visionReviewProvider.Capabilities,
        ];
    }

    public async Task<DesktopDiagnosticsSnapshots> CreateAsync(CancellationToken cancellationToken)
    {
        var configuration = await _configurationService.LoadAsync(cancellationToken);
        var providers = _providerCapabilities
            .GroupBy(item => item.ProviderId, StringComparer.OrdinalIgnoreCase)
            .Select(group => CreateProviderSnapshot(group.First()))
            .OrderBy(item => item.ProviderId, StringComparer.Ordinal)
            .ToArray();

        var secrets = new[]
        {
            new DiagnosticsSecretSnapshot("text-provider-key-pool", configuration.Text.ApiKeyCount > 0),
            new DiagnosticsSecretSnapshot("text-provider-app-credentials", configuration.Text.UsesAppCredentials),
            new DiagnosticsSecretSnapshot("image-provider-key-pool", configuration.Image.ApiKeyCount > 0),
            new DiagnosticsSecretSnapshot("image-provider-app-credentials", configuration.Image.UsesAppCredentials),
        };

        return new DesktopDiagnosticsSnapshots(
            CaptureApplicationSnapshot(),
            CaptureMachineSnapshot(),
            providers,
            secrets);
    }

    private static DiagnosticsProviderSnapshot CreateProviderSnapshot(IProviderCapabilities capabilities)
    {
        var capabilityNames = new List<string>();
        AddIf(capabilityNames, capabilities.SupportsTextPlanning, "text-planning");
        AddIf(capabilityNames, capabilities.SupportsImageGeneration, "image-generation");
        AddIf(capabilityNames, capabilities.SupportsImageEditing, "image-editing");
        AddIf(capabilityNames, capabilities.SupportsVisionReview, "vision-review");
        AddIf(capabilityNames, capabilities.SupportsStreaming, "streaming");
        AddIf(capabilityNames, capabilities.SupportsReferenceImages, "reference-images");

        var isFake = capabilities.ProviderId.StartsWith("fake", StringComparison.OrdinalIgnoreCase);
        return new DiagnosticsProviderSnapshot(
            capabilities.ProviderId,
            capabilities.DisplayName,
            isFake ? "Fake" : "Live",
            capabilities.ModelIds.OrderBy(item => item, StringComparer.Ordinal).ToArray(),
            capabilityNames,
            RealApiEnabled: !isFake,
            DryRunOnly: isFake);
    }

    private static DiagnosticsApplicationSnapshot CaptureApplicationSnapshot()
    {
        var assembly = typeof(App).Assembly;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";

        return new DiagnosticsApplicationSnapshot(
            "AI Content Delivery Studio",
            version,
#if DEBUG
            "Debug",
#else
            "Release",
#endif
            DateTimeOffset.MinValue);
    }

    private static DiagnosticsMachineSnapshot CaptureMachineSnapshot()
    {
        return new DiagnosticsMachineSnapshot(
            RuntimeInformation.OSDescription,
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.ProcessArchitecture.ToString(),
            System.Globalization.CultureInfo.CurrentCulture.Name,
            System.Globalization.CultureInfo.CurrentUICulture.Name);
    }

    private static void AddIf(List<string> names, bool condition, string name)
    {
        if (condition)
        {
            names.Add(name);
        }
    }
}

public sealed record DesktopDiagnosticsSnapshots(
    DiagnosticsApplicationSnapshot Application,
    DiagnosticsMachineSnapshot Machine,
    IReadOnlyList<DiagnosticsProviderSnapshot> Providers,
    IReadOnlyList<DiagnosticsSecretSnapshot> Secrets);
