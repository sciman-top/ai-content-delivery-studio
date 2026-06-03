namespace ImageSeriesStudio.Core.Projects;

public sealed record RoutedRepairPatch(
    Guid Id,
    Guid ProjectId,
    Guid RepairPlanId,
    Guid CandidateImageId,
    IReadOnlyList<RoutedRepairPatchItem> Items,
    DateTimeOffset CreatedAt)
{
    public static RoutedRepairPatch FromRepairPlan(Guid projectId, RepairPlan repairPlan, DateTimeOffset createdAt)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        return FromRepairPlan(projectId, repairPlan, createdAt, allowEmptyProjectId: false);
    }

    public static RoutedRepairPatch FromRepairPlan(RepairPlan repairPlan, DateTimeOffset createdAt)
    {
        return FromRepairPlan(Guid.Empty, repairPlan, createdAt, allowEmptyProjectId: true);
    }

    private static RoutedRepairPatch FromRepairPlan(
        Guid projectId,
        RepairPlan repairPlan,
        DateTimeOffset createdAt,
        bool allowEmptyProjectId)
    {
        ArgumentNullException.ThrowIfNull(repairPlan);

        if (!allowEmptyProjectId && projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        var items = repairPlan.Steps
            .Where(step => step.TargetLayer is ReviewOutcomeTargetLayer.Brief or ReviewOutcomeTargetLayer.Blueprint)
            .OrderBy(step => step.Order)
            .Select(step => RoutedRepairPatchItem.Create(
                step.Order,
                step.TargetLayer,
                step.Severity,
                step.Evidence,
                step.RecommendedActions,
                requiresHumanApproval: true))
            .ToArray();
        if (items.Length == 0)
        {
            throw new InvalidOperationException("Routed repair patch requires Brief or Blueprint repair routes.");
        }

        return new RoutedRepairPatch(
            Guid.NewGuid(),
            projectId,
            repairPlan.Id,
            repairPlan.CandidateImageId,
            items,
            createdAt);
    }
}

public sealed record RoutedRepairPatchItem(
    int Order,
    ReviewOutcomeTargetLayer TargetLayer,
    RepairSeverity Severity,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> ProposedChanges,
    bool RequiresHumanApproval)
{
    public static RoutedRepairPatchItem Create(
        int order,
        ReviewOutcomeTargetLayer targetLayer,
        RepairSeverity severity,
        IReadOnlyList<string> evidence,
        IReadOnlyList<string> proposedChanges,
        bool requiresHumanApproval)
    {
        if (order <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(order), order, "Repair patch item order must be greater than zero.");
        }

        if (targetLayer is not (ReviewOutcomeTargetLayer.Brief or ReviewOutcomeTargetLayer.Blueprint))
        {
            throw new ArgumentException("Repair patch items must target Brief or Blueprint layers.", nameof(targetLayer));
        }

        if (severity is RepairSeverity.None)
        {
            throw new ArgumentException("Repair patch items must have repair severity.", nameof(severity));
        }

        return new RoutedRepairPatchItem(
            order,
            targetLayer,
            severity,
            NormalizeRequiredTextList(evidence, nameof(evidence)),
            NormalizeRequiredTextList(proposedChanges, nameof(proposedChanges)),
            requiresHumanApproval);
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
