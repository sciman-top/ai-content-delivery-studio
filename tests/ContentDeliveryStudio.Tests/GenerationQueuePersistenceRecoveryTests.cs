using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentDeliveryStudio.Tests;

public sealed class GenerationQueuePersistenceRecoveryTests
{
    [Fact]
    public async Task InitializeAndLoad_AddsErrorColumnAndPersistsInterruptedRecovery()
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
            }

            await using (var verify = new AppDbContext(options))
            {
                var persistedTask = await verify.GenerationTasks.AsNoTracking().SingleAsync();
                var columnCount = await CountErrorMessageColumnsAsync(verify);

                Assert.Equal(GenerationTaskStatus.Failed, persistedTask.Status);
                Assert.Contains("interrupted", persistedTask.ErrorMessage, StringComparison.OrdinalIgnoreCase);
                Assert.Equal(1, columnCount);
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
        item.AddGenerationTask(
            new GenerationTask(
                Guid.NewGuid(),
                item.Id,
                prompt.Id,
                profile.Id,
                GenerationTaskStatus.Running,
                attemptCount: 1,
                maxRetries: 0,
                timestamp.AddMinutes(5),
                timestamp.AddMinutes(6)),
            timestamp.AddMinutes(6));
        return project;
    }

    private static async Task<int> CountErrorMessageColumnsAsync(AppDbContext dbContext)
    {
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('GenerationTasks');";
        await using var reader = await command.ExecuteReaderAsync();
        var count = 0;
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), "ErrorMessage", StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }
}
