using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.Tests;

public sealed class GenerationTaskTests
{
    [Fact]
    public void GenerationTask_PausesAndResumesQueuedWorkWithoutStartingAttempt()
    {
        var timestamp = DateTimeOffset.Parse("2026-07-29T13:00:00Z");
        var task = CreateTask(GenerationTaskStatus.Queued, timestamp, queuePosition: 2);

        task.Pause(timestamp.AddSeconds(1));

        Assert.Equal(GenerationTaskStatus.Paused, task.Status);
        Assert.Equal(0, task.AttemptCount);

        task.Resume(timestamp.AddSeconds(2));

        Assert.Equal(GenerationTaskStatus.Queued, task.Status);
        Assert.Equal(0, task.AttemptCount);
    }

    [Fact]
    public void GenerationTask_ReordersOnlyActiveWork()
    {
        var timestamp = DateTimeOffset.Parse("2026-07-29T13:10:00Z");
        var task = CreateTask(GenerationTaskStatus.Paused, timestamp, queuePosition: 3);

        task.MoveTo(1, timestamp.AddSeconds(1));

        Assert.Equal(1, task.QueuePosition);

        task.Resume(timestamp.AddSeconds(2));
        task.Start(timestamp.AddSeconds(3));
        task.Succeed(timestamp.AddSeconds(4));

        Assert.Throws<InvalidOperationException>(() => task.MoveTo(4, timestamp.AddSeconds(5)));
    }

    [Fact]
    public void GenerationTask_PreservesRetryProvenanceAndRejectsInvalidPosition()
    {
        var timestamp = DateTimeOffset.Parse("2026-07-29T13:20:00Z");
        var originalTaskId = Guid.NewGuid();
        var task = new GenerationTask(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            GenerationTaskStatus.Queued,
            attemptCount: 0,
            maxRetries: 0,
            timestamp,
            timestamp,
            queuePosition: 4,
            retryOfTaskId: originalTaskId);

        Assert.Equal(4, task.QueuePosition);
        Assert.Equal(originalTaskId, task.RetryOfTaskId);
        Assert.Throws<ArgumentOutOfRangeException>(() => task.MoveTo(0, timestamp.AddSeconds(1)));
    }

    [Fact]
    public void GenerationTask_RecoveryPreservesPreparedQueuedAndPausedWork()
    {
        var timestamp = DateTimeOffset.Parse("2026-07-29T13:30:00Z");
        var queued = CreateTask(GenerationTaskStatus.Queued, timestamp, queuePosition: 1);
        var paused = CreateTask(GenerationTaskStatus.Paused, timestamp, queuePosition: 2);

        Assert.False(queued.RecoverInterrupted(timestamp.AddMinutes(1)));
        Assert.False(paused.RecoverInterrupted(timestamp.AddMinutes(1)));
        Assert.Equal(GenerationTaskStatus.Queued, queued.Status);
        Assert.Equal(GenerationTaskStatus.Paused, paused.Status);
    }

    [Fact]
    public void GenerationTask_TransitionsFromQueuedThroughSucceeded()
    {
        var timestamp = DateTimeOffset.Parse("2026-07-28T15:00:00Z");
        var task = CreateTask(GenerationTaskStatus.Queued, timestamp);

        task.Start(timestamp.AddSeconds(1));
        task.Succeed(timestamp.AddSeconds(2));

        Assert.Equal(GenerationTaskStatus.Succeeded, task.Status);
        Assert.Equal(1, task.AttemptCount);
        Assert.Null(task.ErrorMessage);
        Assert.Equal(timestamp.AddSeconds(2), task.UpdatedAt);
    }

    [Fact]
    public void GenerationTask_RecordsFailureAndRejectsTerminalMutation()
    {
        var timestamp = DateTimeOffset.Parse("2026-07-28T15:10:00Z");
        var task = CreateTask(GenerationTaskStatus.Queued, timestamp);

        task.Start(timestamp.AddSeconds(1));
        task.Fail("  provider unavailable  ", timestamp.AddSeconds(2));

        Assert.Equal(GenerationTaskStatus.Failed, task.Status);
        Assert.Equal("provider unavailable", task.ErrorMessage);
        Assert.Throws<InvalidOperationException>(() => task.Start(timestamp.AddSeconds(3)));
        Assert.Throws<InvalidOperationException>(() => task.Cancel("late cancellation", timestamp.AddSeconds(3)));
    }

    [Fact]
    public void GenerationTask_RecoversRunningStateWithoutRequeue()
    {
        var timestamp = DateTimeOffset.Parse("2026-07-28T15:20:00Z");
        var task = CreateTask(GenerationTaskStatus.Running, timestamp);

        var changed = task.RecoverInterrupted(timestamp.AddMinutes(1));

        Assert.True(changed);
        Assert.Equal(GenerationTaskStatus.Failed, task.Status);
        Assert.Contains("interrupted", task.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerationTask_DoesNotRecoverTerminalState()
    {
        var timestamp = DateTimeOffset.Parse("2026-07-28T15:30:00Z");
        var task = CreateTask(GenerationTaskStatus.Succeeded, timestamp);

        Assert.False(task.RecoverInterrupted(timestamp.AddMinutes(1)));
        Assert.Equal(GenerationTaskStatus.Succeeded, task.Status);
    }

    [Fact]
    public void GenerationTask_RejectsBackdatedTransition()
    {
        var timestamp = DateTimeOffset.Parse("2026-07-28T15:40:00Z");
        var task = CreateTask(GenerationTaskStatus.Queued, timestamp);

        Assert.Throws<ArgumentOutOfRangeException>(() => task.Start(timestamp.AddSeconds(-1)));
    }

    private static GenerationTask CreateTask(
        GenerationTaskStatus status,
        DateTimeOffset timestamp,
        int? queuePosition = null)
    {
        return new GenerationTask(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            status,
            attemptCount: status is GenerationTaskStatus.Running ? 1 : 0,
            maxRetries: 0,
            timestamp,
            timestamp,
            queuePosition: queuePosition);
    }
}
