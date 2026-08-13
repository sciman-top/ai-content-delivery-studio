using System.Text;
using ContentDeliveryStudio.Application.Diagnostics;
using ContentDeliveryStudio.Infrastructure.Diagnostics;

namespace ContentDeliveryStudio.Tests;

public sealed class JsonlDiagnosticsEventJournalTests
{
    [Fact]
    public async Task RecordAndReadRecent_RoundTripsTypedEventsAndRedactsUnsafeStrings()
    {
        using var directory = new TemporaryDirectory();
        var journal = new JsonlDiagnosticsEventJournal(directory.Path);
        var projectId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var timestamp = DateTimeOffset.Parse("2026-07-29T10:00:00Z");

        journal.Record(new GenerationQueueDiagnosticsEvent(
            timestamp,
            GenerationQueueDiagnosticsEventName.ExecutionStarted,
            projectId,
            taskId,
            Status: "Running",
            QueuePosition: 2));
        journal.Record(new ProviderCallDiagnosticsEvent(
            timestamp.AddSeconds(1),
            "openai-image",
            "image-generation",
            "sk-not-a-safe-model",
            HttpStatusCode: 200,
            Succeeded: true,
            LatencyMilliseconds: 125.5,
            TotalTokens: 20,
            EstimatedCostUsd: 0.03m,
            CorrelationId: "trace123",
            ModelPreset: "terra-high",
            ReasoningEffort: "high",
            RouteReason: "routine-series-plan"));

        var result = await journal.ReadRecentAsync(500, CancellationToken.None);

        Assert.Equal(2, result.Entries.Count);
        Assert.Equal("execution-started", result.Entries[0].EventName);
        Assert.Equal(projectId.ToString("N"), result.Entries[0].CorrelationId);
        Assert.Equal(taskId.ToString("N"), result.Entries[0].Properties.TaskId);
        Assert.Equal("completed", result.Entries[1].EventName);
        Assert.Equal("[redacted]", result.Entries[1].Properties.Model);
        Assert.Equal("terra-high", result.Entries[1].Properties.ModelPreset);
        Assert.Equal("high", result.Entries[1].Properties.ReasoningEffort);
        Assert.Equal("routine-series-plan", result.Entries[1].Properties.RouteReason);
        Assert.Equal(0, result.DroppedCount);
        Assert.Equal(0, result.InvalidCount);
        Assert.DoesNotContain("sk-not-a-safe-model", await File.ReadAllTextAsync(journal.ActiveFilePath));
    }

    [Fact]
    public async Task Record_RotatesAndRetainsAtMostConfiguredFiles()
    {
        using var directory = new TemporaryDirectory();
        var journal = new JsonlDiagnosticsEventJournal(
            directory.Path,
            maxFileBytes: 700,
            retainedFileCount: 3,
            maxLineBytes: 600);

        for (var index = 0; index < 20; index++)
        {
            journal.Record(CreateQueueEvent(index));
        }

        var files = Directory.GetFiles(directory.Path, "events*.jsonl");
        var result = await journal.ReadRecentAsync(500, CancellationToken.None);

        Assert.Equal(3, files.Length);
        Assert.NotEmpty(result.Entries);
        Assert.Equal("19", result.Entries[^1].Properties.Status);
        Assert.All(files, path => Assert.True(new FileInfo(path).Length <= 700));
    }

    [Fact]
    public async Task ReadRecent_SkipsMalformedOversizedAndUnknownRecords()
    {
        using var directory = new TemporaryDirectory();
        var journal = new JsonlDiagnosticsEventJournal(
            directory.Path,
            maxFileBytes: 4096,
            retainedFileCount: 3,
            maxLineBytes: 512);
        Directory.CreateDirectory(directory.Path);
        await File.WriteAllLinesAsync(
            journal.ActiveFilePath,
            [
                "not-json",
                "{\"schemaVersion\":99,\"timestamp\":\"2026-07-29T10:00:00Z\",\"level\":\"information\",\"category\":\"generation-queue\",\"eventName\":\"prepared\",\"correlationId\":\"abc\",\"properties\":{\"projectId\":\"abc\"}}",
                "{\"schemaVersion\":1,\"timestamp\":\"2026-07-29T10:00:00Z\",\"level\":\"information\",\"category\":\"generation-queue\",\"eventName\":\"prepared\",\"correlationId\":\"abc\",\"properties\":{\"projectId\":\"abc\",\"prompt\":\"forbidden\"}}",
                "{\"schemaVersion\":1,\"schemaVersion\":1,\"timestamp\":\"2026-07-29T10:00:00Z\",\"level\":\"information\",\"category\":\"generation-queue\",\"eventName\":\"prepared\",\"correlationId\":\"abc\",\"properties\":{\"projectId\":\"abc\"}}",
                new string('x', 513),
            ],
            Encoding.UTF8);
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "events.1.jsonl"),
            new string('x', 4097),
            Encoding.UTF8);
        journal.Record(CreateQueueEvent(1));

        var result = await journal.ReadRecentAsync(500, CancellationToken.None);

        Assert.Single(result.Entries);
        Assert.Equal(6, result.InvalidCount);
    }

    [Fact]
    public async Task ConcurrentRecordAndBoundedRead_KeepValidNewestFiveHundredEntries()
    {
        using var directory = new TemporaryDirectory();
        var journal = new JsonlDiagnosticsEventJournal(directory.Path);

        await Task.WhenAll(Enumerable.Range(0, 600).Select(index => Task.Run(() =>
            journal.Record(CreateQueueEvent(index)))));

        var result = await journal.ReadRecentAsync(500, CancellationToken.None);

        Assert.Equal(500, result.Entries.Count);
        Assert.Equal(0, result.DroppedCount);
        Assert.Equal(0, result.InvalidCount);
        Assert.All(result.Entries, entry => Assert.Equal(1, entry.SchemaVersion));
        Assert.Equal(600, File.ReadLines(journal.ActiveFilePath).Count());
    }

    private static GenerationQueueDiagnosticsEvent CreateQueueEvent(int index)
    {
        return new GenerationQueueDiagnosticsEvent(
            DateTimeOffset.Parse("2026-07-29T10:00:00Z").AddTicks(index),
            GenerationQueueDiagnosticsEventName.Prepared,
            Guid.Parse("9ed554a6-d05a-4ea1-9585-f6a5eddd2620"),
            Status: index.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ItemCount: 1);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "ContentDeliveryStudio.Tests",
                Guid.NewGuid().ToString("N"));
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
