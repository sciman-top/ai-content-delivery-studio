using System.Text.Json;
using ImageSeriesStudio.Application.Delivery;
using ImageSeriesStudio.Application.Projects;
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

            await using var verification = new AppDbContext(options);
            Assert.Equal(2, await verification.GenerationTasks.CountAsync());
            Assert.Equal(2, await verification.CandidateImages.CountAsync());
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
    public async Task ProjectApplicationService_RecordsFinalApprovalDecision()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "project-final-approval.sqlite");
        Directory.CreateDirectory(databaseDirectory);

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;

            var timestamp = new DateTimeOffset(2026, 6, 7, 10, 0, 0, TimeSpan.Zero);
            Guid projectId;
            Guid candidateId;

            await using (var setup = new AppDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();

                var project = ImageProject.Create("Approval persistence demo", timestamp);
                var profile = project.AddProviderProfile("Fake provider", ProviderKind.Fake, timestamp.AddMinutes(1));
                var series = project.AddSeries("Approval series", "Series", timestamp.AddMinutes(2));
                var item = series.AddItem("Opening", "Opening candidate.", timestamp.AddMinutes(3));
                var prompt = item.AddPromptVersion(
                    "Create an approval-ready image.",
                    new GenerationSettings(1024, 1024, "standard", "png"),
                    profile.Id,
                    timestamp.AddMinutes(4));
                var task = new GenerationTask(
                    Guid.NewGuid(),
                    item.Id,
                    prompt.Id,
                    profile.Id,
                    GenerationTaskStatus.Succeeded,
                    attemptCount: 1,
                    maxRetries: 1,
                    timestamp.AddMinutes(5),
                    timestamp.AddMinutes(6));
                var candidate = new CandidateImage(
                    Guid.NewGuid(),
                    item.Id,
                    prompt.Id,
                    task.Id,
                    profile.Id,
                    CandidateImageStatus.ReviewPending,
                    "outputs/review/final.png",
                    "outputs/review/final.json",
                    timestamp.AddMinutes(7));

                setup.Projects.Add(project);
                setup.GenerationTasks.Add(task);
                setup.CandidateImages.Add(candidate);
                await setup.SaveChangesAsync();

                projectId = project.Id;
                candidateId = candidate.Id;
            }

            await using (var db = new AppDbContext(options))
            {
                var service = new ProjectApplicationService(new EfProjectRepository(db));
                await service.RecordFinalApprovalAsync(
                    projectId,
                    new FinalApprovalRequest(
                        new StructuredReviewOutput(
                            candidateId,
                            ReviewDecision.Pass,
                            [new StructuredReviewScore("match", "Match prompt.", 3, 5)],
                            [],
                            "Looks correct.",
                            null),
                        Approve: true,
                        Reviewer: "Teacher",
                        Notes: "Ready for package."),
                    timestamp.AddMinutes(8),
                    CancellationToken.None);
            }

            await using (var db = new AppDbContext(options))
            {
                var review = await db.ReviewResults.SingleAsync();

                Assert.Equal(candidateId, review.CandidateImageId);
                Assert.True(review.HumanApproved);
                Assert.Equal("Teacher", review.FinalReviewer);
                Assert.Equal("Ready for package.", review.FinalApprovalNotes);
                Assert.Equal(timestamp.AddMinutes(8), review.FinalApprovalDecidedAt);
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

    private sealed class InMemoryProjectRepository : IProjectRepository
    {
        private readonly Dictionary<Guid, ImageProject> _projects = new();
        private readonly Dictionary<Guid, ReviewResult> _reviewResultsByCandidateId = new();

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

        public Task SaveReviewResultAsync(Guid projectId, ReviewResult reviewResult, CancellationToken cancellationToken)
        {
            _reviewResultsByCandidateId[reviewResult.CandidateImageId] = reviewResult;
            return Task.CompletedTask;
        }

        public Task<ReviewResult?> LoadLatestReviewResultAsync(Guid candidateImageId, CancellationToken cancellationToken)
        {
            _reviewResultsByCandidateId.TryGetValue(candidateImageId, out var review);
            return Task.FromResult(review);
        }
    }
}
