using System.Text.Json;
using ContentDeliveryStudio.App.Telemetry;
using Microsoft.Extensions.Configuration;

namespace ContentDeliveryStudio.Tests;

public sealed class AppOpenTelemetryTests
{
    [Fact]
    public void ShouldUseOtlpExporter_ReturnsFalseWithoutEndpointAndTrueWithEndpoint()
    {
        var emptyConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection()
            .Build();

        var dashboardConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [AppOpenTelemetry.OtlpEndpointSetting] = AppOpenTelemetry.DefaultOtlpEndpoint,
                })
            .Build();

        Assert.False(AppOpenTelemetry.ShouldUseOtlpExporter(emptyConfiguration));
        Assert.True(AppOpenTelemetry.ShouldUseOtlpExporter(dashboardConfiguration));
    }

    [Fact]
    public void LaunchSettings_ExposeAspireDashboardProfileForLocalOtlpExport()
    {
        var launchSettingsPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "ContentDeliveryStudio.App",
            "Properties",
            "launchSettings.json"));

        using var document = JsonDocument.Parse(File.ReadAllText(launchSettingsPath));
        var profiles = document.RootElement.GetProperty("profiles");
        var appProfile = profiles.GetProperty("ContentDeliveryStudio.App");
        var dashboardProfile = profiles.GetProperty(AppOpenTelemetry.AspireDashboardProfileName);
        var environmentVariables = dashboardProfile.GetProperty("environmentVariables");

        Assert.Equal("Project", appProfile.GetProperty("commandName").GetString());
        Assert.Equal("Project", dashboardProfile.GetProperty("commandName").GetString());
        Assert.Equal(AppOpenTelemetry.DefaultOtlpEndpoint, environmentVariables.GetProperty(AppOpenTelemetry.OtlpEndpointSetting).GetString());
        Assert.Equal(AppOpenTelemetry.DefaultServiceName, environmentVariables.GetProperty(AppOpenTelemetry.ServiceNameSetting).GetString());
    }
}
