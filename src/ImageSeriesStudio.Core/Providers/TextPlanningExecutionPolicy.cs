namespace ImageSeriesStudio.Core.Providers;

public static class TextPlanningExecutionPolicy
{
    public const int DefaultMaxInputCharacters = 4000;

    public const bool StoreResponsesByDefault = false;
    public const bool AllowPreviousResponseIdByDefault = false;

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
