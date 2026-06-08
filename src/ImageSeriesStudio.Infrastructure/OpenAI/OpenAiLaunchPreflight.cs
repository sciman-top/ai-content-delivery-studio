namespace ImageSeriesStudio.Infrastructure.OpenAI;

public sealed record OpenAiLaunchPreflightReport(
    IReadOnlyList<string> ConfigurationErrors,
    OpenAiOperationPreflight TextPlanning,
    OpenAiOperationPreflight VisionReview,
    OpenAiOperationPreflight ImageGeneration,
    OpenAiSmokeTestDecision TextSmoke,
    OpenAiSmokeTestDecision ImageSmoke,
    bool CanRunLiveV1SampleSeries,
    IReadOnlyList<string> BlockingReasons);

public sealed record OpenAiOperationPreflight(
    OpenAiProviderOperation Operation,
    string ProviderPrefix,
    bool CanCallRealApi,
    IReadOnlyList<string> Errors);

public static class OpenAiLaunchPreflight
{
    public static async Task<OpenAiLaunchPreflightReport> EvaluateAsync(
        ProviderEnvironmentConfiguration configuration,
        IOpenAiSecretStore secretStore,
        IOpenAiEnvironment environment,
        OpenAiSmokeTestOptions smokeOptions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(secretStore);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(smokeOptions);

        var configurationErrors = configuration.Validate().Distinct(StringComparer.Ordinal).ToArray();
        var textProvider = TryCreateOptions(
            "TEXT_PROVIDER",
            () => OpenAiProviderOptions.FromTextProviderEnvironment(configuration, realApiEnabled: true));
        var imageProvider = TryCreateOptions(
            "IMAGE_PROVIDER",
            () => OpenAiProviderOptions.FromImageProviderEnvironment(configuration, realApiEnabled: true));

        var textPlanning = await EvaluateOperationAsync(
            textProvider.ProviderPrefix,
            OpenAiProviderOperation.TextPlanning,
            textProvider.Options,
            textProvider.Errors,
            secretStore,
            cancellationToken);
        var visionReview = await EvaluateOperationAsync(
            textProvider.ProviderPrefix,
            OpenAiProviderOperation.VisionReview,
            textProvider.Options,
            textProvider.Errors,
            secretStore,
            cancellationToken);
        var imageGeneration = await EvaluateOperationAsync(
            imageProvider.ProviderPrefix,
            OpenAiProviderOperation.ImageGeneration,
            imageProvider.Options,
            imageProvider.Errors,
            secretStore,
            cancellationToken);

        var textSmoke = await EvaluateSmokeAsync(
            textProvider.Options,
            textProvider.Errors,
            secretStore,
            environment,
            smokeOptions,
            cancellationToken);
        var imageSmoke = await EvaluateSmokeAsync(
            imageProvider.Options,
            imageProvider.Errors,
            secretStore,
            environment,
            smokeOptions,
            cancellationToken);

        var canRunLiveV1SampleSeries =
            textSmoke.CanRunRealApiSmoke
            && imageSmoke.CanRunRealApiSmoke
            && textPlanning.CanCallRealApi
            && visionReview.CanCallRealApi
            && imageGeneration.CanCallRealApi;

        var blockingReasons = configurationErrors
            .Concat(textPlanning.Errors)
            .Concat(visionReview.Errors)
            .Concat(imageGeneration.Errors)
            .Concat(textSmoke.Reasons)
            .Concat(imageSmoke.Reasons)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new OpenAiLaunchPreflightReport(
            configurationErrors,
            textPlanning,
            visionReview,
            imageGeneration,
            textSmoke,
            imageSmoke,
            canRunLiveV1SampleSeries,
            blockingReasons);
    }

    private static (string ProviderPrefix, OpenAiProviderOptions? Options, IReadOnlyList<string> Errors) TryCreateOptions(
        string providerPrefix,
        Func<OpenAiProviderOptions> factory)
    {
        try
        {
            return (providerPrefix, factory(), []);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return (providerPrefix, null, [exception.Message]);
        }
    }

    private static async Task<OpenAiOperationPreflight> EvaluateOperationAsync(
        string providerPrefix,
        OpenAiProviderOperation operation,
        OpenAiProviderOptions? options,
        IReadOnlyList<string> optionErrors,
        IOpenAiSecretStore secretStore,
        CancellationToken cancellationToken)
    {
        var errors = optionErrors.ToList();
        if (options is null)
        {
            return new OpenAiOperationPreflight(operation, providerPrefix, false, errors.Distinct(StringComparer.Ordinal).ToArray());
        }

        try
        {
            OpenAiProviderGuard.EnsureAllowsOperation(options, operation);
        }
        catch (InvalidOperationException exception)
        {
            errors.Add(exception.Message);
            return new OpenAiOperationPreflight(operation, providerPrefix, false, errors.Distinct(StringComparer.Ordinal).ToArray());
        }

        var readiness = await OpenAiProviderGuard.CheckReadinessAsync(options, secretStore, cancellationToken);
        errors.AddRange(readiness.Errors);

        return new OpenAiOperationPreflight(
            operation,
            providerPrefix,
            readiness.CanCallRealApi && errors.Count == 0,
            errors.Distinct(StringComparer.Ordinal).ToArray());
    }

    private static async Task<OpenAiSmokeTestDecision> EvaluateSmokeAsync(
        OpenAiProviderOptions? options,
        IReadOnlyList<string> optionErrors,
        IOpenAiSecretStore secretStore,
        IOpenAiEnvironment environment,
        OpenAiSmokeTestOptions smokeOptions,
        CancellationToken cancellationToken)
    {
        if (options is null)
        {
            return new OpenAiSmokeTestDecision(
                CanRunRealApiSmoke: false,
                IsDryRun: true,
                smokeOptions.OptInEnvironmentVariable,
                optionErrors.Distinct(StringComparer.Ordinal).ToArray());
        }

        return await OpenAiSmokeTestGate.EvaluateAsync(
            options,
            secretStore,
            environment,
            smokeOptions,
            cancellationToken);
    }
}
