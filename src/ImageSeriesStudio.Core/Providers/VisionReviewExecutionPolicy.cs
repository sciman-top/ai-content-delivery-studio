namespace ImageSeriesStudio.Core.Providers;

public static class VisionReviewExecutionPolicy
{
    public const int DefaultBatchItemLimit = 6;
    public const int DefaultHighRiskBatchItemLimit = 4;
    public const int DefaultCompactSummaryCharacters = 280;

    public const bool StoreResponsesByDefault = false;
    public const bool AllowPreviousResponseIdByDefault = false;

    public static VisionReviewOperatorDescriptor CreateOperatorDescriptor(string providerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        var requiresCompactLocalArtifacts = RequiresCompactLocalArtifacts(providerId);
        return new VisionReviewOperatorDescriptor(
            requiresCompactLocalArtifacts ? "local-direct-stateless" : "fake-local-review",
            DefaultBatchItemLimit,
            DefaultHighRiskBatchItemLimit,
            DefaultCompactSummaryCharacters,
            StoreResponsesByDefault,
            AllowPreviousResponseIdByDefault,
            requiresCompactLocalArtifacts);
    }

    public static bool RequiresCompactLocalArtifacts(string providerId)
    {
        return !providerId.StartsWith("fake", StringComparison.OrdinalIgnoreCase);
    }

    public static ReviewPrepArtifactContract CreateMinimalLocalContract(
        string itemTitle,
        string promptText)
    {
        return new ReviewPrepArtifactContract(
            Summary: CreateCompactSummary(itemTitle, promptText),
            EvidenceSelections:
            [
                new ReviewPrepEvidenceSelection(
                    Role: "prompt-summary",
                    SourceKind: "prompt-text",
                    LocalPath: null,
                    Summary: CreateCompactPromptSummary(promptText)),
            ]);
    }

    public static string CreateCompactSummary(string itemTitle, string promptText)
    {
        return TrimToLimit(
            $"Local review prep for {itemTitle}: prompt summary '{promptText}'.",
            DefaultCompactSummaryCharacters);
    }

    public static string CreateCompactPromptSummary(string promptText)
    {
        return TrimToLimit(promptText, DefaultCompactSummaryCharacters);
    }

    private static string TrimToLimit(string value, int limit)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = string.Join(" ", value
            .Split(['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        if (normalized.Length <= limit)
        {
            return normalized;
        }

        return normalized[..Math.Max(0, limit - 3)] + "...";
    }
}

public sealed record VisionReviewOperatorDescriptor(
    string ExecutionMode,
    int BatchItemLimit,
    int HighRiskBatchItemLimit,
    int CompactSummaryCharacterLimit,
    bool UsesStoredResponses,
    bool AllowsPreviousResponseId,
    bool RequiresCompactLocalArtifacts);
