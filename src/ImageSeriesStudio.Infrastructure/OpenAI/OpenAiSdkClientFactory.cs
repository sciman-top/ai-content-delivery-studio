using System.ClientModel;
using OpenAI;
using OpenAI.Images;
using OpenAI.Responses;

namespace ImageSeriesStudio.Infrastructure.OpenAI;

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

        var credential = await CreateCredentialAsync(options, requiredOperation, cancellationToken);

        return new ResponsesClient(
            credential,
            new ResponsesClientOptions
            {
                Endpoint = options.BaseUri,
            });
    }

    public async Task<ImageClient> CreateImageClientAsync(
        OpenAiProviderOptions options,
        CancellationToken cancellationToken)
    {
        var credential = await CreateCredentialAsync(
            options,
            OpenAiProviderOperation.ImageGeneration,
            cancellationToken);

        return new ImageClient(
            options.ImageGenerationModel,
            credential,
            new OpenAIClientOptions
            {
                Endpoint = options.BaseUri,
            });
    }

    private async Task<ApiKeyCredential> CreateCredentialAsync(
        OpenAiProviderOptions options,
        OpenAiProviderOperation requiredOperation,
        CancellationToken cancellationToken)
    {
        await OpenAiProviderGuard.EnsureCanCallRealApiAsync(
            options,
            _secretStore,
            requiredOperation,
            cancellationToken);

        var apiKey = await _secretStore.GetSecretAsync(options.ApiKeySecretName, cancellationToken)
            ?? throw new InvalidOperationException("OpenAI API key was not found in the configured secret store.");

        return new ApiKeyCredential(apiKey);
    }
}
#pragma warning restore OPENAI001
