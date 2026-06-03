using System.Net;
using System.Net.Http.Headers;

namespace ImageSeriesStudio.Infrastructure.OpenAI;

public sealed class ProviderHealthCheckService
{
    private readonly HttpClient _httpClient;
    private readonly IOpenAiSecretStore _secretStore;

    public ProviderHealthCheckService(HttpClient httpClient, IOpenAiSecretStore secretStore)
    {
        _httpClient = httpClient;
        _secretStore = secretStore;
    }

    public async Task<ProviderHealthCheckResult> CheckModelsEndpointAsync(
        ProviderEndpointEnvironmentConfiguration endpoint,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        if (endpoint.ApiKeySecretName is null)
        {
            return ProviderHealthCheckResult.MissingConfiguration(endpoint.Prefix, string.Empty, "API key secret name is missing.");
        }

        return await CheckModelsEndpointAsync(endpoint, endpoint.ApiKeySecretName, cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderHealthCheckResult>> CheckKeyPoolModelsEndpointAsync(
        ProviderEndpointEnvironmentConfiguration endpoint,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var results = new List<ProviderHealthCheckResult>();
        foreach (var secretName in endpoint.ApiKeySecretNames)
        {
            results.Add(await CheckModelsEndpointAsync(endpoint, secretName, cancellationToken));
        }

        return results;
    }

    private async Task<ProviderHealthCheckResult> CheckModelsEndpointAsync(
        ProviderEndpointEnvironmentConfiguration endpoint,
        string apiKeySecretName,
        CancellationToken cancellationToken)
    {
        if (endpoint.BaseUri is null)
        {
            return ProviderHealthCheckResult.MissingConfiguration(endpoint.Prefix, apiKeySecretName, "Base URL is missing.");
        }

        var apiKey = await _secretStore.GetSecretAsync(apiKeySecretName, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return ProviderHealthCheckResult.MissingSecret(endpoint.Prefix, apiKeySecretName);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, BuildModelsUri(endpoint.BaseUri));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return ProviderHealthCheckResult.FromHttpStatus(endpoint.Prefix, apiKeySecretName, response.StatusCode);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new ProviderHealthCheckResult(
                endpoint.Prefix,
                apiKeySecretName,
                ProviderHealthStatus.Unreachable,
                null,
                "/v1/models",
                exception.GetType().Name);
        }
    }

    private static Uri BuildModelsUri(Uri baseUri)
    {
        var value = baseUri.ToString().TrimEnd('/');
        return value.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
            ? new Uri(value + "/models")
            : new Uri(value + "/v1/models");
    }
}

public sealed record ProviderHealthCheckResult(
    string ProviderPrefix,
    string ApiKeySecretName,
    ProviderHealthStatus Status,
    HttpStatusCode? HttpStatusCode,
    string EndpointPath,
    string Detail)
{
    public static ProviderHealthCheckResult MissingConfiguration(string providerPrefix, string apiKeySecretName, string detail)
    {
        return new ProviderHealthCheckResult(
            providerPrefix,
            apiKeySecretName,
            ProviderHealthStatus.MissingConfiguration,
            null,
            "/v1/models",
            detail);
    }

    public static ProviderHealthCheckResult MissingSecret(string providerPrefix, string apiKeySecretName)
    {
        return new ProviderHealthCheckResult(
            providerPrefix,
            apiKeySecretName,
            ProviderHealthStatus.MissingSecret,
            null,
            "/v1/models",
            "Secret was not found.");
    }

    public static ProviderHealthCheckResult FromHttpStatus(
        string providerPrefix,
        string apiKeySecretName,
        HttpStatusCode statusCode)
    {
        return new ProviderHealthCheckResult(
            providerPrefix,
            apiKeySecretName,
            MapStatus(statusCode),
            statusCode,
            "/v1/models",
            $"HTTP {(int)statusCode}");
    }

    private static ProviderHealthStatus MapStatus(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            System.Net.HttpStatusCode.OK => ProviderHealthStatus.Healthy,
            System.Net.HttpStatusCode.Unauthorized => ProviderHealthStatus.AuthRejected,
            System.Net.HttpStatusCode.Forbidden => ProviderHealthStatus.Forbidden,
            System.Net.HttpStatusCode.NotFound => ProviderHealthStatus.NotFound,
            _ => ProviderHealthStatus.HttpError,
        };
    }
}

public enum ProviderHealthStatus
{
    Healthy = 0,
    MissingConfiguration = 1,
    MissingSecret = 2,
    AuthRejected = 3,
    Forbidden = 4,
    NotFound = 5,
    HttpError = 6,
    Unreachable = 7,
}
