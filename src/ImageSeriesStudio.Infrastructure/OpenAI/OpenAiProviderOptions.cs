namespace ImageSeriesStudio.Infrastructure.OpenAI;

[Flags]
public enum OpenAiProviderOperation
{
    None = 0,
    TextPlanning = 1,
    ImageGeneration = 2,
    VisionReview = 4,
    All = TextPlanning | ImageGeneration | VisionReview,
}

public sealed record OpenAiProviderOptions
{
    public Uri BaseUri { get; init; } = new("https://api.openai.com/v1/");

    public string ApiKeySecretName { get; init; } = "OPENAI_API_KEY";

    public OpenAiProviderOperation AllowedOperations { get; init; } = OpenAiProviderOperation.All;

    public string TextPlanningModel { get; init; } = "gpt-5";

    public string ImageGenerationModel { get; init; } = "gpt-image-2";

    public string VisionReviewModel { get; init; } = "gpt-5";

    public bool RealApiEnabled { get; init; }

    public static OpenAiProviderOptions FromTextProviderEnvironment(
        ProviderEnvironmentConfiguration configuration,
        bool realApiEnabled = false)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new OpenAiProviderOptions
        {
            BaseUri = RequireBaseUri(configuration.Text, "Text provider"),
            ApiKeySecretName = RequireApiKeySecretName(configuration.Text, "Text provider"),
            TextPlanningModel = RequireModel(configuration.Text, "Text provider"),
            ImageGenerationModel = string.Empty,
            VisionReviewModel = RequireModel(configuration.Text, "Text provider"),
            AllowedOperations = OpenAiProviderOperation.TextPlanning | OpenAiProviderOperation.VisionReview,
            RealApiEnabled = realApiEnabled,
        };
    }

    public static OpenAiProviderOptions FromImageProviderEnvironment(
        ProviderEnvironmentConfiguration configuration,
        bool realApiEnabled = false)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new OpenAiProviderOptions
        {
            BaseUri = RequireBaseUri(configuration.Image, "Image provider"),
            ApiKeySecretName = RequireApiKeySecretName(configuration.Image, "Image provider"),
            TextPlanningModel = string.Empty,
            ImageGenerationModel = RequireModel(configuration.Image, "Image provider"),
            VisionReviewModel = string.Empty,
            AllowedOperations = OpenAiProviderOperation.ImageGeneration,
            RealApiEnabled = realApiEnabled,
        };
    }

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

        if (AllowedOperations.HasFlag(OpenAiProviderOperation.TextPlanning)
            && string.IsNullOrWhiteSpace(TextPlanningModel))
        {
            errors.Add("Text planning model is required.");
        }

        if (AllowedOperations.HasFlag(OpenAiProviderOperation.ImageGeneration)
            && string.IsNullOrWhiteSpace(ImageGenerationModel))
        {
            errors.Add("Image generation model is required.");
        }

        if (AllowedOperations.HasFlag(OpenAiProviderOperation.VisionReview)
            && string.IsNullOrWhiteSpace(VisionReviewModel))
        {
            errors.Add("Vision review model is required.");
        }

        if (AllowedOperations is OpenAiProviderOperation.None)
        {
            errors.Add("At least one provider operation must be allowed.");
        }

        return errors;
    }

    private static Uri RequireBaseUri(ProviderEndpointEnvironmentConfiguration endpoint, string displayName)
    {
        return endpoint.BaseUri is { } baseUri
            ? EnsureTrailingSlash(baseUri)
            : throw new InvalidOperationException($"{displayName} base URL is required.");
    }

    private static string RequireApiKeySecretName(ProviderEndpointEnvironmentConfiguration endpoint, string displayName)
    {
        return string.IsNullOrWhiteSpace(endpoint.ApiKeySecretName)
            ? throw new InvalidOperationException($"{displayName} API key secret name is required.")
            : endpoint.ApiKeySecretName;
    }

    private static string RequireModel(ProviderEndpointEnvironmentConfiguration endpoint, string displayName)
    {
        return string.IsNullOrWhiteSpace(endpoint.Model)
            ? throw new InvalidOperationException($"{displayName} model is required.")
            : endpoint.Model;
    }

    private static Uri EnsureTrailingSlash(Uri uri)
    {
        var value = uri.ToString();
        return value.EndsWith("/", StringComparison.Ordinal)
            ? uri
            : new Uri(value + "/");
    }
}

public interface IOpenAiSecretStore
{
    Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken);
}

public interface IWritableOpenAiSecretStore : IOpenAiSecretStore
{
    Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken);

    Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken);
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

public sealed class CompositeOpenAiSecretStore : IOpenAiSecretStore
{
    private readonly IReadOnlyList<IOpenAiSecretStore> _stores;

    public CompositeOpenAiSecretStore(IReadOnlyList<IOpenAiSecretStore> stores)
    {
        if (stores.Count == 0)
        {
            throw new ArgumentException("At least one secret store is required.", nameof(stores));
        }

        _stores = stores;
    }

    public async Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var store in _stores)
        {
            var secret = await store.GetSecretAsync(secretName, cancellationToken);
            if (!string.IsNullOrWhiteSpace(secret))
            {
                return secret;
            }
        }

        return null;
    }
}

public static class OpenAiSecretStores
{
    public static IOpenAiSecretStore CreateDefault()
    {
        if (OperatingSystem.IsWindows())
        {
            return new CompositeOpenAiSecretStore(
            [
                new DpapiOpenAiSecretStore(),
                new EnvironmentOpenAiSecretStore(),
                new DotEnvSecretStore(),
            ]);
        }

        return new CompositeOpenAiSecretStore(
        [
            new EnvironmentOpenAiSecretStore(),
            new DotEnvSecretStore(),
        ]);
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

    public static async Task EnsureCanCallRealApiAsync(
        OpenAiProviderOptions options,
        IOpenAiSecretStore secretStore,
        OpenAiProviderOperation requiredOperation,
        CancellationToken cancellationToken)
    {
        EnsureAllowsOperation(options, requiredOperation);
        await EnsureCanCallRealApiAsync(options, secretStore, cancellationToken);
    }

    public static void EnsureAllowsOperation(
        OpenAiProviderOptions options,
        OpenAiProviderOperation requiredOperation)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.AllowedOperations.HasFlag(requiredOperation))
        {
            throw new InvalidOperationException(
                $"OpenAI provider options are not allowed for {requiredOperation}. Configure a separate provider profile and secret for this operation.");
        }

        EnsureSecretNameMatchesOperation(options.ApiKeySecretName, requiredOperation);
    }

    private static void EnsureSecretNameMatchesOperation(
        string apiKeySecretName,
        OpenAiProviderOperation requiredOperation)
    {
        if (apiKeySecretName.StartsWith("IMAGE_PROVIDER_API_KEY", StringComparison.Ordinal)
            && requiredOperation is OpenAiProviderOperation.TextPlanning or OpenAiProviderOperation.VisionReview)
        {
            throw new InvalidOperationException(
                $"{apiKeySecretName} is reserved for image generation and cannot be used for {requiredOperation}.");
        }

        if (apiKeySecretName.StartsWith("TEXT_PROVIDER_API_KEY", StringComparison.Ordinal)
            && requiredOperation is OpenAiProviderOperation.ImageGeneration)
        {
            throw new InvalidOperationException(
                $"{apiKeySecretName} is reserved for text and vision operations and cannot be used for {requiredOperation}.");
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
