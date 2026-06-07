using System.Text.Json;
using ImageSeriesStudio.Application.Delivery;
using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Core.Styles;
using ImageSeriesStudio.Infrastructure.Delivery;
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
            Assert.Equal(SeriesItemKind.Standard, loadedItem.Kind);
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
    public async Task ProjectApplicationService_AddsExplicitItemKind()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "project-item-kind.sqlite");
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
            var timestamp = new DateTimeOffset(2026, 6, 3, 11, 30, 0, TimeSpan.Zero);
            var project = await service.CreateProjectAsync("Kind demo", timestamp, CancellationToken.None);
            var series = await service.AddSeriesAsync(
                project.Id,
                "Storyboard",
                "Panel sequence",
                timestamp.AddMinutes(1),
                CancellationToken.None);

            var item = await service.AddItemAsync(
                project.Id,
                series.Id,
                "Panel 1",
                "Opening panel.",
                SeriesItemKind.Panel,
                timestamp.AddMinutes(2),
                CancellationToken.None);

            var loaded = await service.LoadProjectAsync(project.Id, CancellationToken.None);
            var loadedItem = Assert.Single(loaded!.Series.Single().Items);

            Assert.Equal(item.Id, loadedItem.Id);
            Assert.Equal(SeriesItemKind.Panel, loadedItem.Kind);
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
    public async Task ProjectApplicationService_CreatesBriefDirectionsAndPromotesPromptVersion()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "project-brief.sqlite");
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
            var timestamp = new DateTimeOffset(2026, 6, 2, 9, 0, 0, TimeSpan.Zero);
            var project = await service.CreateProjectAsync("Brief demo", timestamp, CancellationToken.None);
            var series = await service.AddSeriesAsync(
                project.Id,
                "Article images",
                "Series",
                timestamp.AddMinutes(1),
                CancellationToken.None);
            var item = await service.AddItemAsync(
                project.Id,
                series.Id,
                "Opening",
                "Opening visual",
                timestamp.AddMinutes(2),
                CancellationToken.None);

            var brief = await service.CreateCreativeBriefAsync(
                project.Id,
                series.Id,
                "article illustration",
                "teachers",
                ImageTextPolicy.DeterministicPostRender,
                "clean editorial",
                ["accurate visual"],
                ["small fake text"],
                timestamp.AddMinutes(3),
                CancellationToken.None);

            var planned = await service.CreatePromptDirectionsAsync(
                project.Id,
                brief.Id,
                timestamp.AddMinutes(4),
                CancellationToken.None);

            var promoted = await service.PromotePromptDirectionAsync(
                project.Id,
                item.Id,
                brief.Id,
                "conservative",
                new GenerationSettings(1024, 1024, "standard", "png"),
                timestamp.AddMinutes(5),
                CancellationToken.None);

            var loaded = await service.LoadProjectAsync(project.Id, CancellationToken.None);
            var loadedBrief = loaded!.Series.Single().CreativeBriefs.Single();
            var loadedPrompt = loaded.Series.Single().Items.Single().PromptVersions.Single();
            var loadedDirection = loadedBrief.PromptDirections.Single(direction => direction.Key == "conservative");

            Assert.Equal(brief.Id, loadedBrief.Id);
            Assert.Equal(3, planned.PromptDirections.Count);
            Assert.NotNull(loadedDirection.Recommendation);
            Assert.Equal(ImageTypePresetCatalog.ArticleInlineIllustration, loadedDirection.Recommendation.ImageTypePresetId);
            Assert.Equal(1536, loadedDirection.Recommendation.Width);
            Assert.Equal(1024, loadedDirection.Recommendation.Height);
            Assert.Equal(ReviewRubricTemplateCatalog.EditorialIllustration, loadedDirection.Recommendation.ReviewRubricTemplateId);
            Assert.Equal(promoted.Id, loadedPrompt.Id);
            Assert.Contains("article illustration", loadedPrompt.PromptText);
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
    public async Task ProjectApplicationService_CreatesAndPromotesDesignBlueprints()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "project-blueprint.sqlite");
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
            var timestamp = new DateTimeOffset(2026, 6, 3, 9, 0, 0, TimeSpan.Zero);
            var project = await service.CreateProjectAsync("Blueprint demo", timestamp, CancellationToken.None);
            var series = await service.AddSeriesAsync(
                project.Id,
                "Narrative images",
                "Series",
                timestamp.AddMinutes(1),
                CancellationToken.None);
            var brief = await service.CreateCreativeBriefAsync(
                project.Id,
                series.Id,
                "panel story sequence",
                "students",
                ImageTextPolicy.DeterministicPostRender,
                "clear visual storytelling",
                ["same main character"],
                ["wall of unreadable text"],
                timestamp.AddMinutes(2),
                CancellationToken.None);

            var planned = await service.CreateDesignBlueprintsAsync(
                project.Id,
                brief.Id,
                timestamp.AddMinutes(3),
                CancellationToken.None);

            var firstBlueprint = planned.DesignBlueprints.First();
            var promoted = await service.PromoteDesignBlueprintAsync(
                project.Id,
                brief.Id,
                firstBlueprint.Id,
                timestamp.AddMinutes(4),
                CancellationToken.None);

            var loaded = await service.LoadProjectAsync(project.Id, CancellationToken.None);
            var loadedBrief = loaded!.Series.Single().CreativeBriefs.Single();

            Assert.Equal(3, planned.DesignBlueprints.Count);
            Assert.Equal("panel-narrative-sequence", firstBlueprint.Key);
            Assert.Equal(promoted.Id, loadedBrief.PromotedBlueprintId);
            Assert.Equal(promoted.Id, loadedBrief.DesignBlueprints.First().Id);
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
    public async Task ProjectApplicationService_PromotesPromptDirectionWithRecommendedSettings()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "project-brief-recommendation.sqlite");
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
            var timestamp = new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);
            var project = await service.CreateProjectAsync("Recommendation promotion demo", timestamp, CancellationToken.None);
            var series = await service.AddSeriesAsync(
                project.Id,
                "Article images",
                "Series",
                timestamp.AddMinutes(1),
                CancellationToken.None);
            var item = await service.AddItemAsync(
                project.Id,
                series.Id,
                "Opening",
                "Opening visual",
                timestamp.AddMinutes(2),
                CancellationToken.None);
            var brief = await service.CreateCreativeBriefAsync(
                project.Id,
                series.Id,
                "article illustration",
                "teachers",
                ImageTextPolicy.Hybrid,
                "clean editorial",
                ["accurate visual"],
                ["small fake text"],
                timestamp.AddMinutes(3),
                CancellationToken.None);
            await service.CreatePromptDirectionsAsync(
                project.Id,
                brief.Id,
                timestamp.AddMinutes(4),
                CancellationToken.None);

            var promoted = await service.PromotePromptDirectionAsync(
                project.Id,
                item.Id,
                brief.Id,
                "conservative",
                timestamp.AddMinutes(5),
                CancellationToken.None);

            var loaded = await service.LoadProjectAsync(project.Id, CancellationToken.None);
            var loadedPrompt = loaded!.Series.Single().Items.Single().PromptVersions.Single();

            Assert.Equal(promoted.Id, loadedPrompt.Id);
            Assert.Equal(1536, loadedPrompt.Settings.Width);
            Assert.Equal(1024, loadedPrompt.Settings.Height);
            Assert.Equal("draft", loadedPrompt.Settings.Quality);
            Assert.Equal("png", loadedPrompt.Settings.OutputFormat);
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
    public async Task ProjectApplicationService_PersistsGeneratedCandidatesAndHumanReviewDecisions()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "project-review-persistence.sqlite");
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

            var timestamp = new DateTimeOffset(2026, 6, 7, 10, 0, 0, TimeSpan.Zero);
            Guid projectId;

            await using (var db = new AppDbContext(options))
            {
                var service = new ProjectApplicationService(
                    new EfProjectRepository(db),
                    new FakeTextPlanningProvider(),
                    new FakeImageGenerationProvider());
                var project = await service.CreateProjectAsync("Persisted approval demo", timestamp, CancellationToken.None);
                projectId = project.Id;
                var series = await service.AddSeriesAsync(
                    project.Id,
                    "Approval series",
                    "Series for persisted review decisions.",
                    timestamp.AddMinutes(1),
                    CancellationToken.None);
                var item = await service.AddItemAsync(
                    project.Id,
                    series.Id,
                    "Opening frame",
                    "Opening visual",
                    timestamp.AddMinutes(2),
                    CancellationToken.None);
                await service.AddPromptVersionAsync(
                    project.Id,
                    item.Id,
                    "Create a clean opening frame.",
                    new GenerationSettings(1024, 1024, "standard", "png", 7),
                    providerProfileId: null,
                    timestamp.AddMinutes(3),
                    CancellationToken.None);

                await service.RunGenerationQueueAsync(project.Id, outputDirectory, CancellationToken.None);

                var generatedProject = await service.LoadProjectAsync(project.Id, CancellationToken.None);
                var candidate = Assert.Single(generatedProject!.Series.Single().Items.Single().CandidateImages);

                await service.SaveReviewResultAsync(
                    project.Id,
                    new ReviewResult(
                        Guid.NewGuid(),
                        candidate.Id,
                        ReviewDecision.Pass,
                        new Dictionary<string, int> { ["match"] = 5 },
                        [],
                        "Looks ready.",
                        suggestedFix: null,
                        humanApproved: true,
                        humanReviewer: "Teacher",
                        humanReviewNotes: "Approved for delivery.",
                        humanReviewDecidedAt: timestamp.AddMinutes(5),
                        createdAt: timestamp.AddMinutes(5)),
                    timestamp.AddMinutes(5),
                    CancellationToken.None);
            }

            await using (var db = new AppDbContext(options))
            {
                var service = new ProjectApplicationService(new EfProjectRepository(db));
                var loaded = await service.LoadProjectAsync(projectId, CancellationToken.None);
                var loadedItem = loaded!.Series.Single().Items.Single();
                var loadedCandidate = Assert.Single(loadedItem.CandidateImages);
                var loadedReview = Assert.Single(loadedCandidate.ReviewResults);

                Assert.True(File.Exists(loadedCandidate.AssetPath));
                Assert.True(File.Exists(loadedCandidate.MetadataPath));
                Assert.True(loadedReview.HumanApproved);
                Assert.Equal("Teacher", loadedReview.HumanReviewer);
                Assert.Equal("Approved for delivery.", loadedReview.HumanReviewNotes);
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
    public async Task ProjectApplicationService_RunsMaskEditWithFakeProvider()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "project-fake-edit.sqlite");
        var sourcePath = Path.Combine(databaseDirectory, "source.png");
        var maskPath = Path.Combine(databaseDirectory, "mask.png");
        var outputDirectory = Path.Combine(databaseDirectory, "edited");
        Directory.CreateDirectory(databaseDirectory);
        await File.WriteAllBytesAsync(sourcePath, [1, 2, 3], CancellationToken.None);
        await File.WriteAllBytesAsync(maskPath, [4, 5, 6], CancellationToken.None);

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;

            await using (var setup = new AppDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
            }

            var fakeImageProvider = new FakeImageGenerationProvider();
            var service = new ProjectApplicationService(
                new EfProjectRepository(new AppDbContext(options)),
                textPlanningProvider: null,
                imageGenerationProvider: null,
                visionReviewProvider: null,
                deliveryPackageWriter: null,
                imageEditProvider: fakeImageProvider);
            var timestamp = new DateTimeOffset(2026, 6, 2, 17, 0, 0, TimeSpan.Zero);
            var project = await service.CreateProjectAsync("Fake edit demo", timestamp, CancellationToken.None);

            var result = await service.RunImageEditAsync(
                new ImageEditWorkflowRequest(
                    project.Id,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    sourcePath,
                    maskPath,
                    "Clean the masked area.",
                    new GenerationSettings(1024, 1024, "standard", "png"),
                    outputDirectory,
                    "edited.png"),
                CancellationToken.None);

            Assert.True(File.Exists(result.AssetPath));
            Assert.Equal("fake-image-edit", result.ProviderTraceId);
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

    [Fact]
    public async Task ProjectApplicationService_RunsStructuredVisionReviewWithRubricScores()
    {
        var service = new ProjectApplicationService(
            new InMemoryProjectRepository(),
            textPlanningProvider: null,
            imageGenerationProvider: null,
            visionReviewProvider: new FakeVisionReviewProvider(defaultPasses: true));
        var timestamp = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);
        var project = await service.CreateProjectAsync("Structured review demo", timestamp, CancellationToken.None);

        var reviews = await service.RunStructuredVisionReviewAsync(
            project.Id,
            [
                new ReviewCandidateInput(
                    Guid.NewGuid(),
                    "Opening image",
                    Path.Combine(Path.GetTempPath(), "candidate.png"),
                    "A clean editorial candidate."),
            ],
            CancellationToken.None);

        var review = Assert.Single(reviews);
        Assert.Equal(ReviewDecision.Pass, review.Decision);
        Assert.Contains(review.Scores, score => score.Name == "match" && score.Score == 5);
        Assert.False(review.NeedsRepair);
    }

    [Fact]
    public void ProjectApplicationService_RoutesReviewOutcomes()
    {
        var service = new ProjectApplicationService(new InMemoryProjectRepository());
        var review = new StructuredReviewOutput(
            Guid.NewGuid(),
            ReviewDecision.Pass,
            [new StructuredReviewScore("settings", "Aspect ratio should match delivery.", 2, 2)],
            [],
            "Needs settings repair.",
            SuggestedFix: "Adjust aspect ratio settings.");

        var plan = Assert.Single(service.RouteReviewOutcomes([review]));

        Assert.True(plan.RequiresRepair);
        Assert.Equal(ReviewOutcomeTargetLayer.Settings, plan.PrimaryRoute.TargetLayer);
        Assert.Equal(RepairSeverity.Minor, plan.PrimaryRoute.Severity);
    }

    [Fact]
    public async Task ProjectApplicationService_ExportsDeliveryPackage()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var sourceDirectory = Path.Combine(rootDirectory, "source");
        var packageDirectory = Path.Combine(rootDirectory, "delivery");
        Directory.CreateDirectory(sourceDirectory);

        try
        {
            var imagePath = Path.Combine(sourceDirectory, "final.png");
            var metadataPath = Path.Combine(sourceDirectory, "final.json");
            await File.WriteAllBytesAsync(imagePath, [1, 2, 3], CancellationToken.None);
            await File.WriteAllTextAsync(metadataPath, """{"providerId":"fake-image"}""", CancellationToken.None);
            var blueprintId = Guid.NewGuid();

            var service = new ProjectApplicationService(
                new InMemoryProjectRepository(),
                textPlanningProvider: null,
                imageGenerationProvider: null,
                visionReviewProvider: null,
                deliveryPackageWriter: new DeliveryPackageWriter());

            var result = await service.ExportDeliveryPackageAsync(
                new DeliveryExportRequest(
                    "Delivery demo",
                    packageDirectory,
                    [
                        new DeliveryExportItem(
                            "final-image",
                            "Final image",
                            imagePath,
                            metadataPath,
                            "A finished fake image.",
                            ReviewDecision.Pass,
                            HumanApproved: true,
                            Blueprint: new DeliveryBlueprintMetadata(
                                blueprintId,
                                "article-illustration-pack",
                                "Article illustration pack",
                                "editorial",
                                "standard items",
                                "consistent editorial visual hierarchy",
                                "vary section emphasis")),
                    ]),
                CancellationToken.None);

            Assert.True(File.Exists(result.ManifestJsonPath));
            Assert.True(File.Exists(result.ManifestCsvPath));
            Assert.True(File.Exists(result.ReviewReportPath));
            Assert.Single(result.FinalImagePaths);

            using var manifestStream = File.OpenRead(result.ManifestJsonPath);
            using var manifest = await JsonDocument.ParseAsync(manifestStream, cancellationToken: CancellationToken.None);
            var blueprint = manifest.RootElement.GetProperty("items")[0].GetProperty("blueprint");
            Assert.Equal(blueprintId, blueprint.GetProperty("id").GetGuid());
            Assert.Equal("article-illustration-pack", blueprint.GetProperty("key").GetString());
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ProjectApplicationService_CompletesBriefBlueprintReviewAndDeliveryGoldenPathWithFakeProviders()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(rootDirectory, "golden-path.sqlite");
        var generatedDirectory = Path.Combine(rootDirectory, "generated");
        var deliveryDirectory = Path.Combine(rootDirectory, "delivery");
        Directory.CreateDirectory(rootDirectory);

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
                new FakeImageGenerationProvider(),
                new FakeVisionReviewProvider(defaultPasses: true),
                new DeliveryPackageWriter());
            var timestamp = new DateTimeOffset(2026, 6, 7, 13, 0, 0, TimeSpan.Zero);
            var project = await service.CreateProjectAsync("V1 golden path", timestamp, CancellationToken.None);
            var series = await service.AddSeriesAsync(
                project.Id,
                "Short requirement launch route",
                "A creator turns one short requirement into a reviewed image-series delivery.",
                timestamp.AddMinutes(1),
                CancellationToken.None);
            var item = await service.AddItemAsync(
                project.Id,
                series.Id,
                "Opening classroom poster",
                "A clean physics teaching poster opening panel.",
                timestamp.AddMinutes(2),
                CancellationToken.None);

            var brief = await service.CreateCreativeBriefAsync(
                project.Id,
                series.Id,
                "Explain conservation of energy with a three-panel classroom visual",
                "middle school physics teachers",
                ImageTextPolicy.DeterministicPostRender,
                "clear editorial diagrams with deterministic labels",
                ["energy transfer arrows", "teacher-friendly composition"],
                ["unreadable model-rendered text"],
                timestamp.AddMinutes(3),
                CancellationToken.None);

            var blueprints = await service.CreateDesignBlueprintsAsync(
                project.Id,
                brief.Id,
                timestamp.AddMinutes(4),
                CancellationToken.None);
            var promotedBlueprint = await service.PromoteDesignBlueprintAsync(
                project.Id,
                brief.Id,
                blueprints.DesignBlueprints.First().Id,
                timestamp.AddMinutes(5),
                CancellationToken.None);

            await service.CreatePromptDirectionsAsync(
                project.Id,
                brief.Id,
                timestamp.AddMinutes(6),
                CancellationToken.None);
            var prompt = await service.PromotePromptDirectionAsync(
                project.Id,
                item.Id,
                brief.Id,
                "conservative",
                timestamp.AddMinutes(7),
                CancellationToken.None);

            var generation = await service.RunGenerationQueueAsync(
                project.Id,
                generatedDirectory,
                CancellationToken.None);
            var generatedImage = Assert.Single(generation.Images);

            var reviews = await service.RunStructuredVisionReviewAsync(
                project.Id,
                [
                    new ReviewCandidateInput(
                        generatedImage.CandidateImageId,
                        item.Title,
                        generatedImage.AssetPath,
                        prompt.PromptText),
                ],
                CancellationToken.None);
            var review = Assert.Single(reviews).ToReviewResult(
                timestamp.AddMinutes(8),
                humanApproved: true,
                humanReviewer: "Teacher",
                humanReviewNotes: "Approved for launch rehearsal.",
                humanReviewDecidedAt: timestamp.AddMinutes(8));
            await service.SaveReviewResultAsync(
                project.Id,
                review,
                timestamp.AddMinutes(8),
                CancellationToken.None);

            var loaded = await service.LoadProjectAsync(project.Id, CancellationToken.None);
            var loadedSeries = Assert.Single(loaded!.Series);
            var loadedBrief = Assert.Single(loadedSeries.CreativeBriefs);
            var loadedItem = Assert.Single(loadedSeries.Items);
            var candidate = Assert.Single(loadedItem.CandidateImages);
            var loadedReview = Assert.Single(candidate.ReviewResults);

            var delivery = await service.ExportDeliveryPackageAsync(
                new DeliveryExportRequest(
                    loaded.Name,
                    deliveryDirectory,
                    [
                        new DeliveryExportItem(
                            "opening-classroom-poster",
                            loadedItem.Title,
                            candidate.AssetPath,
                            candidate.MetadataPath,
                            prompt.PromptText,
                            loadedReview.Decision,
                            loadedReview.HumanApproved,
                            loadedReview.HumanReviewer,
                            loadedReview.HumanReviewNotes,
                            loadedReview.HumanReviewDecidedAt,
                            Blueprint: new DeliveryBlueprintMetadata(
                                promotedBlueprint.Id,
                                promotedBlueprint.Key,
                                promotedBlueprint.DisplayName,
                                promotedBlueprint.Category,
                                promotedBlueprint.SupportsPanelSequence ? "panel-sequence" : "standard-items",
                                string.Join("; ", promotedBlueprint.ConsistencyRules),
                                string.Join("; ", promotedBlueprint.VariationRules))),
                    ]),
                CancellationToken.None);

            Assert.Equal(promotedBlueprint.Id, loadedBrief.PromotedBlueprintId);
            Assert.Equal(prompt.Id, loadedItem.PromptVersions.Single().Id);
            Assert.True(File.Exists(candidate.AssetPath));
            Assert.True(File.Exists(Assert.Single(delivery.FinalImagePaths)));
            Assert.True(loadedReview.HumanApproved);
            Assert.Equal("Teacher", loadedReview.HumanReviewer);

            using var manifestStream = File.OpenRead(delivery.ManifestJsonPath);
            using var manifest = await JsonDocument.ParseAsync(manifestStream, cancellationToken: CancellationToken.None);
            var manifestItem = manifest.RootElement.GetProperty("items")[0];
            Assert.True(manifestItem.GetProperty("humanApproved").GetBoolean());
            Assert.Equal(promotedBlueprint.Key, manifestItem.GetProperty("blueprint").GetProperty("key").GetString());
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ProjectApplicationService_CompletesDocumentIllustrationSupportingRouteWithFakeProviders()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(rootDirectory, "document-supporting-route.sqlite");
        var generatedDirectory = Path.Combine(rootDirectory, "generated");
        var deliveryDirectory = Path.Combine(rootDirectory, "delivery");
        Directory.CreateDirectory(rootDirectory);

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
                new FakeImageGenerationProvider(),
                new FakeVisionReviewProvider(defaultPasses: true),
                new DeliveryPackageWriter());
            var timestamp = new DateTimeOffset(2026, 6, 7, 14, 0, 0, TimeSpan.Zero);
            var project = await service.CreateProjectAsync("Document supporting route", timestamp, CancellationToken.None);

            var workflow = await service.CreateDocumentIllustrationPlanWithProviderAsync(
                project.Id,
                new DocumentIllustrationPlanningRequest(
                    "Energy conservation article",
                    "The article explains that energy changes form but remains conserved in a closed system.",
                    "physics teachers",
                    DocumentFamily.Educational,
                    IllustrationStrictnessLevel.Educational,
                    ["Introduction", "Classroom activity"],
                    ["Energy transfers between kinetic and potential forms."],
                    ["Use only evidence from the supplied article text.", "Do not invent lab data"]),
                approveAllTargets: true,
                timestamp.AddMinutes(1),
                CancellationToken.None);

            Assert.True(workflow.SeriesId.HasValue);
            Assert.True(workflow.ApprovedTargetCount > 0);

            var generation = await service.RunGenerationQueueAsync(
                project.Id,
                generatedDirectory,
                CancellationToken.None);
            Assert.Equal(workflow.ApprovedTargetCount, generation.Images.Count);

            var generatedProject = await service.LoadProjectAsync(project.Id, CancellationToken.None);
            var generatedSeries = Assert.Single(generatedProject!.Series);
            var candidates = generatedSeries.Items
                .SelectMany(item => item.CandidateImages.Select(candidate =>
                {
                    var prompt = item.PromptVersions.Single(prompt => prompt.Id == candidate.PromptVersionId);
                    return new { Item = item, Candidate = candidate, Prompt = prompt };
                }))
                .ToArray();

            var reviews = await service.RunStructuredVisionReviewAsync(
                project.Id,
                candidates
                    .Select(value => new ReviewCandidateInput(
                        value.Candidate.Id,
                        value.Item.Title,
                        value.Candidate.AssetPath,
                        value.Prompt.PromptText))
                    .ToArray(),
                CancellationToken.None);

            foreach (var review in reviews)
            {
                await service.SaveReviewResultAsync(
                    project.Id,
                    review.ToReviewResult(
                        timestamp.AddMinutes(2),
                        humanApproved: true,
                        humanReviewer: "Teacher",
                        humanReviewNotes: "Supporting document route approved.",
                        humanReviewDecidedAt: timestamp.AddMinutes(2)),
                    timestamp.AddMinutes(2),
                    CancellationToken.None);
            }

            var approvedProject = await service.LoadProjectAsync(project.Id, CancellationToken.None);
            var approvedSeries = Assert.Single(approvedProject!.Series);
            var deliveryItems = approvedSeries.Items
                .SelectMany(item => item.CandidateImages.Select(candidate =>
                {
                    var prompt = item.PromptVersions.Single(prompt => prompt.Id == candidate.PromptVersionId);
                    var review = Assert.Single(candidate.ReviewResults);
                    return new DeliveryExportItem(
                        item.Title,
                        item.Title,
                        candidate.AssetPath,
                        candidate.MetadataPath,
                        prompt.PromptText,
                        review.Decision,
                        review.HumanApproved,
                        review.HumanReviewer,
                        review.HumanReviewNotes,
                        review.HumanReviewDecidedAt);
                }))
                .ToArray();

            var delivery = await service.ExportDeliveryPackageAsync(
                new DeliveryExportRequest(
                    approvedProject.Name,
                    deliveryDirectory,
                    deliveryItems),
                CancellationToken.None);

            Assert.Equal(workflow.ApprovedTargetCount, deliveryItems.Length);
            Assert.Equal(workflow.ApprovedTargetCount, delivery.FinalImagePaths.Count);
            Assert.All(deliveryItems, item => Assert.Contains("Source evidence", item.PromptText));

            using var manifestStream = File.OpenRead(delivery.ManifestJsonPath);
            using var manifest = await JsonDocument.ParseAsync(manifestStream, cancellationToken: CancellationToken.None);
            var manifestItems = manifest.RootElement.GetProperty("items");
            Assert.Equal(workflow.ApprovedTargetCount, manifestItems.GetArrayLength());
            Assert.All(manifestItems.EnumerateArray(), item => Assert.True(item.GetProperty("humanApproved").GetBoolean()));
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    private sealed class InMemoryProjectRepository : IProjectRepository
    {
        private readonly Dictionary<Guid, ImageProject> _projects = new();

        public Task SaveAsync(ImageProject project, CancellationToken cancellationToken)
        {
            _projects[project.Id] = project;
            return Task.CompletedTask;
        }

        public Task<ImageProject?> LoadAsync(Guid projectId, CancellationToken cancellationToken)
        {
            _projects.TryGetValue(projectId, out var project);
            return Task.FromResult(project);
        }

        public Task<IReadOnlyList<ProjectSummary>> ListAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<ProjectSummary> summaries = _projects.Values
                .Select(project => new ProjectSummary(
                    project.Id,
                    project.Name,
                    project.CreatedAt,
                    project.UpdatedAt))
                .OrderByDescending(project => project.UpdatedAt)
                .ToArray();

            return Task.FromResult(summaries);
        }
    }
}
