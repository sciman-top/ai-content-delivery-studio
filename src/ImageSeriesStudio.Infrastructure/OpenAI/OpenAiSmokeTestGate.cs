namespace ImageSeriesStudio.Infrastructure.OpenAI;

public sealed record OpenAiSmokeTestOptions(
    string OptInEnvironmentVariable = OpenAiSmokeTestGate.DefaultOptInEnvironmentVariable,
    string RequiredOptInValue = "1");

public sealed record OpenAiSmokeTestDecision(
    bool CanRunRealApiSmoke,
    bool IsDryRun,
    string OptInEnvironmentVariable,
    IReadOnlyList<string> Reasons);

public interface IOpenAiEnvironment
{
    string? GetEnvironmentVariable(string variableName);
}

public sealed class SystemOpenAiEnvironment : IOpenAiEnvironment
{
    public string? GetEnvironmentVariable(string variableName)
    {
        return Environment.GetEnvironmentVariable(variableName);
    }
}

public static class OpenAiSmokeTestGate
{
    public const string DefaultOptInEnvironmentVariable = "IMAGE_SERIES_STUDIO_OPENAI_REAL_API_SMOKE";

    public static async Task<OpenAiSmokeTestDecision> EvaluateAsync(
        OpenAiProviderOptions providerOptions,
        IOpenAiSecretStore secretStore,
        IOpenAiEnvironment environment,
        OpenAiSmokeTestOptions smokeOptions,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var reasons = new List<string>();
        var optInValue = environment.GetEnvironmentVariable(smokeOptions.OptInEnvironmentVariable);
        var optedIn = string.Equals(optInValue, smokeOptions.RequiredOptInValue, StringComparison.OrdinalIgnoreCase);

        if (!optedIn)
        {
            reasons.Add(
                $"Real OpenAI API smoke is not opted in. Set {smokeOptions.OptInEnvironmentVariable}={smokeOptions.RequiredOptInValue} to enable it locally.");
        }

        var readiness = await OpenAiProviderGuard.CheckReadinessAsync(providerOptions, secretStore, cancellationToken);
        reasons.AddRange(readiness.Errors);

        var canRun = optedIn && readiness.CanCallRealApi;

        return new OpenAiSmokeTestDecision(
            canRun,
            IsDryRun: !canRun,
            smokeOptions.OptInEnvironmentVariable,
            reasons.Distinct(StringComparer.Ordinal).ToArray());
    }
}
