using System.ClientModel;
using System.ClientModel.Primitives;
using OpenAI;
using OpenAI.Images;
using OpenAI.Responses;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

#pragma warning disable OPENAI001 // SDK Responses APIs are adopted behind ADR 0009 parity gates.
public sealed class OpenAiSdkClientFactory
{
    private readonly IOpenAiSecretStore _secretStore;

    public OpenAiSdkClientFactory(IOpenAiSecretStore secretStore)
    {
        _secretStore = secretStore;
    }

    public async Task<ResponsesClient> CreateResponsesClientAsync(
        OpenAiProviderOptions options,
        OpenAiProviderOperation requiredOperation,
        CancellationToken cancellationToken)
    {
        if (requiredOperation is not OpenAiProviderOperation.TextPlanning
            and not OpenAiProviderOperation.VisionReview)
        {
            throw new ArgumentException(
                "Responses SDK clients are only supported for text planning and vision review provider roles.",
                nameof(requiredOperation));
        }

        var credentials = await ResolveCredentialsAsync(options, requiredOperation, cancellationToken);
        var clientOptions = new ResponsesClientOptions
        {
            Endpoint = options.BaseUri,
        };
        AddAppCredentialPolicies(clientOptions, credentials);

        return new ResponsesClient(
            new ApiKeyCredential(credentials.ApiKey),
            clientOptions);
    }

    public async Task<ImageClient> CreateImageClientAsync(
        OpenAiProviderOptions options,
        CancellationToken cancellationToken)
    {
        var credentials = await ResolveCredentialsAsync(
            options,
            OpenAiProviderOperation.ImageGeneration,
            cancellationToken);
        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = options.BaseUri,
        };
        AddAppCredentialPolicies(clientOptions, credentials);

        return new ImageClient(
            options.ImageGenerationModel,
            new ApiKeyCredential(credentials.ApiKey),
            clientOptions);
    }

    private async Task<ProviderRequestCredentials> ResolveCredentialsAsync(
        OpenAiProviderOptions options,
        OpenAiProviderOperation requiredOperation,
        CancellationToken cancellationToken)
    {
        await OpenAiProviderGuard.EnsureCanCallRealApiAsync(
            options,
            _secretStore,
            requiredOperation,
            cancellationToken);

        return await ProviderRequestAuthentication.ResolveAsync(
            _secretStore,
            options.ApiKeySecretName,
            options.AppIdSecretName,
            options.AppSecretSecretName,
            cancellationToken);
    }

    private static void AddAppCredentialPolicies(
        ClientPipelineOptions options,
        ProviderRequestCredentials credentials)
    {
        if (!string.IsNullOrWhiteSpace(credentials.AppId))
        {
            options.AddPolicy(
                ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(
                    new ApiKeyCredential(credentials.AppId),
                    "X-App-ID",
                    keyPrefix: null!),
                PipelinePosition.PerCall);
        }

        if (!string.IsNullOrWhiteSpace(credentials.AppSecret))
        {
            options.AddPolicy(
                ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(
                    new ApiKeyCredential(credentials.AppSecret),
                    "X-App-Secret",
                    keyPrefix: null!),
                PipelinePosition.PerCall);
        }
    }
}
#pragma warning restore OPENAI001
