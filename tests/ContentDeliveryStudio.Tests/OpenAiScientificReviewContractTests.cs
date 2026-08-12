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
    public async Task VisualReview_AutoRouteKeepsPayloadAndCheckpointIdentityAligned()
    {
        var fixture = ScientificReviewTestFixture.Create();
        var handler = new CapturingHandler(Response("Pass", []));
        var checkpointStore = new CapturingCheckpointStore();
        var provider = Provider(
            handler,
            new OpenAiProviderOptions
            {
                RealApiEnabled = true,
                TextRoutingMode = OpenAiTextRoutingMode.Auto,
            },
            checkpointStore);

        await provider.ReviewAsync(fixture.VisualRequest, CancellationToken.None);

        Assert.NotNull(checkpointStore.LoadIdentity);
        Assert.Equal(checkpointStore.LoadIdentity, checkpointStore.SaveIdentity);
        Assert.Equal("gpt-5.6-sol", checkpointStore.LoadIdentity!.Model);
        Assert.Equal("xhigh", checkpointStore.LoadIdentity.ReasoningEffort);
        using var payload = JsonDocument.Parse(handler.Body!);
        Assert.Equal("gpt-5.6-sol", payload.RootElement.GetProperty("model").GetString());
        Assert.Equal(
            "xhigh",
            payload.RootElement.GetProperty("reasoning").GetProperty("effort").GetString());
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

    [Fact]
    public async Task Provider_ResumesExactRequestAcrossInstancesWithoutAnotherApiCall()
    {
        var directory = TemporaryDirectory();
        try
        {
            var fixture = ScientificReviewTestFixture.Create();
            var store = new JsonOpenAiScientificReviewCheckpointStore(directory);
            var firstHandler = new SequenceHandler(JsonResponse(Response("Pass", [])));
            var first = Provider(firstHandler, checkpointStore: store);

            var initial = await first.ReviewAsync(fixture.SemanticRequest, CancellationToken.None);

            var resumedHandler = new SequenceHandler();
            var resumed = await Provider(
                    resumedHandler,
                    checkpointStore: store,
                    secretStore: new ThrowingSecretStore())
                .ReviewAsync(fixture.SemanticRequest, CancellationToken.None);

            Assert.Equal(ScientificReviewVerdict.Pass, initial.Verdict);
            Assert.Equal(initial.Verdict, resumed.Verdict);
            Assert.Equal(initial.Findings, resumed.Findings);
            Assert.Equal(initial.ProviderTraceId, resumed.ProviderTraceId);
            Assert.Equal(ScientificProviderReviewOrigin.ProviderResponse, initial.Origin);
            Assert.Equal(ScientificProviderReviewOrigin.PersistedCheckpoint, resumed.Origin);
            Assert.Equal(1, firstHandler.InvocationCount);
            Assert.Equal(0, resumedHandler.InvocationCount);
            Assert.Single(Directory.GetFiles(directory, "*.json"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Provider_DoesNotResumeWhenModelOrPayloadChanges()
    {
        var directory = TemporaryDirectory();
        try
        {
            var fixture = ScientificReviewTestFixture.Create();
            var store = new JsonOpenAiScientificReviewCheckpointStore(directory);
            await Provider(
                    new SequenceHandler(JsonResponse(Response("Pass", []))),
                    checkpointStore: store)
                .ReviewAsync(fixture.VisualRequest, CancellationToken.None);

            var changedModelHandler = new SequenceHandler(JsonResponse(Response("Pass", [])));
            await Provider(
                    changedModelHandler,
                    new OpenAiProviderOptions
                    {
                        RealApiEnabled = true,
                        VisionReviewModel = "gpt-5.5",
                    },
                    store)
                .ReviewAsync(fixture.VisualRequest, CancellationToken.None);

            var changedImage = fixture.VisualRequest.FullResolutionOutput with
            {
                Bytes = [9, 8, 7, 6],
                Sha256 = $"sha256:{new string('b', 64)}",
            };
            var changedRequest = ScientificVisualReviewRequest.Create(
                changedImage,
                fixture.VisualRequest.RegionCrops);
            var changedPayloadHandler = new SequenceHandler(JsonResponse(Response("Pass", [])));
            await Provider(changedPayloadHandler, checkpointStore: store)
                .ReviewAsync(changedRequest, CancellationToken.None);

            Assert.Equal(1, changedModelHandler.InvocationCount);
            Assert.Equal(1, changedPayloadHandler.InvocationCount);
            Assert.Equal(3, Directory.GetFiles(directory, "*.json").Length);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Provider_FailsClosedForCorruptedCheckpointWithoutCallingApi()
    {
        var directory = TemporaryDirectory();
        try
        {
            var fixture = ScientificReviewTestFixture.Create();
            var store = new JsonOpenAiScientificReviewCheckpointStore(directory);
            await Provider(
                    new SequenceHandler(JsonResponse(Response("Pass", []))),
                    checkpointStore: store)
                .ReviewAsync(fixture.SemanticRequest, CancellationToken.None);
            await File.WriteAllTextAsync(Assert.Single(Directory.GetFiles(directory, "*.json")), "{");
            var handler = new SequenceHandler(JsonResponse(Response("Pass", [])));

            await Assert.ThrowsAsync<InvalidDataException>(() =>
                Provider(handler, checkpointStore: store)
                    .ReviewAsync(fixture.SemanticRequest, CancellationToken.None));

            Assert.Equal(0, handler.InvocationCount);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Provider_FailsClosedForIdentityTamperingWithoutCallingApi()
    {
        var directory = TemporaryDirectory();
        try
        {
            var fixture = ScientificReviewTestFixture.Create();
            var store = new JsonOpenAiScientificReviewCheckpointStore(directory);
            await Provider(
                    new SequenceHandler(JsonResponse(Response("Pass", []))),
                    checkpointStore: store)
                .ReviewAsync(fixture.SemanticRequest, CancellationToken.None);
            var checkpointPath = Assert.Single(Directory.GetFiles(directory, "*.json"));
            var checkpoint = await File.ReadAllTextAsync(checkpointPath);
            await File.WriteAllTextAsync(
                checkpointPath,
                checkpoint.Replace("gpt-5.6-sol", "gpt-5.5", StringComparison.Ordinal));
            var handler = new SequenceHandler(JsonResponse(Response("Pass", [])));

            await Assert.ThrowsAsync<InvalidDataException>(() =>
                Provider(handler, checkpointStore: store)
                    .ReviewAsync(fixture.SemanticRequest, CancellationToken.None));

            Assert.Equal(0, handler.InvocationCount);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Provider_DoesNotReuseCheckpointWhenRealApiIsDisabled()
    {
        var directory = TemporaryDirectory();
        try
        {
            var fixture = ScientificReviewTestFixture.Create();
            var store = new JsonOpenAiScientificReviewCheckpointStore(directory);
            await Provider(
                    new SequenceHandler(JsonResponse(Response("Pass", []))),
                    checkpointStore: store)
                .ReviewAsync(fixture.SemanticRequest, CancellationToken.None);
            var handler = new SequenceHandler(JsonResponse(Response("Pass", [])));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Provider(
                        handler,
                        new OpenAiProviderOptions { RealApiEnabled = false },
                        store,
                        new ThrowingSecretStore())
                    .ReviewAsync(fixture.SemanticRequest, CancellationToken.None));

            Assert.Contains("disabled", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, handler.InvocationCount);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Provider_ResumesFailVerdictWithoutUpgradingItAndCheckpointExcludesPayloadAndSecret()
    {
        var directory = TemporaryDirectory();
        try
        {
            var fixture = ScientificReviewTestFixture.Create();
            var finding = new
            {
                code = "wrong-force",
                kind = "ScientificMismatch",
                responsibleItemId = "element-force",
                evidence = "Force direction is reversed.",
            };
            var store = new JsonOpenAiScientificReviewCheckpointStore(directory);
            var first = await Provider(
                    new SequenceHandler(JsonResponse(Response("Fail", [finding]))),
                    checkpointStore: store)
                .ReviewAsync(fixture.VisualRequest, CancellationToken.None);
            var resumedHandler = new SequenceHandler(JsonResponse(Response("Pass", [])));

            var resumed = await Provider(resumedHandler, checkpointStore: store)
                .ReviewAsync(fixture.VisualRequest, CancellationToken.None);

            Assert.Equal(ScientificReviewVerdict.Fail, first.Verdict);
            Assert.Equal(ScientificReviewVerdict.Fail, resumed.Verdict);
            Assert.Equal(ScientificProviderReviewOrigin.PersistedCheckpoint, resumed.Origin);
            Assert.Equal(0, resumedHandler.InvocationCount);
            var checkpoint = await File.ReadAllTextAsync(
                Assert.Single(Directory.GetFiles(directory, "*.json")));
            Assert.DoesNotContain("test-openai-key", checkpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("image_url", checkpoint, StringComparison.Ordinal);
            Assert.DoesNotContain(Convert.ToBase64String(fixture.VisualRequest.FullResolutionOutput.Bytes), checkpoint, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static OpenAiScientificReviewProvider Provider(
        HttpMessageHandler handler,
        OpenAiProviderOptions? options = null,
        IOpenAiScientificReviewCheckpointStore? checkpointStore = null,
        IOpenAiSecretStore? secretStore = null)
    {
        return new OpenAiScientificReviewProvider(
            new HttpClient(handler),
            options ?? new OpenAiProviderOptions { RealApiEnabled = true },
            secretStore ?? new StaticSecretStore(),
            checkpointStore: checkpointStore);
    }

    private static string TemporaryDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "ContentDeliveryStudio.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
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

    private sealed class ThrowingSecretStore : IOpenAiSecretStore
    {
        public Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Checkpoint resume must not read provider secrets.");
        }
    }

    private sealed class CapturingCheckpointStore : IOpenAiScientificReviewCheckpointStore
    {
        public OpenAiScientificReviewCheckpointIdentity? LoadIdentity { get; private set; }

        public OpenAiScientificReviewCheckpointIdentity? SaveIdentity { get; private set; }

        public Task<ScientificProviderReviewResult?> TryLoadAsync(
            OpenAiScientificReviewCheckpointIdentity identity,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LoadIdentity = identity;
            return Task.FromResult<ScientificProviderReviewResult?>(null);
        }

        public Task SaveAsync(
            OpenAiScientificReviewCheckpointIdentity identity,
            ScientificProviderReviewResult result,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SaveIdentity = identity;
            return Task.CompletedTask;
        }
    }
}
