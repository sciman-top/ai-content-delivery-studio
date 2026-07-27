using System.Text.Json;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.OpenAI;
using OpenAI.Responses;

namespace ContentDeliveryStudio.Tests;

#pragma warning disable OPENAI001 // Contract tests exercise the adopted SDK Responses surface.
public sealed class OpenAiScientificUnderstandingContractTests
{
    [Fact]
    public async Task Provider_UsesBoundedStrictStatelessResponseAndMapsAnchoredOutput()
    {
        var extraction = ScientificUnderstandingProviderTests.Extraction(
            ("block-force", "Net force causes acceleration for constant mass."));
        var request = new ScientificUnderstandingChunkRequest(
            extraction,
            "Explain the force-acceleration mechanism.",
            ChunkIndex: 0,
            ChunkCount: 1,
            extraction.Blocks);
        var client = new CapturingResponsesClient(Response(
            """
            {
              "terms": [{"termId":"term-force","canonicalTerm":"net force","definition":"vector sum of forces","aliases":[],"sourceBlockId":"block-force"}],
              "claims": [{"mergeKey":"force-acceleration","category":"CausalRelation","normalizedStatement":"Net force causes acceleration for constant mass.","sourceWording":"Net force causes acceleration for constant mass.","confidence":0.98,"status":"Accepted","sourceBlockId":"block-force","quotedText":"Net force causes acceleration for constant mass.","evidenceRole":"Support","evidenceValidationState":"Validated"}],
              "limitations": [],
              "conflicts": [],
              "figureProposal": {"centralMessage":"Net force changes motion.","elements":[{"proposalId":"force","meaning":"net force","sourceBlockIds":["block-force"]}],"relations":[]}
            }
            """));
        var provider = Provider(client);

        var result = await provider.AnalyzeChunkAsync(request, CancellationToken.None);

        Assert.False(result.IsBlocked);
        Assert.Equal("resp_scientific_123", result.ProviderTraceId);
        Assert.Single(result.Claims);
        Assert.Single(result.Terms);
        Assert.NotNull(result.FigureProposal);
        Assert.NotNull(client.LastOptions);
        Assert.Equal("gpt-5", client.LastOptions!.Model);
        Assert.False(client.LastOptions.StoredOutputEnabled);
        Assert.Equal(4000, client.LastOptions.MaxOutputTokenCount);
        Assert.NotNull(client.LastOptions.TextOptions?.TextFormat);
        Assert.Single(client.LastOptions.InputItems);

        var input = OpenAiScientificUnderstandingMapper.BuildInput(request);
        Assert.Contains("block-force", input, StringComparison.Ordinal);
        Assert.Contains("pageNumber", input, StringComparison.Ordinal);
        Assert.Contains("characterRange", input, StringComparison.Ordinal);
        Assert.Contains("chunkIndex", input, StringComparison.Ordinal);
        using var schema = JsonDocument.Parse(
            OpenAiScientificUnderstandingMapper.CreateSchemaBinaryData().ToString());
        Assert.False(schema.RootElement.GetProperty("additionalProperties").GetBoolean());
        Assert.Contains(
            "figureProposal",
            schema.RootElement.GetProperty("required").EnumerateArray()
                .Select(item => item.GetString()));
    }

