using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Infrastructure.OpenAI;

namespace ContentDeliveryStudio.Tests;

public sealed class OpenAiOfficialSdkImageGenerationProviderTests
{
    [Fact]
    public async Task GenerateImageAsync_UsesSdkTransportAndWritesMetadata()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        var transport = new FakeOpenAiSdkImageTransport(
            new OpenAiSdkImageTransportResult(
                [137, 80, 78, 71],
                "img_sdk_provider_123",
                200,
                "req_sdk_provider_123",
                BinaryData.FromString(
                    """
                    {
                      "id": "img_sdk_provider_123",
                      "data": [
                        {
                          "b64_json": "iVBORw=="
                        }
                      ]
                    }
                    """)));
        var telemetrySink = new RecordingTelemetrySink();
        var provider = new OpenAiOfficialSdkImageGenerationProvider(
            new OpenAiProviderOptions { RealApiEnabled = true },
            new StaticSecretStore("test-openai-key"),
            transport,
            telemetrySink,
            new OpenAiCostRateCard("test-card", 0.03m, 0.20m, 0.01m));

        try
        {
            var result = await provider.GenerateImageAsync(
                new ImageGenerationRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Create a clean science poster.",
                    new GenerationSettings(1024, 1024, "standard", "png"),
                    rootDirectory,
                    "science-poster.png"),
                CancellationToken.None);

            Assert.Equal("img_sdk_provider_123", result.ProviderTraceId);
            Assert.True(File.Exists(result.AssetPath));
            Assert.True(File.Exists(result.MetadataPath));

            var telemetry = Assert.Single(telemetrySink.Events);
            Assert.Equal("openai-image-sdk", telemetry.ProviderId);
            Assert.Equal("image-generation", telemetry.Operation);
            Assert.Equal("req_sdk_provider_123", telemetry.RequestId);

