using System.Net;
using System.Text;
using System.Text.Json;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.OpenAI;
using Microsoft.Extensions.DependencyInjection;

namespace ContentDeliveryStudio.Tests;

public sealed class OpenAiScientificReviewContractTests
{
    [Fact]
    public void ServiceCollection_ResolvesScientificReviewProviderFromNamedClient()
    {
        var services = new ServiceCollection();
        services.AddOpenAiProviderHttpClient(new OpenAiProviderOptions());

        using var serviceProvider = services.BuildServiceProvider();

        Assert.IsType<OpenAiScientificReviewProvider>(
            serviceProvider.GetRequiredService<OpenAiScientificReviewProvider>());
    }

    [Fact]
    public async Task SemanticReview_SendsApprovedEvidenceAndRenderItemsWithStrictStatelessSchema()
    {
        var fixture = ScientificReviewTestFixture.Create();
        var handler = new CapturingHandler(Response("Pass", []));
        var provider = Provider(handler);

        var result = await provider.ReviewAsync(fixture.SemanticRequest, CancellationToken.None);

        Assert.Equal(ScientificReviewVerdict.Pass, result.Verdict);
        using var payload = JsonDocument.Parse(handler.Body!);
        var root = payload.RootElement;
        Assert.False(root.GetProperty("store").GetBoolean());
        Assert.True(root.GetProperty("text").GetProperty("format").GetProperty("strict").GetBoolean());
        var inputText = root.GetProperty("input")[0].GetProperty("content")[0].GetProperty("text").GetString();
        Assert.Contains("block-dynamics", inputText, StringComparison.Ordinal);
        Assert.Contains("element-force", inputText, StringComparison.Ordinal);
        Assert.Contains("approvedClaims", inputText, StringComparison.Ordinal);
        Assert.DoesNotContain("input_image", handler.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VisualReview_SendsExpectedChecksAndUsesOriginalDetailForGpt56()
    {
        var fixture = ScientificReviewTestFixture.Create();
        var handler = new CapturingHandler(Response("Pass", []));
        var provider = Provider(handler);

        await provider.ReviewAsync(fixture.VisualRequest, CancellationToken.None);

        using var payload = JsonDocument.Parse(handler.Body!);
        var content = payload.RootElement.GetProperty("input")[0].GetProperty("content");
        Assert.Equal(3, content.GetArrayLength());
        Assert.Equal("input_text", content[0].GetProperty("type").GetString());
        Assert.All(content.EnumerateArray().Skip(1), part =>
        {
            Assert.Equal("input_image", part.GetProperty("type").GetString());
            Assert.Equal("original", part.GetProperty("detail").GetString());
            Assert.StartsWith("data:image/png;base64,", part.GetProperty("image_url").GetString(), StringComparison.Ordinal);
        });
        var metadata = content[0].GetProperty("text").GetString();
        Assert.Contains("1200", metadata, StringComparison.Ordinal);
        Assert.Contains("Element", metadata, StringComparison.Ordinal);
        Assert.Contains("element-force", metadata, StringComparison.Ordinal);
        Assert.Contains("ScientificMeaning", metadata, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ApprovedSpecification", metadata, StringComparison.Ordinal);
        Assert.DoesNotContain("384", handler.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VisualReview_FallsBackToHighWhenModelDoesNotSupportOriginalDetail()
    {
        var fixture = ScientificReviewTestFixture.Create();
        var handler = new CapturingHandler(Response("Pass", []));
        var provider = Provider(handler, new OpenAiProviderOptions
        {
            RealApiEnabled = true,
            VisionReviewModel = "gpt-5",
        });

        await provider.ReviewAsync(fixture.VisualRequest, CancellationToken.None);

        using var payload = JsonDocument.Parse(handler.Body!);
        var content = payload.RootElement.GetProperty("input")[0].GetProperty("content");
        Assert.All(content.EnumerateArray().Skip(1), part =>
            Assert.Equal("high", part.GetProperty("detail").GetString()));
    }

    [Theory]
    [InlineData("not-json", "invalid-provider-output")]
    [InlineData("{\"verdict\":\"Uncertain\",\"findings\":[]}", "provider-uncertain")]
    [InlineData("{\"verdict\":\"Fail\",\"findings\":[]}", "missing-provider-findings")]
    [InlineData("{\"verdict\":\"Fail\",\"findings\":[{\"code\":\"mismatch\",\"kind\":\"ScientificMismatch\",\"responsibleItemId\":\"unknown\",\"evidence\":\"Wrong direction.\"}]}", "unknown-responsible-item")]
    public async Task Provider_FailsClosedForMalformedUncertainOrIncompleteOutput(
        string output,
        string expectedCode)
    {
        var fixture = ScientificReviewTestFixture.Create();
        var handler = new CapturingHandler(RawResponse(output));
        var provider = Provider(handler);
        var service = new ScientificReviewExecutionService(provider, provider);

        var decision = await service.ReviewAsync(
            fixture.SemanticRequest,
            fixture.VisualRequest,
            CancellationToken.None);

        Assert.False(decision.CanProceedToGate2);
        Assert.Contains(decision.Blockers, blocker => blocker.Code == expectedCode);
    }

    [Fact]
    public async Task Provider_RetriesTransientStatusWithinBound()
    {
        var fixture = ScientificReviewTestFixture.Create();
        var handler = new SequenceHandler(
            new HttpResponseMessage(HttpStatusCode.TooManyRequests),
            JsonResponse(Response("Pass", [])));

        var result = await Provider(handler).ReviewAsync(
            fixture.SemanticRequest,
            CancellationToken.None);

        Assert.Equal(ScientificReviewVerdict.Pass, result.Verdict);
        Assert.Equal(2, handler.InvocationCount);
    }

    [Fact]
    public async Task Provider_DoesNotRetryNonTransientStatus()
    {
        var fixture = ScientificReviewTestFixture.Create();
        var handler = new SequenceHandler(new HttpResponseMessage(HttpStatusCode.BadRequest));

        await Assert.ThrowsAsync<HttpRequestException>(() => Provider(handler).ReviewAsync(
            fixture.SemanticRequest,
            CancellationToken.None));

        Assert.Equal(1, handler.InvocationCount);
    }

    private static OpenAiScientificReviewProvider Provider(
        HttpMessageHandler handler,
        OpenAiProviderOptions? options = null)
    {
        return new OpenAiScientificReviewProvider(
            new HttpClient(handler),
            options ?? new OpenAiProviderOptions { RealApiEnabled = true },
            new StaticSecretStore());
    }

    private static string Response(string verdict, object[] findings)
    {
        return RawResponse(JsonSerializer.Serialize(new { verdict, findings }));
    }

    private static string RawResponse(string output)
    {
        return JsonSerializer.Serialize(new { id = "resp_scientific_review_123", output_text = output });
    }

    private static HttpResponseMessage JsonResponse(string body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json"),
    };

    private sealed class CapturingHandler(string responseBody) : HttpMessageHandler
    {
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed class SequenceHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        public int InvocationCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            InvocationCount++;
            return Task.FromResult(_responses.Dequeue());
        }
    }

    private sealed class StaticSecretStore : IOpenAiSecretStore
    {
        public Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken)
        {
            return Task.FromResult<string?>("test-openai-key");
        }
    }
}