    [Theory]
    [InlineData("{ definitely not json", "invalid-json")]
    [InlineData("{\"terms\":[],\"claims\":[{\"mergeKey\":\"x\",\"category\":\"Mechanism\",\"normalizedStatement\":\"invented\",\"sourceWording\":\"invented\",\"confidence\":0.9,\"status\":\"Accepted\",\"sourceBlockId\":\"unknown\",\"quotedText\":\"invented\",\"evidenceRole\":\"Support\",\"evidenceValidationState\":\"Validated\"}],\"limitations\":[],\"conflicts\":[],\"figureProposal\":null}", "unknown-source-block")]
    [InlineData("{\"terms\":[],\"claims\":[],\"limitations\":[],\"conflicts\":[],\"figureProposal\":null}", "no-scientific-claims")]
    public async Task Provider_TurnsMalformedOrUnanchoredOutputIntoBlockedChunk(
        string output,
        string expectedCode)
    {
        var extraction = ScientificUnderstandingProviderTests.Extraction(
            ("block-force", "Net force causes acceleration."));
        var provider = Provider(new CapturingResponsesClient(Response(output)));

        var result = await provider.AnalyzeChunkAsync(
            new ScientificUnderstandingChunkRequest(
                extraction,
                "Explain force.",
                0,
                1,
                extraction.Blocks),
            CancellationToken.None);

        Assert.True(result.IsBlocked);
        Assert.Contains(result.BlockingCodes, code => code.Contains(expectedCode, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Provider_FlagsConflictingStatementsWithSameMergeKey()
    {
        var extraction = ScientificUnderstandingProviderTests.Extraction(
            ("block-a", "Acceleration follows net force."),
            ("block-b", "Acceleration opposes net force."));
        var client = new CapturingResponsesClient(Response(
            """
            {
              "terms": [],
              "claims": [
                {"mergeKey":"direction","category":"CausalRelation","normalizedStatement":"Acceleration follows net force.","sourceWording":"Acceleration follows net force.","confidence":0.95,"status":"Accepted","sourceBlockId":"block-a","quotedText":"Acceleration follows net force.","evidenceRole":"Support","evidenceValidationState":"Validated"},
                {"mergeKey":"direction","category":"CausalRelation","normalizedStatement":"Acceleration opposes net force.","sourceWording":"Acceleration opposes net force.","confidence":0.95,"status":"Accepted","sourceBlockId":"block-b","quotedText":"Acceleration opposes net force.","evidenceRole":"Support","evidenceValidationState":"Validated"}
              ],
              "limitations": [],
              "conflicts": [{"conflictId":"direction-conflict","firstMergeKey":"direction","secondMergeKey":"direction","description":"Statements disagree."}],
              "figureProposal": null
            }
            """));

        var result = await Provider(client).AnalyzeChunkAsync(
            new ScientificUnderstandingChunkRequest(
                extraction,
                "Resolve direction.",
                0,
                1,
                extraction.Blocks),
            CancellationToken.None);

        Assert.True(result.IsBlocked);
        Assert.Contains("conflicting-claims:direction", result.BlockingCodes);
        Assert.Equal(2, result.Claims.Count);
    }

    [Fact]
    public async Task ApplicationService_PropagatesProviderBlockingCodesIntoDomainDraft()
    {
        var extraction = ScientificUnderstandingProviderTests.Extraction(
            ("block-force", "Net force causes acceleration."));
        var client = new CapturingResponsesClient(Response(
            """
            {
              "terms": [{"termId":"term-invalid","canonicalTerm":"force","definition":"unsupported anchor","aliases":[],"sourceBlockId":"unknown"}],
              "claims": [{"mergeKey":"force","category":"CausalRelation","normalizedStatement":"Net force causes acceleration.","sourceWording":"Net force causes acceleration.","confidence":0.95,"status":"Accepted","sourceBlockId":"block-force","quotedText":"Net force causes acceleration.","evidenceRole":"Support","evidenceValidationState":"Validated"}],
              "limitations": [],
              "conflicts": [],
              "figureProposal": null
            }
            """));
        var service = new ScientificFigureApplicationService(
            Provider(client),
            new InMemoryScientificFigureWorkflowRepository());

        var draft = await service.CreateDraftAsync(
            Guid.NewGuid(),
            extraction,
            ScientificUnderstandingProviderTests.DraftRequest(),
            DateTimeOffset.Parse("2026-07-27T10:00:00Z"),
            CancellationToken.None);

        Assert.Equal(ScientificUnderstandingStatus.Blocked, draft.Understanding.Status);
        Assert.Equal(ScientificFigureSpecStatus.Blocked, draft.Workflow.Specification.Status);
        Assert.Contains(
            draft.Understanding.BlockingCodes,
            code => code.Contains("provider-output", StringComparison.Ordinal));
    }

    private static OpenAiScientificUnderstandingProvider Provider(
        IOpenAiResponsesClient client)
    {
        return new OpenAiScientificUnderstandingProvider(
            new OpenAiProviderOptions { RealApiEnabled = true },
            client,
            new StaticSecretStore());
    }

    private static OpenAiResponsesClientResult Response(string output)
    {
        using var body = JsonDocument.Parse(
            JsonSerializer.Serialize(new { id = "resp_scientific_123", output_text = output }));
        return new OpenAiResponsesClientResult(
            output,
            body.RootElement.Clone(),
            200,
            "OK",
            "req_scientific_123");
    }

    private sealed class CapturingResponsesClient(OpenAiResponsesClientResult response)
        : IOpenAiResponsesClient
    {
        public CreateResponseOptions? LastOptions { get; private set; }

        public Task<OpenAiResponsesClientResult> CreateResponseAsync(
            CreateResponseOptions options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastOptions = options;
            return Task.FromResult(response);
        }
    }

    private sealed class StaticSecretStore : IOpenAiSecretStore
    {
        public Task<string?> GetSecretAsync(
            string secretName,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<string?>("test-openai-key");
        }
    }
}
#pragma warning restore OPENAI001
