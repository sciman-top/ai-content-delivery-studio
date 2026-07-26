namespace ContentDeliveryStudio.Core.ScientificFigures;

public enum ScientificRepairLayer
{
    Extraction = 0,
    ScientificUnderstanding = 1,
    FigureSpecification = 2,
    SvgRenderer = 3,
    LayoutStyle = 4,
    NonEvidentiaryAsset = 5,
    Exporter = 6,
}

public enum ScientificRepairExecutionMode
{
    HumanRequired = 0,
    Automatic = 1,
}

public sealed record ScientificRepairAction(
    string FindingCode,
    string ResponsibleItemId,
    string Evidence,
    ScientificRepairLayer Layer,
    ScientificRepairExecutionMode ExecutionMode);

public sealed record ScientificRepairPlan
{
    private ScientificRepairPlan(IReadOnlyList<ScientificRepairAction> actions)
    {
        Actions = actions;
    }

    public IReadOnlyList<ScientificRepairAction> Actions { get; }

    public IReadOnlyList<ScientificRepairAction> AutomaticActions =>
        Actions.Where(item => item.ExecutionMode == ScientificRepairExecutionMode.Automatic)
            .ToArray();

    public bool RequiresHumanAction =>
        Actions.Any(item => item.ExecutionMode == ScientificRepairExecutionMode.HumanRequired);

    public static ScientificRepairPlan Create(
        IReadOnlyList<ScientificRepairAction> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        if (actions.Any(item => item is null))
        {
            throw new ArgumentException("Repair actions cannot contain null entries.", nameof(actions));
        }

        return new ScientificRepairPlan(Array.AsReadOnly(actions.ToArray()));
    }
}

public sealed record ScientificRepairLoopState
{
    private ScientificRepairLoopState(int completedAutomaticAttempts)
    {
        CompletedAutomaticAttempts = completedAutomaticAttempts;
    }

    public const int MaximumAutomaticAttempts = 3;

    public int CompletedAutomaticAttempts { get; }

    public static ScientificRepairLoopState Start() => new(0);

    public ScientificRepairLoopState RecordAutomaticAttempt(
        ScientificRepairPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (plan.AutomaticActions.Count == 0)
        {
            throw new InvalidOperationException(
                "A repair attempt requires at least one automatic action.");
        }

        if (plan.AutomaticActions.Any(item =>
                item.Layer is not ScientificRepairLayer.LayoutStyle
                    and not ScientificRepairLayer.NonEvidentiaryAsset))
        {
            throw new InvalidOperationException(
                "Only layout/style and non-evidentiary asset repairs may run automatically.");
        }

        if (CompletedAutomaticAttempts >= MaximumAutomaticAttempts)
        {
            throw new InvalidOperationException(
                "The fourth automatic scientific-figure repair attempt requires human action.");
        }

        return new ScientificRepairLoopState(CompletedAutomaticAttempts + 1);
    }
}