            using var metadata = JsonDocument.Parse(await File.ReadAllTextAsync(result.MetadataPath, CancellationToken.None));
            Assert.Equal("openai-image-sdk", metadata.RootElement.GetProperty("providerId").GetString());
            Assert.Equal("images", metadata.RootElement.GetProperty("endpointFamily").GetString());
            Assert.False(metadata.RootElement.GetProperty("store").GetBoolean());
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task GenerateImageAsync_DoesNotCallSdkTransportWhenRealApiIsDisabled()
    {
        var transport = new FakeOpenAiSdkImageTransport(
            new OpenAiSdkImageTransportResult([], "unused", 200, null, BinaryData.FromString("{}")));
        var provider = new OpenAiOfficialSdkImageGenerationProvider(
            new OpenAiProviderOptions(),
            new StaticSecretStore("test-openai-key"),
            transport);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.GenerateImageAsync(
                new ImageGenerationRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "prompt",
                    new GenerationSettings(1024, 1024, "standard", "png"),
                    Path.GetTempPath()),
                CancellationToken.None));

        Assert.Equal(0, transport.CallCount);
    }

    [Fact]
    public async Task GenerateImageAsync_MapsSdkFailuresToHttpRequestExceptionAndRecordsFailureTelemetry()
    {
        var telemetrySink = new RecordingTelemetrySink();
        var transport = new ThrowingOpenAiSdkImageTransport(429);
        var provider = new OpenAiOfficialSdkImageGenerationProvider(
            new OpenAiProviderOptions { RealApiEnabled = true },
            new StaticSecretStore("test-openai-key"),
            transport,
            telemetrySink,
            new OpenAiCostRateCard("test-card", 0.03m, 0.20m, 0.01m));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            provider.GenerateImageAsync(
                new ImageGenerationRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "prompt",
                    new GenerationSettings(1024, 1024, "standard", "png"),
                    Path.GetTempPath()),
                CancellationToken.None));

        Assert.Contains("429", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, transport.CallCount);

        var telemetry = Assert.Single(telemetrySink.Events);
        Assert.Equal("openai-image-sdk", telemetry.ProviderId);
        Assert.Equal(429, telemetry.HttpStatusCode);
        Assert.False(telemetry.Succeeded);
        Assert.Equal(0m, telemetry.EstimatedCostUsd);
        Assert.Null(telemetry.RequestId);
    }

    [Fact]
    public async Task GenerateImageAsync_RetriesSingleTransientUpstream502AndThenSucceeds()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        var transport = new SequencedOpenAiSdkImageTransport(
        [
            new ClientResultException(
                "HTTP 502 (server_error: upstream_error)",
                new FakePipelineResponse(502),
                innerException: null),
            new OpenAiSdkImageTransportResult(
                [137, 80, 78, 71],
                "img_sdk_provider_retry",
                200,
                "req_sdk_provider_retry",
                BinaryData.FromString(
                    """
                    {
                      "id": "img_sdk_provider_retry",
                      "data": [
                        {
                          "b64_json": "iVBORw=="
                        }
                      ]
                    }
                    """)),
        ]);
        var telemetrySink = new RecordingTelemetrySink();
        var provider = new OpenAiOfficialSdkImageGenerationProvider(
            new OpenAiProviderOptions { RealApiEnabled = true },
            new StaticSecretStore("test-openai-key"),
            transport,
            telemetrySink,
            new OpenAiCostRateCard("test-card", 0.03m, 0.20m, 0.01m));

        try
        {
            var result = await provider.GenerateImageAsync(
                new ImageGenerationRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Create a clean science poster.",
                    new GenerationSettings(1024, 1024, "standard", "png"),
                    rootDirectory,
                    "science-poster.png"),
                CancellationToken.None);

            Assert.Equal("img_sdk_provider_retry", result.ProviderTraceId);
            Assert.Equal(2, transport.CallCount);

            var telemetry = Assert.Single(telemetrySink.Events);
            Assert.True(telemetry.Succeeded);
            Assert.Equal(200, telemetry.HttpStatusCode);
            Assert.Equal("req_sdk_provider_retry", telemetry.RequestId);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    private sealed class FakeOpenAiSdkImageTransport : IOpenAiSdkImageTransport
    {
        private readonly OpenAiSdkImageTransportResult _result;

        public FakeOpenAiSdkImageTransport(OpenAiSdkImageTransportResult result)
        {
            _result = result;
        }

        public int CallCount { get; private set; }

        public Task<OpenAiSdkImageTransportResult> GenerateAsync(
            OpenAiProviderOptions options,
            string apiKey,
            string? appId,
            string? appSecret,
            ImageGenerationRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_result);
        }
    }

    private sealed class ThrowingOpenAiSdkImageTransport(int statusCode) : IOpenAiSdkImageTransport
    {
        public int CallCount { get; private set; }

        public Task<OpenAiSdkImageTransportResult> GenerateAsync(
            OpenAiProviderOptions options,
            string apiKey,
            string? appId,
            string? appSecret,
            ImageGenerationRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            throw new ClientResultException(
                $"SDK request failed with status {statusCode}.",
                new FakePipelineResponse(statusCode),
                innerException: null);
        }
    }

    private sealed class SequencedOpenAiSdkImageTransport(IReadOnlyList<object> results) : IOpenAiSdkImageTransport
    {
        private int _index;

        public int CallCount { get; private set; }

        public Task<OpenAiSdkImageTransportResult> GenerateAsync(
            OpenAiProviderOptions options,
            string apiKey,
            string? appId,
            string? appSecret,
            ImageGenerationRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            var current = results[_index++];
            if (current is Exception exception)
            {
                throw exception;
            }

            return Task.FromResult((OpenAiSdkImageTransportResult)current);
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

    private sealed class RecordingTelemetrySink : IProviderCallTelemetrySink
    {
        private readonly List<ProviderCallTelemetry> _events = [];

        public IReadOnlyList<ProviderCallTelemetry> Events => _events;

        public void Record(ProviderCallTelemetry telemetry)
        {
            _events.Add(telemetry);
        }
    }

    private sealed class FakePipelineResponse(int statusCode) : PipelineResponse
    {
        private readonly PipelineResponseHeaders _headers = new FakePipelineResponseHeaders();
        private readonly BinaryData _content = BinaryData.FromString("{}");
        private Stream? _contentStream = new MemoryStream();

        public override int Status => statusCode;

        public override string ReasonPhrase => $"HTTP {statusCode}";

        protected override PipelineResponseHeaders HeadersCore => _headers;

        public override Stream? ContentStream
        {
            get => _contentStream;
            set => _contentStream = value;
        }

        public override BinaryData Content => _content;

        public override BinaryData BufferContent(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _content;
        }

        public override ValueTask<BinaryData> BufferContentAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(_content);
        }

        public override void Dispose()
        {
            _contentStream?.Dispose();
        }
    }

    private sealed class FakePipelineResponseHeaders : PipelineResponseHeaders
    {
        public override bool TryGetValue(string name, out string value)
        {
            value = string.Empty;
            return false;
        }

        public override bool TryGetValues(string name, out IEnumerable<string> values)
        {
            values = [];
            return false;
        }

        public override IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return Enumerable.Empty<KeyValuePair<string, string>>().GetEnumerator();
        }
    }
}
