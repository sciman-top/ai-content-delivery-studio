using ContentDeliveryStudio.Core.Operators;

namespace ContentDeliveryStudio.Application.RemoteWorkflows;

public interface IRemoteWorkflowEngineAdapter
{
    RemoteWorkflowEngineDescriptor Descriptor { get; }

    Task<RemoteWorkflowRunResult> RunAsync(
        RemoteWorkflowRunRequest request,
        CancellationToken cancellationToken);
}

public sealed record RemoteWorkflowEngineDescriptor(
    string Id,
    string DisplayName,
    RemoteWorkflowEngineKind Kind,
    OperatorRiskLevel RiskLevel,
    bool DryRunSupported,
    bool RequiresApproval,
    bool StoresRemoteStateByDefault,
    IReadOnlyList<string> InputNames,
    IReadOnlyList<string> OutputNames,
    IReadOnlyList<string> SideEffects,
    TimeSpan DefaultTimeout)
{
    public static RemoteWorkflowEngineDescriptor Create(
        string id,
        string displayName,
        RemoteWorkflowEngineKind kind,
        OperatorRiskLevel riskLevel,
        bool dryRunSupported,
        bool storesRemoteStateByDefault,
        IReadOnlyList<string> inputNames,
        IReadOnlyList<string> outputNames,
        IReadOnlyList<string> sideEffects,
        TimeSpan defaultTimeout)
    {
        if (!Enum.IsDefined(typeof(RemoteWorkflowEngineKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Remote workflow engine kind is not supported.");
        }

        if (!Enum.IsDefined(typeof(OperatorRiskLevel), riskLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(riskLevel), riskLevel, "Operator risk level is not supported.");
        }

        if (defaultTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(defaultTimeout), defaultTimeout, "Remote workflow timeout must be greater than zero.");
        }

        return new RemoteWorkflowEngineDescriptor(
            NormalizeId(id, nameof(id)),
            RequireText(displayName, nameof(displayName)),
            kind,
            riskLevel,
            dryRunSupported,
            riskLevel is OperatorRiskLevel.Medium or OperatorRiskLevel.High,
            storesRemoteStateByDefault,
            NormalizeRequiredTextList(inputNames, nameof(inputNames)),
            NormalizeRequiredTextList(outputNames, nameof(outputNames)),
            NormalizeRequiredTextList(sideEffects, nameof(sideEffects)),
            defaultTimeout);
    }

    internal static string NormalizeId(string value, string parameterName)
    {
        var text = RequireText(value, parameterName).ToLowerInvariant();
        var normalized = new string(text.Select(character =>
            char.IsAsciiLetterLower(character) || char.IsDigit(character) || character is '-' or '_' or '.'
                ? character
                : '-').ToArray()).Trim('-');

        if (normalized.Length == 0)
        {
            throw new ArgumentException("Remote workflow id cannot be empty.", parameterName);
        }

        return normalized;
    }

    internal static IReadOnlyList<string> NormalizeRequiredTextList(IReadOnlyList<string> values, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values);

        var normalized = values
            .Select(value => value?.Trim() ?? string.Empty)
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one value is required.", parameterName);
        }

        return normalized;
    }

    internal static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }
}

public sealed record RemoteWorkflowRunRequest(
    RemoteWorkflowEngineDescriptor Descriptor,
    string WorkflowKey,
    bool DryRun,
    Guid CorrelationId,
    IReadOnlyDictionary<string, string> Inputs,
    DateTimeOffset RequestedAt)
{
    public static RemoteWorkflowRunRequest Create(
        RemoteWorkflowEngineDescriptor descriptor,
        string workflowKey,
        bool dryRun,
        Guid correlationId,
        IReadOnlyDictionary<string, string> inputs,
        DateTimeOffset requestedAt)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (dryRun && !descriptor.DryRunSupported)
        {
            throw new InvalidOperationException($"Remote workflow engine does not support dry-run: {descriptor.Id}");
        }

        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException("Correlation id cannot be empty.", nameof(correlationId));
        }

        var normalizedInputs = NormalizeInputs(inputs);
        var missingInput = descriptor.InputNames.FirstOrDefault(inputName => !normalizedInputs.ContainsKey(inputName));
        if (missingInput is not null)
        {
            throw new ArgumentException($"Remote workflow input is missing: {missingInput}", nameof(inputs));
        }

        return new RemoteWorkflowRunRequest(
            descriptor,
            RemoteWorkflowEngineDescriptor.NormalizeId(workflowKey, nameof(workflowKey)),
            dryRun,
            correlationId,
            normalizedInputs,
            requestedAt);
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
}

public sealed record RemoteWorkflowRunResult(
    string AdapterId,
    string WorkflowKey,
    bool DryRun,
    bool RemoteCallsMade,
    string RemoteRunId,
    IReadOnlyDictionary<string, string> Outputs,
    IReadOnlyList<string> Warnings,
    string Summary,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt)
{
    public static RemoteWorkflowRunResult Create(
        string adapterId,
        string workflowKey,
        bool dryRun,
        bool remoteCallsMade,
        string remoteRunId,
        IReadOnlyDictionary<string, string> outputs,
        IReadOnlyList<string> warnings,
        string summary,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt)
    {
        ArgumentNullException.ThrowIfNull(outputs);
        ArgumentNullException.ThrowIfNull(warnings);

        if (completedAt < startedAt)
        {
            throw new ArgumentException("Remote workflow completion cannot be earlier than start time.", nameof(completedAt));
        }

        if (dryRun && remoteCallsMade)
        {
            throw new InvalidOperationException("Dry-run remote workflow results cannot report remote calls.");
        }

        return new RemoteWorkflowRunResult(
            RemoteWorkflowEngineDescriptor.NormalizeId(adapterId, nameof(adapterId)),
            RemoteWorkflowEngineDescriptor.NormalizeId(workflowKey, nameof(workflowKey)),
            dryRun,
            remoteCallsMade,
            RemoteWorkflowEngineDescriptor.RequireText(remoteRunId, nameof(remoteRunId)),
            outputs
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                .ToDictionary(
                    pair => pair.Key.Trim(),
                    pair => pair.Value.Trim(),
                    StringComparer.OrdinalIgnoreCase),
            warnings
                .Select(warning => warning?.Trim() ?? string.Empty)
                .Where(warning => warning.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            RemoteWorkflowEngineDescriptor.RequireText(summary, nameof(summary)),
            startedAt,
            completedAt);
    }
}

public enum RemoteWorkflowEngineKind
{
    HostedWorkflowEngine = 0,
    ManagedAutomation = 1,
    SelfHostedGateway = 2,
    FakeNoNetwork = 3,
}
