namespace ImageSeriesStudio.Core.Projects;

public sealed record RepairPlan(
    Guid Id,
    Guid CandidateImageId,
    RepairSeverity Severity,
    IReadOnlyList<RepairPlanStep> Steps,
    DateTimeOffset CreatedAt)
{
    public bool HasRepair => Severity is not RepairSeverity.None && Steps.Count > 0;

    public bool RequiresOperator => Steps.Any(step => step.RequiresOperator);

    public static RepairPlan FromReview(
        StructuredReviewOutput review,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(review);

        return FromRoutingPlan(ReviewOutcomeRoutingPlan.FromReview(review), createdAt);
    }

    public static RepairPlan FromRoutingPlan(
        ReviewOutcomeRoutingPlan routingPlan,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(routingPlan);

        if (!routingPlan.RequiresRepair)
        {
            return new RepairPlan(
                Guid.NewGuid(),
                routingPlan.CandidateImageId,
                RepairSeverity.None,
                [],
                createdAt);
        }

        var steps = routingPlan.Routes
            .Where(route => route.TargetLayer is not ReviewOutcomeTargetLayer.None)
            .Select((route, index) => RepairPlanStep.Create(
                index + 1,
                route.TargetLayer,
                route.Severity,
                route.Evidence,
                route.Actions,
                requiresOperator: false))
            .ToArray();

        return new RepairPlan(
            Guid.NewGuid(),
            routingPlan.CandidateImageId,
            ResolvePlanSeverity(steps),
            steps,
            createdAt);
    }

    private static RepairSeverity ResolvePlanSeverity(IReadOnlyList<RepairPlanStep> steps)
    {
        if (steps.Any(step => step.Severity is RepairSeverity.Regenerate))
        {
            return RepairSeverity.Regenerate;
        }

        if (steps.Any(step => step.Severity is RepairSeverity.Major))
        {
            return RepairSeverity.Major;
        }

        return steps.Any(step => step.Severity is RepairSeverity.Minor)
            ? RepairSeverity.Minor
            : RepairSeverity.None;
    }
}

public sealed record RepairPlanStep(
    int Order,
    ReviewOutcomeTargetLayer TargetLayer,
    RepairSeverity Severity,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> RecommendedActions,
    bool RequiresOperator)
{
    public static RepairPlanStep Create(
        int order,
        ReviewOutcomeTargetLayer targetLayer,
        RepairSeverity severity,
        IReadOnlyList<string> evidence,
        IReadOnlyList<string> recommendedActions,
        bool requiresOperator)
    {
        if (order <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(order), order, "Repair plan step order must be greater than zero.");
        }

        if (targetLayer is ReviewOutcomeTargetLayer.None)
        {
            throw new ArgumentException("Repair plan steps must target a repairable layer.", nameof(targetLayer));
        }

        if (severity is RepairSeverity.None)
        {
            throw new ArgumentException("Repair plan steps must have repair severity.", nameof(severity));
        }

        return new RepairPlanStep(
            order,
            targetLayer,
            severity,
            NormalizeRequiredTextList(evidence, nameof(evidence)),
            NormalizeRequiredTextList(recommendedActions, nameof(recommendedActions)),
            requiresOperator);
    }

    private static IReadOnlyList<string> NormalizeRequiredTextList(IReadOnlyList<string> values, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values);

        var normalized = values
            .Select(value => value?.Trim() ?? string.Empty)
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one value is required.", parameterName);
        }

        return normalized;
    }
}
