namespace ContentDeliveryStudio.Application.Diagnostics;

public interface IDiagnosticsEventJournal
{
    void Record(GenerationQueueDiagnosticsEvent value);

    void Record(ProviderCallDiagnosticsEvent value);

    Task<DiagnosticsLogReadResult> ReadRecentAsync(int maxCount, CancellationToken cancellationToken);
}

public sealed class NullDiagnosticsEventJournal : IDiagnosticsEventJournal
{
    public static NullDiagnosticsEventJournal Instance { get; } = new();

    private NullDiagnosticsEventJournal()
    {
    }

    public void Record(GenerationQueueDiagnosticsEvent value)
    {
    }

    public void Record(ProviderCallDiagnosticsEvent value)
    {
    }

    public Task<DiagnosticsLogReadResult> ReadRecentAsync(int maxCount, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new DiagnosticsLogReadResult([], DroppedCount: 0, InvalidCount: 0));
    }
}

public enum GenerationQueueDiagnosticsEventName
{
    Prepared = 0,
    Paused = 1,
    Resumed = 2,
    Moved = 3,
    Retried = 4,
    ExecutionStarted = 5,
    ExecutionSucceeded = 6,
    ExecutionFailed = 7,
    ExecutionCancelled = 8,
}

public sealed record GenerationQueueDiagnosticsEvent(
    DateTimeOffset Timestamp,
    GenerationQueueDiagnosticsEventName EventName,
    Guid ProjectId,
    Guid? TaskId = null,
    string? Status = null,
    int? QueuePosition = null,
    int? ItemCount = null,
    Guid? RetryOfTaskId = null,
    string? Direction = null);

public sealed record ProviderCallDiagnosticsEvent(
    DateTimeOffset Timestamp,
    string ProviderId,
    string Operation,
    string Model,
    int HttpStatusCode,
    bool Succeeded,
    double LatencyMilliseconds,
    int? TotalTokens,
    decimal EstimatedCostUsd,
    string? CorrelationId = null,
    string? ModelPreset = null,
    string? ReasoningEffort = null,
    string? RouteReason = null);

public sealed record DiagnosticsLogReadResult(
    IReadOnlyList<DiagnosticsLogEntry> Entries,
    int DroppedCount,
    int InvalidCount);

public sealed record DiagnosticsLogEntry(
    int SchemaVersion,
    DateTimeOffset Timestamp,
    string Level,
    string Category,
    string EventName,
    string CorrelationId,
    DiagnosticsLogProperties Properties);

public sealed record DiagnosticsLogProperties(
    string? ProjectId = null,
    string? TaskId = null,
    string? Status = null,
    int? QueuePosition = null,
    int? ItemCount = null,
    string? RetryOfTaskId = null,
    string? Direction = null,
    string? ProviderId = null,
    string? Operation = null,
    string? Model = null,
    int? HttpStatusCode = null,
    bool? Succeeded = null,
    double? LatencyMilliseconds = null,
    int? TotalTokens = null,
    decimal? EstimatedCostUsd = null,
    string? ModelPreset = null,
    string? ReasoningEffort = null,
    string? RouteReason = null);
