using System.Net;
using System.Text;
using System.Text.Json;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Infrastructure.OpenAI;

namespace ImageSeriesStudio.Tests;

public sealed class OpenAiProviderContractTests
{
    [Fact]
    public async Task TextPlanningProvider_PostsResponsesRequestAndParsesOutputText()
    {
        using var handler = new CaptureHandler(_ => JsonResponse(
            """
            {
              "id": "resp_plan_123",
              "output_text": "{\"summary\":\"Two poster plan\",\"items\":[{\"title\":\"Opening\",\"brief\":\"Introduce the topic\",\"promptDraft\":\"Create an opening poster\"},{\"title\":\"Detail\",\"brief\":\"Show the key detail\",\"promptDraft\":\"Create a detail poster\"}]}"
            }
            """));
        using var httpClient = new HttpClient(handler);
        var provider = CreateProvider(httpClient);

        var result = await provider.CreatePlanAsync(
            new PlanningRequest("gravity poster series", "middle school", 2, "clean diagrams"),
            CancellationToken.None);

        Assert.Equal("resp_plan_123", result.ProviderTraceId);
        Assert.Equal("Two poster plan", result.Summary);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Opening", result.Items[0].Title);
        Assert.Equal("Create a detail poster", result.Items[1].PromptDraft);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("https://api.openai.com/v1/responses", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal("test-openai-key", handler.LastRequest.Headers.Authorization.Parameter);

        using var payload = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("gpt-5", payload.RootElement.GetProperty("model").GetString());
        Assert.False(payload.RootElement.GetProperty("store").GetBoolean());
        Assert.Equal(
            "json_schema",
            payload.RootElement.GetProperty("text").GetProperty("format").GetProperty("type").GetString());

        var input = payload.RootElement.GetProperty("input").GetString();
        Assert.Contains("gravity poster series", input);
        Assert.Contains("middle school", input);
        Assert.Contains("Item count: 2", input);
    }

    [Fact]
    public async Task TextPlanningProvider_ParsesMessageContentFallback()
    {
        using var handler = new CaptureHandler(_ => JsonResponse(
            """
            {
              "id": "resp_plan_456",
              "output": [
                {
                  "type": "message",
                  "content": [
                    {
                      "type": "output_text",
                      "text": "{\"summary\":\"Fallback plan\",\"items\":[{\"title\":\"Frame 1\",\"brief\":\"A brief\",\"promptDraft\":\"A prompt\"}]}"
                    }
                  ]
                }
              ]
            }
            """));
        using var httpClient = new HttpClient(handler);
        var provider = CreateProvider(httpClient);

        var result = await provider.CreatePlanAsync(
            new PlanningRequest("topic", "audience", 1),
            CancellationToken.None);

        Assert.Equal("resp_plan_456", result.ProviderTraceId);
        Assert.Equal("Fallback plan", result.Summary);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task TextPlanningProvider_DoesNotSendHttpWhenRealApiIsDisabled()
    {
        using var handler = new CaptureHandler(_ => JsonResponse("{}"));
        using var httpClient = new HttpClient(handler);
        var provider = new OpenAiTextPlanningProvider(
            httpClient,
            new OpenAiProviderOptions(),
            new StaticSecretStore("test-openai-key"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.CreatePlanAsync(new PlanningRequest("topic", "audience", 1), CancellationToken.None));

        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task TextPlanningProvider_MapsNonSuccessResponsesToHttpRequestException()
    {
        using var handler = new CaptureHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            ReasonPhrase = "Too Many Requests",
            Content = new StringContent("""{"error":{"message":"rate limited"}}""", Encoding.UTF8, "application/json"),
        });
        using var httpClient = new HttpClient(handler);
        var provider = CreateProvider(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            provider.CreatePlanAsync(new PlanningRequest("topic", "audience", 1), CancellationToken.None));

        Assert.Contains("429", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, handler.CallCount);
    }

    private static OpenAiTextPlanningProvider CreateProvider(HttpClient httpClient)
    {
        return new OpenAiTextPlanningProvider(
            httpClient,
            new OpenAiProviderOptions { RealApiEnabled = true },
            new StaticSecretStore("test-openai-key"));
    }

    private static HttpResponseMessage JsonResponse(string content)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json"),
        };
    }

    private sealed class CaptureHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler, IDisposable
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        public string? LastRequestBody { get; private set; }

        public int CallCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return responseFactory(request);
        }
    }

    private sealed class StaticSecretStore(string? value) : IOpenAiSecretStore
    {
        public Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(value);
        }
    }
}
