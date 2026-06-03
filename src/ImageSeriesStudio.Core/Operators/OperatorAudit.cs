namespace ImageSeriesStudio.Core.Operators;

public sealed record OperatorAction(
    Guid Id,
    Guid RepairPlanId,
    int RepairPlanStepOrder,
    string ToolAdapterId,
    string DisplayName,
    OperatorRiskLevel RiskLevel,
    bool DryRunSupported,
    bool RequiresApproval,
    IReadOnlyDictionary<string, string> Inputs,
    IReadOnlyList<string> ExpectedOutputs,
    IReadOnlyList<string> SideEffects,
    TimeSpan Timeout,
    string? CleanupPath,
    OperatorActionStatus Status,
    string? ApprovedBy,
    string? ApprovalNote,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset CreatedAt)
{
    public static OperatorAction CreateDraft(
        Guid repairPlanId,
        int repairPlanStepOrder,
        string toolAdapterId,
        string displayName,
        OperatorRiskLevel riskLevel,
        bool dryRunSupported,
        IReadOnlyDictionary<string, string> inputs,
        IReadOnlyList<string> expectedOutputs,
        IReadOnlyList<string> sideEffects,
        TimeSpan timeout,
        string? cleanupPath,
        DateTimeOffset createdAt)
    {
        if (repairPlanId == Guid.Empty)
        {
            throw new ArgumentException("Repair plan id cannot be empty.", nameof(repairPlanId));
        }

        if (repairPlanStepOrder <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(repairPlanStepOrder), repairPlanStepOrder, "Repair plan step order must be greater than zero.");
        }

        if (!Enum.IsDefined(typeof(OperatorRiskLevel), riskLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(riskLevel), riskLevel, "Operator risk level is not supported.");
        }

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Operator action timeout must be greater than zero.");
        }

        var requiresApproval = riskLevel is OperatorRiskLevel.Medium or OperatorRiskLevel.High;

        return new OperatorAction(
            Guid.NewGuid(),
            repairPlanId,
            repairPlanStepOrder,
            NormalizeId(toolAdapterId, nameof(toolAdapterId)),
            RequireText(displayName, nameof(displayName)),
            riskLevel,
            dryRunSupported,
            requiresApproval,
            NormalizeInputs(inputs),
            NormalizeRequiredTextList(expectedOutputs, nameof(expectedOutputs)),
            NormalizeRequiredTextList(sideEffects, nameof(sideEffects)),
            timeout,
            NormalizeOptionalPath(cleanupPath),
            requiresApproval ? OperatorActionStatus.PendingApproval : OperatorActionStatus.Ready,
            ApprovedBy: null,
            ApprovalNote: null,
            ApprovedAt: null,
            createdAt);
    }

    public OperatorAction Approve(
        string approvedBy,
        string approvalNote,
        DateTimeOffset approvedAt)
    {
        if (!RequiresApproval)
        {
            throw new InvalidOperationException("Operator action does not require approval.");
        }

        if (Status is not OperatorActionStatus.PendingApproval)
        {
            throw new InvalidOperationException($"Operator action cannot be approved from status: {Status}");
        }

        if (approvedAt < CreatedAt)
        {
            throw new ArgumentException("Operator action cannot be approved before it is created.", nameof(approvedAt));
        }

        return this with
        {
            Status = OperatorActionStatus.Approved,
            ApprovedBy = RequireText(approvedBy, nameof(approvedBy)),
            ApprovalNote = RequireText(approvalNote, nameof(approvalNote)),
            ApprovedAt = approvedAt,
        };
    }

    private static string? NormalizeOptionalPath(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Replace('\\', '/').Trim();
    }

    private static IReadOnlyDictionary<string, string> NormalizeInputs(IReadOnlyDictionary<string, string> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        return inputs
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(
                pair => pair.Key.Trim(),
                pair => pair.Value.Trim(),
                StringComparer.OrdinalIgnoreCase);
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

    private static string NormalizeId(string value, string parameterName)
    {
        var text = RequireText(value, parameterName).ToLowerInvariant();
        var normalized = new string(text.Select(character =>
            char.IsAsciiLetterLower(character) || char.IsDigit(character) || character is '-' or '_' or '.'
                ? character
                : '-').ToArray()).Trim('-');

        if (normalized.Length == 0)
        {
            throw new ArgumentException("Operator id cannot be empty.", parameterName);
        }

        return normalized;
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }
}

public sealed record OperatorRun(
    Guid Id,
    Guid OperatorActionId,
    string ToolAdapterId,
    bool DryRun,
    string InputSnapshot,
    OperatorRunStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string? OutputSummary,
    string? ErrorMessage)
{
    public static OperatorRun Start(
        Guid operatorActionId,
        string toolAdapterId,
        bool dryRun,
        string inputSnapshot,
        DateTimeOffset startedAt)
    {
        if (operatorActionId == Guid.Empty)
        {
            throw new ArgumentException("Operator action id cannot be empty.", nameof(operatorActionId));
        }

        return new OperatorRun(
            Guid.NewGuid(),
            operatorActionId,
            NormalizeId(toolAdapterId, nameof(toolAdapterId)),
            dryRun,
            RequireText(inputSnapshot, nameof(inputSnapshot)),
            OperatorRunStatus.Running,
            startedAt,
            CompletedAt: null,
            OutputSummary: null,
            ErrorMessage: null);
    }

    public OperatorRun CompleteSucceeded(
        string outputSummary,
        DateTimeOffset completedAt)
    {
        EnsureCompletionTime(completedAt);

        return this with
        {
            Status = OperatorRunStatus.Succeeded,
            CompletedAt = completedAt,
            OutputSummary = RequireText(outputSummary, nameof(outputSummary)),
            ErrorMessage = null,
        };
    }

    public OperatorRun CompleteFailed(
        string errorMessage,
        DateTimeOffset completedAt)
    {
        EnsureCompletionTime(completedAt);

        return this with
        {
            Status = OperatorRunStatus.Failed,
            CompletedAt = completedAt,
            OutputSummary = null,
            ErrorMessage = RequireText(errorMessage, nameof(errorMessage)),
        };
    }

    private void EnsureCompletionTime(DateTimeOffset completedAt)
    {
        if (completedAt < StartedAt)
        {
            throw new ArgumentException("Operator run cannot complete before it starts.", nameof(completedAt));
        }
    }

    private static string NormalizeId(string value, string parameterName)
    {
        var text = RequireText(value, parameterName).ToLowerInvariant();
        var normalized = new string(text.Select(character =>
            char.IsAsciiLetterLower(character) || char.IsDigit(character) || character is '-' or '_' or '.'
                ? character
                : '-').ToArray()).Trim('-');

        if (normalized.Length == 0)
        {
            throw new ArgumentException("Operator id cannot be empty.", parameterName);
        }

        return normalized;
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }
}

public enum OperatorRiskLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
}

public enum OperatorActionStatus
{
    Ready = 0,
    PendingApproval = 1,
    Approved = 2,
    Running = 3,
    Succeeded = 4,
    Failed = 5,
    Cancelled = 6,
}

public enum OperatorRunStatus
{
    Running = 0,
    Succeeded = 1,
    Failed = 2,
    Cancelled = 3,
}
