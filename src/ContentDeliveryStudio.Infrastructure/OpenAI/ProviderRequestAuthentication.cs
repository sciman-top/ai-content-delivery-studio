using System.Net.Http.Headers;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

internal sealed record ProviderRequestCredentials(
    string ApiKey,
    string? AppId,
    string? AppSecret);

internal sealed class ProviderCredentialNotFoundException(string secretName)
    : InvalidOperationException($"Provider credential was not found: {secretName}")
{
    public string SecretName { get; } = secretName;
}

internal static class ProviderRequestAuthentication
{
    public static async Task<ProviderRequestCredentials> ResolveAsync(
        IOpenAiSecretStore secretStore,
        string apiKeySecretName,
        string? appIdSecretName,
        string? appSecretSecretName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(secretStore);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKeySecretName);

        var apiKey = await RequireSecretAsync(secretStore, apiKeySecretName, cancellationToken);
        var appId = await GetConfiguredSecretAsync(secretStore, appIdSecretName, cancellationToken);
        var appSecret = await GetConfiguredSecretAsync(secretStore, appSecretSecretName, cancellationToken);
        return new ProviderRequestCredentials(apiKey, appId, appSecret);
    }

    public static void Apply(HttpRequestMessage request, ProviderRequestCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(credentials);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.ApiKey);
        if (!string.IsNullOrWhiteSpace(credentials.AppId))
        {
            request.Headers.TryAddWithoutValidation("X-App-ID", credentials.AppId);
        }

        if (!string.IsNullOrWhiteSpace(credentials.AppSecret))
        {
            request.Headers.TryAddWithoutValidation("X-App-Secret", credentials.AppSecret);
        }
    }

    private static async Task<string> RequireSecretAsync(
        IOpenAiSecretStore secretStore,
        string secretName,
        CancellationToken cancellationToken)
    {
        var value = await secretStore.GetSecretAsync(secretName, cancellationToken);
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ProviderCredentialNotFoundException(secretName);
    }

    private static async Task<string?> GetConfiguredSecretAsync(
        IOpenAiSecretStore secretStore,
        string? secretName,
        CancellationToken cancellationToken)
    {
        return string.IsNullOrWhiteSpace(secretName)
            ? null
            : await RequireSecretAsync(secretStore, secretName, cancellationToken);
    }
}
