using System.Text;
using System.Text.Json;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Infrastructure.OpenAI;
using System.ClientModel;

namespace ContentDeliveryStudio.Tests;

public sealed class OpenAiSdkImageTransportTests
{
    [Fact]
    public async Task GenerateAsync_UsesOfficialImageClientAndPreservesStoreFalse()
    {
        var imageBytes = new byte[] { 137, 80, 78, 71 };
        var fakeBackend = new FakeSdkImageBackend(
            """
            {
              "id": "img_sdk_123",
              "data": [
                {
                  "b64_json": "iVBORw=="
                }
              ]
            }
            """,
            requestId: "req_sdk_123");
        var sdkTransport = new OpenAiSdkImageTransport(fakeBackend);
        var options = new OpenAiProviderOptions
        {
            BaseUri = new Uri("https://api.openai.com/v1/"),
            ImageGenerationModel = "gpt-image-2",
            RealApiEnabled = true,
        };

        var result = await sdkTransport.GenerateAsync(
            options,
            "test-openai-key",
            appId: "app-id",
            appSecret: "app-secret",
            new ImageGenerationRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Create a clean science poster.",
                new GenerationSettings(1024, 1024, "standard", "png"),
                Path.GetTempPath()),
            CancellationToken.None);

        Assert.Equal("img_sdk_123", result.ProviderTraceId);
        Assert.Equal("req_sdk_123", result.RequestId);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(imageBytes, result.ImageBytes);
        Assert.Equal("app-id", fakeBackend.LastAppId);
        Assert.Equal("app-secret", fakeBackend.LastAppSecret);

        using var payload = JsonDocument.Parse(fakeBackend.LastPayload!);
        Assert.Equal("gpt-image-2", payload.RootElement.GetProperty("model").GetString());
        Assert.Equal("Create a clean science poster.", payload.RootElement.GetProperty("prompt").GetString());
        Assert.Equal(1, payload.RootElement.GetProperty("n").GetInt32());
        Assert.False(payload.RootElement.GetProperty("store").GetBoolean());
    }

    [Fact]
    public async Task GenerateAsync_RejectsResponseWithoutBase64ImageData()
    {
        var fakeBackend = new FakeSdkImageBackend(
            """
            {
              "id": "img_sdk_missing_data",
              "data": [
                {
                  "url": "https://example.test/generated.png"
                }
              ]
            }
            """,
            requestId: "req_sdk_missing_data");
        var sdkTransport = new OpenAiSdkImageTransport(fakeBackend);
        var options = new OpenAiProviderOptions
        {
            BaseUri = new Uri("https://api.openai.com/v1/"),
            ImageGenerationModel = "gpt-image-2",
            RealApiEnabled = true,
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sdkTransport.GenerateAsync(
                options,
                "test-openai-key",
                appId: null,
                appSecret: null,
                new ImageGenerationRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Create a clean science poster.",
                    new GenerationSettings(1024, 1024, "standard", "png"),
                    Path.GetTempPath()),
                CancellationToken.None));

        Assert.Contains("base64", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeSdkImageBackend : IOpenAiSdkImageBackend
    {
        private readonly string _responseJson;
        private readonly string _requestId;

        public FakeSdkImageBackend(string responseJson, string requestId)
        {
            _responseJson = responseJson;
            _requestId = requestId;
        }

        public string? LastPayload { get; private set; }

        public string? LastAppId { get; private set; }

        public string? LastAppSecret { get; private set; }

        public Task<OpenAiSdkImageBackendResponse> SendAsync(
            OpenAiProviderOptions options,
            string apiKey,
            string? appId,
            string? appSecret,
            BinaryContent payload,
            CancellationToken cancellationToken)
        {
            using var stream = new MemoryStream();
            payload.WriteTo(stream, cancellationToken);
            LastPayload = Encoding.UTF8.GetString(stream.ToArray());
            LastAppId = appId;
            LastAppSecret = appSecret;

            return Task.FromResult(new OpenAiSdkImageBackendResponse(
                200,
                _requestId,
                BinaryData.FromString(_responseJson)));
        }
    }
}
