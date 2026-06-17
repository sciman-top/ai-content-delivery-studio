using OpenAI;
using OpenAI.Images;
using System.ClientModel;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

public interface IOpenAiOfficialSdkFactory
{
    string PackageId { get; }

    string PackageVersion { get; }

    OpenAIClient CreateRootClient(string apiKey, OpenAiProviderOptions options);

    ImageClient CreateImageClient(string apiKey, OpenAiProviderOptions options);
}

public enum OpenAiProviderTransportKind
{
    RawHttp = 0,
    OfficialSdk = 1,
}

public sealed record OpenAiOfficialSdkDecision(
    OpenAiProviderTransportKind TransportKind,
    string Reason);

public static class OpenAiOfficialSdkSelection
{
    public static OpenAiOfficialSdkDecision ForOperation(OpenAiProviderOperation operation)
    {
        return operation switch
        {
            OpenAiProviderOperation.ImageGeneration => new OpenAiOfficialSdkDecision(
                OpenAiProviderTransportKind.OfficialSdk,
                "The official OpenAI .NET SDK ImageClient is the preferred transport for the stable Images API path."),
            OpenAiProviderOperation.TextPlanning => new OpenAiOfficialSdkDecision(
                OpenAiProviderTransportKind.RawHttp,
                "ResponsesClient still emits OPENAI001 evaluation warnings in OpenAI 2.11.0, so text planning stays on raw HTTP for now."),
            OpenAiProviderOperation.VisionReview => new OpenAiOfficialSdkDecision(
                OpenAiProviderTransportKind.RawHttp,
                "ResponsesClient still emits OPENAI001 evaluation warnings in OpenAI 2.11.0, so vision review stays on raw HTTP for now."),
            _ => new OpenAiOfficialSdkDecision(
                OpenAiProviderTransportKind.RawHttp,
                "No official SDK transport decision is defined for this operation."),
        };
    }
}

public sealed class OpenAiOfficialSdkFactory : IOpenAiOfficialSdkFactory
{
    public string PackageId => typeof(OpenAIClient).Assembly.GetName().Name ?? "OpenAI";

    public string PackageVersion => typeof(OpenAIClient).Assembly.GetName().Version?.ToString() ?? "unknown";

    public OpenAIClient CreateRootClient(string apiKey, OpenAiProviderOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentNullException.ThrowIfNull(options);

        return new OpenAIClient(
            new ApiKeyCredential(apiKey),
            CreateClientOptions(options));
    }

    public ImageClient CreateImageClient(string apiKey, OpenAiProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return CreateRootClient(apiKey, options).GetImageClient(options.ImageGenerationModel);
    }

    private static OpenAIClientOptions CreateClientOptions(OpenAiProviderOptions options)
    {
        return new OpenAIClientOptions
        {
            Endpoint = options.BaseUri,
        };
    }
}
