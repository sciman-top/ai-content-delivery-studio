namespace ImageSeriesStudio.Core.Providers;

public static class TextPlanningExecutionPolicy
{
    public const int DefaultMaxInputCharacters = 4000;
    public const int MaxTransientUpstreamRetryCount = 1;

    public const bool StoreResponsesByDefault = false;
    public const bool AllowPreviousResponseIdByDefault = false;
    public static readonly TimeSpan TransientUpstreamRetryDelay = TimeSpan.FromSeconds(2);

    public static TextPlanningOperatorDescriptor CreateOperatorDescriptor()
    {
        return new TextPlanningOperatorDescriptor(
            "local-direct-stateless",
            DefaultMaxInputCharacters,
            MaxTransientUpstreamRetryCount,
            TransientUpstreamRetryDelay,
            StoreResponsesByDefault,
            AllowPreviousResponseIdByDefault);
    }

    public static int EstimateInputCharacters(PlanningRequest request)
    {
        return string.Join(
            Environment.NewLine,
            [
                request.Goal,
                request.Audience,
                request.StyleBrief,
            ]).Length;
    }

    public static int EstimateInputCharacters(BriefPlanningRequest request)
    {
        return string.Join(
            Environment.NewLine,
            [
                request.Goal,
                request.Audience,
                request.StyleIntent,
                string.Join(Environment.NewLine, request.MustInclude),
                string.Join(Environment.NewLine, request.MustAvoid),
            ]).Length;
    }

    public static int EstimateInputCharacters(BlueprintPlanningRequest request)
    {
        return string.Join(
            Environment.NewLine,
            [
                request.Goal,
                request.Audience,
                request.StyleIntent,
                string.Join(Environment.NewLine, request.MustInclude),
                string.Join(Environment.NewLine, request.MustAvoid),
                request.TextPolicy.ToString(),
            ]).Length;
    }
}

public sealed record TextPlanningOperatorDescriptor(
    string ExecutionMode,
    int MaxInputCharacters,
    int MaxTransientUpstreamRetryCount,
    TimeSpan TransientUpstreamRetryDelay,
    bool UsesStoredResponses,
    bool AllowsPreviousResponseId);
