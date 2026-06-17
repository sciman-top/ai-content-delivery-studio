using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using OpenAI.Responses;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

#pragma warning disable OPENAI001 // SDK Responses APIs are adopted behind ADR 0009 parity gates.
internal interface IOpenAiResponsesClient
{
    Task<OpenAiResponsesClientResult> CreateResponseAsync(
        CreateResponseOptions options,
        CancellationToken cancellationToken);
}

internal sealed class OpenAiSdkResponsesClient : IOpenAiResponsesClient
{
    private readonly ResponsesClient _client;

    public OpenAiSdkResponsesClient(ResponsesClient client)
    {
        _client = client;
    }

    public async Task<OpenAiResponsesClientResult> CreateResponseAsync(
        CreateResponseOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _client.CreateResponseAsync(options, cancellationToken);
            return CreateResult(result.Value.GetOutputText(), result.GetRawResponse());
        }
        catch (ClientResultException exception) when (exception.GetRawResponse() is { } response)
        {
            throw new OpenAiResponsesClientException(CreateResult(outputText: null, response), exception);
        }
    }

    private static OpenAiResponsesClientResult CreateResult(string? outputText, PipelineResponse response)
    {
        var body = TryCloneJsonBody(response.Content);
        return new OpenAiResponsesClientResult(
            outputText,
            body,
            response.Status,
            response.ReasonPhrase,
            ExtractRequestId(response));
    }

    private static JsonElement? TryCloneJsonBody(BinaryData? content)
    {
        if (content is null)
        {
            return null;
        }

        var text = content.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        using var document = JsonDocument.Parse(text);
        return document.RootElement.Clone();
    }

    private static string? ExtractRequestId(PipelineResponse response)
    {
        foreach (var headerName in OpenAiProviderTelemetry.RequestIdHeaderNames)
        {
            if (response.Headers.TryGetValue(headerName, out var value)
                && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
#pragma warning restore OPENAI001

internal sealed record OpenAiResponsesClientResult(
    string? OutputText,
    JsonElement? Body,
    int StatusCode,
    string? ReasonPhrase,
    string? RequestId)
{
    public bool IsSuccessStatusCode => StatusCode is >= 200 and <= 299;
}

internal sealed class OpenAiResponsesClientException : Exception
{
    public OpenAiResponsesClientException(
        OpenAiResponsesClientResult result,
        Exception innerException)
        : base(innerException.Message, innerException)
    {
        Result = result;
    }

    public OpenAiResponsesClientResult Result { get; }
}
