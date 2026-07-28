using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.Tests;

public sealed class GenerationTaskTests
{
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

    [Theory]
    [InlineData(GenerationTaskStatus.Queued, GenerationTaskStatus.Cancelled, "not dispatched")]
    [InlineData(GenerationTaskStatus.Running, GenerationTaskStatus.Failed, "interrupted")]
    public void GenerationTask_RecoversIncompleteStateWithoutRequeue(
        GenerationTaskStatus initialStatus,
        GenerationTaskStatus expectedStatus,
        string expectedReasonFragment)
    {
        var timestamp = DateTimeOffset.Parse("2026-07-28T15:20:00Z");
        var task = CreateTask(initialStatus, timestamp);

        var changed = task.RecoverInterrupted(timestamp.AddMinutes(1));

        Assert.True(changed);
        Assert.Equal(expectedStatus, task.Status);
        Assert.Contains(expectedReasonFragment, task.ErrorMessage, StringComparison.OrdinalIgnoreCase);
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
        DateTimeOffset timestamp)
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
            timestamp);
    }
}
