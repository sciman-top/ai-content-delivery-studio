using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Infrastructure.ScientificFigures;

public sealed class FakeScientificUnderstandingProvider : IScientificUnderstandingProvider
{
    public int InvocationCount { get; private set; }

    public Task<ScientificUnderstandingChunkResult> AnalyzeChunkAsync(
        ScientificUnderstandingChunkRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        InvocationCount++;

        var claims = request.Blocks
            .Where(block => !string.IsNullOrWhiteSpace(block.OriginalText))
            .Select(CreateDraft)
            .ToArray();
        return Task.FromResult(new ScientificUnderstandingChunkResult(
            claims,
            "fake-scientific-understanding"));
    }

    private static ScientificUnderstandingClaimDraft CreateDraft(
        ScientificSourceBlock block)
    {
        var sourceText = block.OriginalText!;
        var (mergeKey, statement) = ParseStatement(block.BlockId, sourceText);
        return new ScientificUnderstandingClaimDraft(
            mergeKey,
            Classify(statement),
            statement,
            statement,
            Confidence: 0.99,
            ScientificClaimStatus.Accepted,
            block.BlockId,
            statement,
            ClaimEvidenceRole.Support,
            EvidenceValidationState.Validated);
    }

    private static (string MergeKey, string Statement) ParseStatement(
        string blockId,
        string sourceText)
    {
        const string prefix = "CLAIM[";
        if (!sourceText.StartsWith(prefix, StringComparison.Ordinal))
        {
            return (blockId, sourceText.Trim());
        }

        var keyEnd = sourceText.IndexOf("]:", prefix.Length, StringComparison.Ordinal);
        if (keyEnd < 0)
        {
            return (blockId, sourceText.Trim());
        }

        var mergeKey = sourceText[prefix.Length..keyEnd].Trim();
        var statement = sourceText[(keyEnd + 2)..].Trim();
        return (mergeKey.Length == 0 ? blockId : mergeKey, statement);
    }

    private static ScientificClaimCategory Classify(string statement)
    {
        if (statement.Contains("causes", StringComparison.OrdinalIgnoreCase)
            || statement.Contains("follows", StringComparison.OrdinalIgnoreCase)
            || statement.Contains("opposes", StringComparison.OrdinalIgnoreCase))
        {
            return ScientificClaimCategory.CausalRelation;
        }

        if (statement.Contains(" is ", StringComparison.OrdinalIgnoreCase))
        {
            return ScientificClaimCategory.Definition;
        }

        return ScientificClaimCategory.Mechanism;
    }
}
