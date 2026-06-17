using System.Text.Json;
using ContentDeliveryStudio.Core.Operators;

namespace ContentDeliveryStudio.Application.ToolAdapters;

public sealed class LowRiskAutoRepairService
{
    private readonly Dictionary<string, IToolAdapter> _adaptersById;

    public LowRiskAutoRepairService(IReadOnlyList<IToolAdapter> adapters)
    {
        ArgumentNullException.ThrowIfNull(adapters);

        _adaptersById = adapters.ToDictionary(adapter => adapter.Descriptor.Id, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<LowRiskAutoRepairResult> RunAsync(
        OperatorAction action,
        bool dryRun,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (action.RiskLevel is not OperatorRiskLevel.Low || action.RequiresApproval)
        {
            throw new InvalidOperationException("Only low-risk actions can run through the automatic repair path.");
        }

        if (action.Status is not OperatorActionStatus.Ready)
        {
            throw new InvalidOperationException($"Operator action is not ready for automatic repair: {action.Status}");
        }

        if (!_adaptersById.TryGetValue(action.ToolAdapterId, out var adapter))
        {
            throw new InvalidOperationException($"Tool adapter not found: {action.ToolAdapterId}");
        }

        var request = ToolAdapterRunRequest.Create(
            adapter.Descriptor,
            dryRun,
            action.Inputs,
            startedAt);
        var run = OperatorRun.Start(
            action.Id,
            adapter.Descriptor.Id,
            dryRun,
            JsonSerializer.Serialize(action.Inputs),
            startedAt);
        var adapterResult = await adapter.RunAsync(request, cancellationToken);

        return new LowRiskAutoRepairResult(
            run.CompleteSucceeded(adapterResult.Summary, startedAt),
            adapterResult);
    }
}

public sealed record LowRiskAutoRepairResult(
    OperatorRun Run,
    ToolAdapterRunResult AdapterResult);
