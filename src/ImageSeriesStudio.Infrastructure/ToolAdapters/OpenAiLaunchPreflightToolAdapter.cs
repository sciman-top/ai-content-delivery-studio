using ImageSeriesStudio.Application.ToolAdapters;
using ImageSeriesStudio.Infrastructure.OpenAI;

namespace ImageSeriesStudio.Infrastructure.ToolAdapters;

public sealed class OpenAiLaunchPreflightToolAdapter : IToolAdapter
{
    private static readonly ToolAdapterDescriptor BuiltInDescriptor =
        LocalToolRegistry.CreateBuiltIn().GetRequired("openai-launch-preflight");

    public ToolAdapterDescriptor Descriptor => BuiltInDescriptor;

    public async Task<ToolAdapterRunResult> RunAsync(
        ToolAdapterRunRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var envPath = GetRequiredInput(request, "envPath");
        if (!File.Exists(envPath))
        {
            throw new FileNotFoundException("Environment file was not found.", envPath);
        }

        var outputDirectory = ResolveOutputDirectory(request, envPath);
        var configuration = await ProviderEnvironmentConfiguration.FromDotEnvFileAsync(envPath, cancellationToken);
        var secretStore = BuildSecretStore(envPath);
        var smokeOptions = BuildSmokeOptions(request);
        var report = await OpenAiLaunchPreflight.EvaluateAsync(
            configuration,
            secretStore,
            new SystemOpenAiEnvironment(),
            smokeOptions,
            cancellationToken);

        var plannedJsonPath = Path.Combine(outputDirectory, "openai-launch-preflight.json");
        var plannedMarkdownPath = Path.Combine(outputDirectory, "openai-launch-preflight.md");
        var warnings = report.BlockingReasons;

        if (request.DryRun)
        {
            return ToolAdapterRunResult.Create(
                Descriptor.Id,
                dryRun: true,
                new Dictionary<string, string>
                {
                    ["preflightJsonPath"] = plannedJsonPath,
                    ["preflightMarkdownPath"] = plannedMarkdownPath,
                },
                warnings,
                $"OpenAI launch preflight dry-run evaluated readiness and planned report output under {outputDirectory}.");
        }

        var writer = new OpenAiLaunchPreflightReportWriter();
        var result = await writer.WriteAsync(outputDirectory, report, cancellationToken);

        return ToolAdapterRunResult.Create(
            Descriptor.Id,
            dryRun: false,
            new Dictionary<string, string>
            {
                ["preflightJsonPath"] = result.JsonPath,
                ["preflightMarkdownPath"] = result.MarkdownPath,
            },
            warnings,
            report.CanRunLiveV1SampleSeries
                ? "OpenAI launch preflight is ready for a live V1 sample series run."
                : $"OpenAI launch preflight blocked a live V1 sample series run with {report.BlockingReasons.Count} blocking reason(s).");
    }

    private static string GetRequiredInput(ToolAdapterRunRequest request, string inputName)
    {
        if (!request.Inputs.TryGetValue(inputName, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Tool adapter input is missing: {inputName}");
        }

        return value.Trim();
    }

    private static string ResolveOutputDirectory(ToolAdapterRunRequest request, string envPath)
    {
        if (request.Inputs.TryGetValue("outputDirectory", out var outputDirectory)
            && !string.IsNullOrWhiteSpace(outputDirectory))
        {
            return outputDirectory.Trim();
        }

        var rootDirectory = Path.GetDirectoryName(envPath)
            ?? throw new InvalidOperationException("Environment path does not have a directory.");
        return Path.Combine(rootDirectory, "diagnostics");
    }

    private static OpenAiSmokeTestOptions BuildSmokeOptions(ToolAdapterRunRequest request)
    {
        var envVar = request.Inputs.TryGetValue("optInEnvironmentVariable", out var envValue)
            && !string.IsNullOrWhiteSpace(envValue)
                ? envValue.Trim()
                : OpenAiSmokeTestGate.DefaultOptInEnvironmentVariable;
        var requiredValue = request.Inputs.TryGetValue("optInRequiredValue", out var requiredValueInput)
            && !string.IsNullOrWhiteSpace(requiredValueInput)
                ? requiredValueInput.Trim()
                : "1";

        return new OpenAiSmokeTestOptions(envVar, requiredValue);
    }

    private static IOpenAiSecretStore BuildSecretStore(string envPath)
    {
        if (OperatingSystem.IsWindows())
        {
            return new CompositeOpenAiSecretStore(
            [
                new DpapiOpenAiSecretStore(),
                new EnvironmentOpenAiSecretStore(),
                new DotEnvSecretStore(envPath),
            ]);
        }

        return new CompositeOpenAiSecretStore(
        [
            new EnvironmentOpenAiSecretStore(),
            new DotEnvSecretStore(envPath),
        ]);
    }
}
