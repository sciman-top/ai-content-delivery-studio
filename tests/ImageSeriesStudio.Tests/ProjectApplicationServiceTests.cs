using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Infrastructure.Fakes;
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

    [Fact]
    public async Task ProjectApplicationService_AddsPromptVersionWithDefaultFakeProfile()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "project-prompts.sqlite");
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
            var project = await service.CreateProjectAsync("Prompt demo", timestamp, CancellationToken.None);
            var series = await service.AddSeriesAsync(
                project.Id,
                "Article images",
                "Two illustrations",
                timestamp.AddMinutes(1),
                CancellationToken.None);
            var item = await service.AddItemAsync(
                project.Id,
                series.Id,
                "Opening image",
                "A bright workbench scene",
                timestamp.AddMinutes(2),
                CancellationToken.None);

            var prompt = await service.AddPromptVersionAsync(
                project.Id,
                item.Id,
                "Create a clean editorial image set.",
                new GenerationSettings(1024, 1024, "standard", "png", 11),
                providerProfileId: null,
                timestamp.AddMinutes(3),
                CancellationToken.None);

            var loaded = await service.LoadProjectAsync(project.Id, CancellationToken.None);
            var loadedItem = loaded!.Series.Single().Items.Single();
            var loadedPrompt = Assert.Single(loadedItem.PromptVersions);
            var loadedProfile = Assert.Single(loaded.ProviderProfiles);

            Assert.Equal(prompt.Id, loadedPrompt.Id);
            Assert.Equal(1, loadedPrompt.VersionNumber);
            Assert.Equal("Create a clean editorial image set.", loadedPrompt.PromptText);
            Assert.Equal(1024, loadedPrompt.Settings.Width);
            Assert.Equal(ProviderKind.Fake, loadedProfile.Kind);
            Assert.Equal(loadedProfile.Id, loadedPrompt.ProviderProfileId);
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
    public async Task ProjectApplicationService_CreatesPlanWithFakeProvider()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "project-fake-plan.sqlite");
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

            var service = new ProjectApplicationService(
                new EfProjectRepository(new AppDbContext(options)),
                new FakeTextPlanningProvider());
            var timestamp = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);
            var project = await service.CreateProjectAsync("Fake planning demo", timestamp, CancellationToken.None);

            var series = await service.CreatePlanWithProviderAsync(
                project.Id,
                new PlanningRequest(
                    "three article illustrations",
                    "content authors",
                    3,
                    "clean editorial style"),
                timestamp.AddMinutes(1),
                CancellationToken.None);

            var loaded = await service.LoadProjectAsync(project.Id, CancellationToken.None);
            var loadedSeries = Assert.Single(loaded!.Series);

            Assert.Equal(series.Id, loadedSeries.Id);
            Assert.Equal("three article illustrations", loadedSeries.Title);
            Assert.Equal(3, loadedSeries.Items.Count);
            Assert.All(loadedSeries.Items, item => Assert.Single(item.PromptVersions));
            Assert.Single(loaded.ProviderProfiles);
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
    public async Task ProjectApplicationService_RunsGenerationQueueWithFakeProvider()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "project-fake-generation.sqlite");
        var outputDirectory = Path.Combine(databaseDirectory, "generated");
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

            var service = new ProjectApplicationService(
                new EfProjectRepository(new AppDbContext(options)),
                new FakeTextPlanningProvider(),
                new FakeImageGenerationProvider());
            var timestamp = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);
            var project = await service.CreateProjectAsync("Fake generation demo", timestamp, CancellationToken.None);
            await service.CreatePlanWithProviderAsync(
                project.Id,
                new PlanningRequest("two concept images", "designers", 2, "bright studio"),
                timestamp.AddMinutes(1),
                CancellationToken.None);

            var run = await service.RunGenerationQueueAsync(project.Id, outputDirectory, CancellationToken.None);

            Assert.Equal(2, run.Tasks.Count);
            Assert.All(run.Tasks, task => Assert.Equal(GenerationTaskStatus.Succeeded, task.Status));
            Assert.Equal(2, run.Images.Count);
            Assert.All(run.Images, image => Assert.True(File.Exists(image.AssetPath)));
            Assert.All(run.Images, image => Assert.True(File.Exists(image.MetadataPath)));
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
    public async Task ProjectApplicationService_RunsVisionReviewWithFakeProvider()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "project-fake-review.sqlite");
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

            var service = new ProjectApplicationService(
                new EfProjectRepository(new AppDbContext(options)),
                textPlanningProvider: null,
                imageGenerationProvider: null,
                visionReviewProvider: new FakeVisionReviewProvider(defaultPasses: true));
            var timestamp = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);
            var project = await service.CreateProjectAsync("Fake review demo", timestamp, CancellationToken.None);

            var reviews = await service.RunVisionReviewAsync(
                project.Id,
                [
                    new ReviewCandidateInput(
                        Guid.NewGuid(),
                        "Opening image",
                        Path.Combine(databaseDirectory, "candidate.png"),
                        "A clean editorial candidate."),
                ],
                CancellationToken.None);

            var review = Assert.Single(reviews);
            Assert.Equal(ReviewDecision.Pass, review.Decision);
            Assert.Empty(review.HardFailures);
            Assert.Contains("match", review.Scores.Keys);
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
