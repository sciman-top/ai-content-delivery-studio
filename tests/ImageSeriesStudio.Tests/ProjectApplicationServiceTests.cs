using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImageSeriesStudio.Tests;

public sealed class ProjectApplicationServiceTests
{
    [Fact]
    public async Task ProjectApplicationService_CreatesAndLoadsProjectFromSqlite()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "project-service.sqlite");
        Directory.CreateDirectory(databaseDirectory);

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;

            await using (var setup = new AppDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
            }

            var repository = new EfProjectRepository(new AppDbContext(options));
            var service = new ProjectApplicationService(repository);
            var timestamp = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);

            var created = await service.CreateProjectAsync("Bilingual demo", timestamp, CancellationToken.None);
            var loaded = await service.LoadProjectAsync(created.Id, CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal(created.Id, loaded.Id);
            Assert.Equal("Bilingual demo", loaded.Name);
            Assert.Equal(timestamp, loaded.CreatedAt);
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
    public async Task ProjectApplicationService_ListsProjectsInUpdatedOrder()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "project-list.sqlite");
        Directory.CreateDirectory(databaseDirectory);

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;

            await using (var setup = new AppDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
            }

            var service = new ProjectApplicationService(new EfProjectRepository(new AppDbContext(options)));
            var first = await service.CreateProjectAsync(
                "First project",
                new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero),
                CancellationToken.None);
            var second = await service.CreateProjectAsync(
                "Second project",
                new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero),
                CancellationToken.None);

            var projects = await service.ListProjectsAsync(CancellationToken.None);

            Assert.Collection(
                projects,
                project => Assert.Equal(second.Id, project.Id),
                project => Assert.Equal(first.Id, project.Id));
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
    public async Task ProjectApplicationService_AddsSeriesAndItemsToProject()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "project-plan.sqlite");
        Directory.CreateDirectory(databaseDirectory);

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;

            await using (var setup = new AppDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
            }

            var service = new ProjectApplicationService(new EfProjectRepository(new AppDbContext(options)));
            var timestamp = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);
            var project = await service.CreateProjectAsync("Plan demo", timestamp, CancellationToken.None);

            var series = await service.AddSeriesAsync(
                project.Id,
                "Poster series",
                "A classroom poster set",
                timestamp.AddMinutes(1),
                CancellationToken.None);
            var item = await service.AddItemAsync(
                project.Id,
                series.Id,
                "Cover image",
                "Opening classroom visual",
                timestamp.AddMinutes(2),
                CancellationToken.None);

            var loaded = await service.LoadProjectAsync(project.Id, CancellationToken.None);
            var loadedSeries = Assert.Single(loaded!.Series);
            var loadedItem = Assert.Single(loadedSeries.Items);

            Assert.Equal(series.Id, loadedSeries.Id);
            Assert.Equal(item.Id, loadedItem.Id);
            Assert.Equal("Cover image", loadedItem.Title);
        }
        finally
        {
            if (Directory.Exists(databaseDirectory))
            {
                Directory.Delete(databaseDirectory, recursive: true);
            }
        }
    }
}
