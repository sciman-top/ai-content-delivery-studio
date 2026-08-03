using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Core.Generation;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentDeliveryStudio.Tests;

public sealed class GenerationQueuePersistenceRecoveryTests
{
    [Fact]
    public async Task InitializeAndLoad_AddsQueueCompatibilityColumnsAndPersistsInterruptedRecovery()
    {
        var databaseDirectory = Path.Combine(
            Path.GetTempPath(),
            "ContentDeliveryStudio.Tests",
            Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "legacy-generation-queue.sqlite");
        Directory.CreateDirectory(databaseDirectory);

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;
            var project = CreateProjectWithRunningTask();

            await using (var legacy = new AppDbContext(options))
            {
                await legacy.Database.EnsureCreatedAsync();
                legacy.Projects.Add(project);
                await legacy.SaveChangesAsync();
                await legacy.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE \"GenerationTasks\" DROP COLUMN \"ErrorMessage\";");
                await legacy.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE \"GenerationTasks\" DROP COLUMN \"QueuePosition\";");
                await legacy.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE \"GenerationTasks\" DROP COLUMN \"RetryOfTaskId\";");
                await legacy.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE \"GenerationTasks\" DROP COLUMN \"ApprovalReceipt\";");
            }

            await using (var upgrade = new AppDbContext(options))
            {
                await AppDatabaseInitializer.InitializeAsync(upgrade, CancellationToken.None);
                await AppDatabaseInitializer.InitializeAsync(upgrade, CancellationToken.None);
            }

            await using (var recover = new AppDbContext(options))
            {
                var service = new ProjectApplicationService(new EfProjectRepository(recover));
                var loaded = await service.LoadProjectAsync(project.Id, CancellationToken.None);
                var recoveredTask = loaded!.Series.Single().Items.Single().GenerationTasks.Single();

                Assert.Equal(GenerationTaskStatus.Failed, recoveredTask.Status);
                Assert.Contains("interrupted", recoveredTask.ErrorMessage, StringComparison.OrdinalIgnoreCase);
                Assert.Null(recoveredTask.QueuePosition);
                Assert.Null(recoveredTask.RetryOfTaskId);
                Assert.Null(recoveredTask.ApprovalReceipt);
            }

            await using (var verify = new AppDbContext(options))
            {
                var persistedTask = await verify.GenerationTasks.AsNoTracking().SingleAsync();
                var compatibilityColumns = await GetCompatibilityColumnsAsync(verify);

                Assert.Equal(GenerationTaskStatus.Failed, persistedTask.Status);
                Assert.Contains("interrupted", persistedTask.ErrorMessage, StringComparison.OrdinalIgnoreCase);
                Assert.Null(persistedTask.QueuePosition);
                Assert.Null(persistedTask.RetryOfTaskId);
                Assert.Null(persistedTask.ApprovalReceipt);
                Assert.Equal(["ApprovalReceipt", "ErrorMessage", "QueuePosition", "RetryOfTaskId"], compatibilityColumns);
            }
        }
        finally
        {
            if (Directory.Exists(databaseDirectory))
            {
                Directory.Delete(databaseDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task NewSchema_RoundTripsPausedPositionRetryProvenanceAndApprovalReceipt()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            "ContentDeliveryStudio.Tests",
            $"queue-roundtrip-{Guid.NewGuid():N}.sqlite");
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;

        try
        {
            var project = CreateProjectWithPausedRetryTask();
            await using (var create = new AppDbContext(options))
            {
                await AppDatabaseInitializer.InitializeAsync(create, CancellationToken.None);
                create.Projects.Add(project);
                await create.SaveChangesAsync();
            }

            await using var verify = new AppDbContext(options);
            var task = await verify.GenerationTasks.AsNoTracking().SingleAsync();

            Assert.Equal(GenerationTaskStatus.Paused, task.Status);
            Assert.Equal(3, task.QueuePosition);
            Assert.NotNull(task.RetryOfTaskId);
            Assert.NotNull(task.ApprovalReceipt);
            Assert.Equal("paid-image-v1", task.ApprovalReceipt.ModelId);
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    private static ImageProject CreateProjectWithRunningTask()
    {
        var timestamp = DateTimeOffset.Parse("2026-07-28T17:30:00Z");
        var project = ImageProject.Create("Legacy queue project", timestamp);
        var series = project.AddSeries("Series", "Legacy queue", timestamp.AddMinutes(1));
        var item = series.AddItem("Item", "Legacy running item", timestamp.AddMinutes(2));
        var profile = project.AddProviderProfile("Fake provider", ProviderKind.Fake, timestamp.AddMinutes(3));
        var prompt = item.AddPromptVersion(
            "Legacy running prompt",
            new GenerationSettings(1024, 1024, "standard", "png"),
            profile.Id,
            timestamp.AddMinutes(4));
        var task = item.AddGenerationTask(
            new GenerationTask(
                Guid.NewGuid(),
                item.Id,
                prompt.Id,
                profile.Id,
                GenerationTaskStatus.Running,
                attemptCount: 1,
                maxRetries: 0,
                timestamp.AddMinutes(5),
                timestamp.AddMinutes(6),
                queuePosition: 7,
                retryOfTaskId: Guid.NewGuid()),
            timestamp.AddMinutes(6));
        return project;
    }

    private static ImageProject CreateProjectWithPausedRetryTask()
    {
        var timestamp = DateTimeOffset.Parse("2026-07-29T14:30:00Z");
        var project = ImageProject.Create("Queue roundtrip", timestamp);
        var series = project.AddSeries("Series", "Queue roundtrip", timestamp.AddMinutes(1));
        var item = series.AddItem("Item", "Paused retry", timestamp.AddMinutes(2));
        var profile = project.AddProviderProfile("Fake provider", ProviderKind.Fake, timestamp.AddMinutes(3));
        var prompt = item.AddPromptVersion(
            "Paused retry prompt",
            new GenerationSettings(1024, 1024, "standard", "png"),
            profile.Id,
            timestamp.AddMinutes(4));
        var task = item.AddGenerationTask(
            new GenerationTask(
                Guid.NewGuid(),
                item.Id,
                prompt.Id,
                profile.Id,
                GenerationTaskStatus.Paused,
                attemptCount: 0,
                maxRetries: 0,
                timestamp.AddMinutes(5),
                timestamp.AddMinutes(5),
                queuePosition: 3,
                retryOfTaskId: Guid.NewGuid()),
            timestamp.AddMinutes(5));
        var requestSet = new GenerationApprovalRequestSet(
            project.Id,
            "paid-image",
            "images",
            "paid-image-v1",
            [new GenerationApprovalOperation(
                task.Id,
                item.SeriesId!.Value,
                prompt.Id,
                profile.Id,
                prompt.PromptText,
                "prompt-sha",
                prompt.Settings.Width,
                prompt.Settings.Height,
                prompt.Settings.Quality,
                prompt.Settings.OutputFormat,
                "auto",
                prompt.Settings.Seed,
                task.MaxRetries,
                0.10m)]);
        task.AttachApproval(
            GenerationApprovalReceipt.Issue(
                requestSet,
                0.10m,
                0.20m,
                "operator:test",
                "authority:test-001",
                timestamp.AddMinutes(5),
                timestamp.AddHours(1)),
            timestamp.AddMinutes(5));
        return project;
    }

    private static async Task<IReadOnlyList<string>> GetCompatibilityColumnsAsync(AppDbContext dbContext)
    {
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('GenerationTasks');";
        await using var reader = await command.ExecuteReaderAsync();
        var columns = new List<string>();
        while (await reader.ReadAsync())
        {
            var name = reader.GetString(1);
            if (name is "ErrorMessage" or "QueuePosition" or "RetryOfTaskId" or "ApprovalReceipt")
            {
                columns.Add(name);
            }
        }

        return columns.OrderBy(name => name, StringComparer.Ordinal).ToArray();
    }
}
