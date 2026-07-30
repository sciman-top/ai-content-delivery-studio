using System.Text;
using System.Text.Json;
using ContentDeliveryStudio.Application.Diagnostics;
using ContentDeliveryStudio.Application.Projects;

namespace ContentDeliveryStudio.Infrastructure.Diagnostics;

public sealed class JsonlDiagnosticsEventJournal : IDiagnosticsEventJournal
{
    internal const int DefaultMaxFileBytes = 1024 * 1024;
    internal const int DefaultRetainedFileCount = 3;
    internal const int DefaultMaxLineBytes = 16 * 1024;
    private const int SchemaVersion = 1;
    private const string RedactedValue = "[redacted]";
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };
    private static readonly HashSet<string> TopLevelPropertyNames =
    [
        "schemaVersion",
        "timestamp",
        "level",
        "category",
        "eventName",
        "correlationId",
        "properties",
    ];
    private static readonly HashSet<string> SafePropertyNames =
    [
        "projectId",
        "taskId",
        "status",
        "queuePosition",
        "itemCount",
        "retryOfTaskId",
        "direction",
        "providerId",
        "operation",
        "model",
        "httpStatusCode",
        "succeeded",
        "latencyMilliseconds",
        "totalTokens",
        "estimatedCostUsd",
    ];
    private static readonly HashSet<string> QueueEventNames =
    [
        "prepared",
        "paused",
        "resumed",
        "moved",
        "retried",
        "execution-started",
        "execution-succeeded",
        "execution-failed",
        "execution-cancelled",
    ];
    private static readonly string[] SensitiveMarkers =
    [
        "authorization",
        "bearer ",
        "api-key",
        "api_key",
        "apikey",
        ".env",
        "sk-",
    ];

    private readonly object _gate = new();
    private readonly string _directoryPath;
    private readonly int _maxFileBytes;
    private readonly int _retainedFileCount;
    private readonly int _maxLineBytes;
    private int _droppedCount;

    public JsonlDiagnosticsEventJournal()
        : this(
            Path.Combine(LocalStudioDataPaths.ResolveStudioRoot(), "diagnostics", "events"),
            DefaultMaxFileBytes,
            DefaultRetainedFileCount,
            DefaultMaxLineBytes)
    {
    }

    internal JsonlDiagnosticsEventJournal(
        string directoryPath,
        int maxFileBytes = DefaultMaxFileBytes,
        int retainedFileCount = DefaultRetainedFileCount,
        int maxLineBytes = DefaultMaxLineBytes)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("Journal directory cannot be empty.", nameof(directoryPath));
        }

        if (maxFileBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxFileBytes));
        }

        if (retainedFileCount is < 1 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(retainedFileCount));
        }

        if (maxLineBytes <= 0 || maxLineBytes > maxFileBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLineBytes));
        }

        _directoryPath = Path.GetFullPath(directoryPath);
        _maxFileBytes = maxFileBytes;
        _retainedFileCount = retainedFileCount;
        _maxLineBytes = maxLineBytes;
    }

    internal string ActiveFilePath => GetFilePath(0);

    public void Record(GenerationQueueDiagnosticsEvent value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var eventName = ToEventName(value.EventName);
        var entry = new DiagnosticsLogEntry(
            SchemaVersion,
            value.Timestamp,
            value.EventName is GenerationQueueDiagnosticsEventName.ExecutionFailed
                ? "error"
                : value.EventName is GenerationQueueDiagnosticsEventName.ExecutionCancelled
                    ? "warning"
                    : "information",
            "generation-queue",
            eventName,
            value.ProjectId.ToString("N"),
            new DiagnosticsLogProperties(
                ProjectId: value.ProjectId.ToString("N"),
                TaskId: value.TaskId?.ToString("N"),
                Status: SanitizeString(value.Status),
                QueuePosition: value.QueuePosition,
                ItemCount: value.ItemCount,
                RetryOfTaskId: value.RetryOfTaskId?.ToString("N"),
                Direction: SanitizeString(value.Direction)));

        TryAppend(entry);
    }

    public void Record(ProviderCallDiagnosticsEvent value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var correlationId = string.IsNullOrWhiteSpace(value.CorrelationId)
            ? Guid.NewGuid().ToString("N")
            : SanitizeString(value.CorrelationId);
        var entry = new DiagnosticsLogEntry(
            SchemaVersion,
            value.Timestamp,
            value.Succeeded ? "information" : "warning",
            "provider-call",
            "completed",
            correlationId ?? RedactedValue,
            new DiagnosticsLogProperties(
                ProviderId: SanitizeString(value.ProviderId),
                Operation: SanitizeString(value.Operation),
                Model: SanitizeString(value.Model),
                HttpStatusCode: value.HttpStatusCode,
                Succeeded: value.Succeeded,
                LatencyMilliseconds: value.LatencyMilliseconds,
                TotalTokens: value.TotalTokens,
                EstimatedCostUsd: value.EstimatedCostUsd));

        TryAppend(entry);
    }

    public Task<DiagnosticsLogReadResult> ReadRecentAsync(int maxCount, CancellationToken cancellationToken)
    {
        if (maxCount is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCount));
        }

        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var entries = new Queue<DiagnosticsLogEntry>(maxCount);
            var invalidCount = 0;
            foreach (var path in GetRetainedPathsOldestFirst())
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                string[] lines;
                try
                {
                    if (new FileInfo(path).Length > _maxFileBytes)
                    {
                        invalidCount++;
                        continue;
                    }

                    lines = File.ReadAllLines(path, Utf8WithoutBom);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    invalidCount++;
                    continue;
                }

                foreach (var line in lines)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (Utf8WithoutBom.GetByteCount(line) > _maxLineBytes
                        || !TryParseAndValidate(line, out var entry))
                    {
                        invalidCount++;
                        continue;
                    }

                    if (entries.Count == maxCount)
                    {
                        entries.Dequeue();
                    }

                    entries.Enqueue(entry!);
                }
            }

            return Task.FromResult(new DiagnosticsLogReadResult(entries.ToArray(), _droppedCount, invalidCount));
        }
    }

    private void TryAppend(DiagnosticsLogEntry entry)
    {
        lock (_gate)
        {
            try
            {
                if (!Validate(entry))
                {
                    _droppedCount++;
                    return;
                }

                var lineBytes = Utf8WithoutBom.GetBytes(JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine);
                if (lineBytes.Length > _maxLineBytes)
                {
                    _droppedCount++;
                    return;
                }

                Directory.CreateDirectory(_directoryPath);
                RotateIfRequired(lineBytes.Length);
                using var stream = new FileStream(
                    ActiveFilePath,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.Read,
                    bufferSize: 4096,
                    FileOptions.WriteThrough);
                stream.Write(lineBytes);
                stream.Flush(flushToDisk: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _droppedCount++;
            }
        }
    }

    private void RotateIfRequired(int incomingByteCount)
    {
        if (!File.Exists(ActiveFilePath)
            || new FileInfo(ActiveFilePath).Length + incomingByteCount <= _maxFileBytes)
        {
            return;
        }

        var oldestPath = GetFilePath(_retainedFileCount - 1);
        if (File.Exists(oldestPath))
        {
            File.Delete(oldestPath);
        }

        for (var index = _retainedFileCount - 2; index >= 0; index--)
        {
            var source = GetFilePath(index);
            if (File.Exists(source))
            {
                File.Move(source, GetFilePath(index + 1));
            }
        }
    }

    private IEnumerable<string> GetRetainedPathsOldestFirst()
    {
        for (var index = _retainedFileCount - 1; index >= 0; index--)
        {
            yield return GetFilePath(index);
        }
    }

    private string GetFilePath(int index)
    {
        return index == 0
            ? Path.Combine(_directoryPath, "events.jsonl")
            : Path.Combine(_directoryPath, $"events.{index}.jsonl");
    }

    private static bool TryParseAndValidate(string line, out DiagnosticsLogEntry? entry)
    {
        entry = null;
        try
        {
            using var document = JsonDocument.Parse(line);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || HasUnknownOrDuplicateProperties(document.RootElement, TopLevelPropertyNames)
                || !document.RootElement.TryGetProperty("properties", out var properties)
                || properties.ValueKind != JsonValueKind.Object
                || HasUnknownOrDuplicateProperties(properties, SafePropertyNames))
            {
                return false;
            }

            entry = JsonSerializer.Deserialize<DiagnosticsLogEntry>(line, JsonOptions);
            return entry is not null && Validate(entry);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool Validate(DiagnosticsLogEntry entry)
    {
        if (entry.SchemaVersion != SchemaVersion
            || entry.Timestamp == default
            || entry.Properties is null
            || entry.Level is not ("information" or "warning" or "error")
            || !IsSafeString(entry.CorrelationId))
        {
            return false;
        }

        return entry.Category switch
        {
            "generation-queue" => ValidateQueueEntry(entry),
            "provider-call" => ValidateProviderEntry(entry),
            _ => false,
        };
    }

    private static bool ValidateQueueEntry(DiagnosticsLogEntry entry)
    {
        var properties = entry.Properties;
        return QueueEventNames.Contains(entry.EventName)
            && TryParseGuid(properties.ProjectId)
            && (properties.TaskId is null || TryParseGuid(properties.TaskId))
            && (properties.RetryOfTaskId is null || TryParseGuid(properties.RetryOfTaskId))
            && IsSafeOptionalString(properties.Status)
            && IsSafeOptionalString(properties.Direction)
            && properties.QueuePosition is null or > 0
            && properties.ItemCount is null or >= 0
            && properties.ProviderId is null
            && properties.Operation is null
            && properties.Model is null
            && properties.HttpStatusCode is null
            && properties.Succeeded is null
            && properties.LatencyMilliseconds is null
            && properties.TotalTokens is null
            && properties.EstimatedCostUsd is null;
    }

    private static bool ValidateProviderEntry(DiagnosticsLogEntry entry)
    {
        var properties = entry.Properties;
        return entry.EventName == "completed"
            && IsSafeString(properties.ProviderId)
            && IsSafeString(properties.Operation)
            && IsSafeString(properties.Model)
            && properties.HttpStatusCode is >= 0 and <= 599
            && properties.Succeeded is not null
            && properties.LatencyMilliseconds is >= 0 and <= 86_400_000
            && properties.TotalTokens is null or >= 0
            && properties.EstimatedCostUsd is >= 0 and <= 1_000_000
            && properties.ProjectId is null
            && properties.TaskId is null
            && properties.Status is null
            && properties.QueuePosition is null
            && properties.ItemCount is null
            && properties.RetryOfTaskId is null
            && properties.Direction is null;
    }

    private static string ToEventName(GenerationQueueDiagnosticsEventName value)
    {
        return value switch
        {
            GenerationQueueDiagnosticsEventName.Prepared => "prepared",
            GenerationQueueDiagnosticsEventName.Paused => "paused",
            GenerationQueueDiagnosticsEventName.Resumed => "resumed",
            GenerationQueueDiagnosticsEventName.Moved => "moved",
            GenerationQueueDiagnosticsEventName.Retried => "retried",
            GenerationQueueDiagnosticsEventName.ExecutionStarted => "execution-started",
            GenerationQueueDiagnosticsEventName.ExecutionSucceeded => "execution-succeeded",
            GenerationQueueDiagnosticsEventName.ExecutionFailed => "execution-failed",
            GenerationQueueDiagnosticsEventName.ExecutionCancelled => "execution-cancelled",
            _ => string.Empty,
        };
    }

    private static string? SanitizeString(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        return IsSafeString(trimmed) ? trimmed : RedactedValue;
    }

    private static bool IsSafeOptionalString(string? value)
    {
        return value is null || IsSafeString(value);
    }

    private static bool IsSafeString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > 128
            || value.IndexOfAny(['\r', '\n', '\0']) >= 0
            || Path.IsPathRooted(value)
            || value.StartsWith("\\\\", StringComparison.Ordinal))
        {
            return false;
        }

        return !SensitiveMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryParseGuid(string? value)
    {
        return value is not null && Guid.TryParseExact(value, "N", out _);
    }

    private static bool HasUnknownOrDuplicateProperties(JsonElement value, HashSet<string> allowedNames)
    {
        var encounteredNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
        {
            if (!allowedNames.Contains(property.Name) || !encounteredNames.Add(property.Name))
            {
                return true;
            }
        }

        return false;
    }
}
