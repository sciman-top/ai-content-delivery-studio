namespace ImageSeriesStudio.Infrastructure.OpenAI;

public sealed record OpenAiProviderOptions
{
    public Uri BaseUri { get; init; } = new("https://api.openai.com/v1/");

    public string ApiKeySecretName { get; init; } = "OPENAI_API_KEY";

    public string TextPlanningModel { get; init; } = "gpt-5";

    public string ImageGenerationModel { get; init; } = "gpt-image-2";

    public string VisionReviewModel { get; init; } = "gpt-5";

    public bool RealApiEnabled { get; init; }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (BaseUri is null)
        {
            errors.Add("OpenAI base URI is required.");
        }
        else if (BaseUri.Scheme != Uri.UriSchemeHttps)
        {
            errors.Add("OpenAI base URI must use HTTPS.");
        }

        if (RealApiEnabled && string.IsNullOrWhiteSpace(ApiKeySecretName))
        {
            errors.Add("API key secret name is required when real API calls are enabled.");
        }

        if (string.IsNullOrWhiteSpace(TextPlanningModel))
        {
            errors.Add("Text planning model is required.");
        }

        if (string.IsNullOrWhiteSpace(ImageGenerationModel))
        {
            errors.Add("Image generation model is required.");
        }

        if (string.IsNullOrWhiteSpace(VisionReviewModel))
        {
            errors.Add("Vision review model is required.");
        }

        return errors;
    }
}

public interface IOpenAiSecretStore
{
    Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken);
}

public sealed class EnvironmentOpenAiSecretStore : IOpenAiSecretStore
{
    public Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(
            string.IsNullOrWhiteSpace(secretName)
                ? null
                : Environment.GetEnvironmentVariable(secretName));
    }
}

public static class OpenAiProviderGuard
{
    public static async Task EnsureCanCallRealApiAsync(
        OpenAiProviderOptions options,
        IOpenAiSecretStore secretStore,
        CancellationToken cancellationToken)
    {
        var readiness = await CheckReadinessAsync(options, secretStore, cancellationToken);

        if (!readiness.CanCallRealApi)
        {
            throw new InvalidOperationException(
                "Real OpenAI API calls are not ready: " + string.Join(" ", readiness.Errors));
        }
    }

    public static async Task<OpenAiProviderReadiness> CheckReadinessAsync(
        OpenAiProviderOptions options,
        IOpenAiSecretStore secretStore,
        CancellationToken cancellationToken)
    {
        var errors = options.Validate().ToList();
        var apiKey = options.RealApiEnabled
            ? await secretStore.GetSecretAsync(options.ApiKeySecretName, cancellationToken)
            : null;

        if (!options.RealApiEnabled)
        {
            errors.Add("Real OpenAI API calls are disabled.");
        }
        else if (string.IsNullOrWhiteSpace(apiKey))
        {
            errors.Add("OpenAI API key was not found in the configured secret store.");
        }

        return new OpenAiProviderReadiness(errors.Count == 0, errors);
    }
}

public sealed record OpenAiProviderReadiness(
    bool CanCallRealApi,
    IReadOnlyList<string> Errors);
