using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Styles;
using ImageSeriesStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImageSeriesStudio.Tests;

public sealed class PersistenceTests
{
    [Fact]
    public async Task AppDbContext_SavesAndLoadsCompleteFakeProject()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "project.sqlite");
        Directory.CreateDirectory(databaseDirectory);

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;

            var timestamp = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);
            var project = ImageProject.Create("Sample project", timestamp);
            var profile = project.AddProviderProfile("Fake provider", ProviderKind.Fake, timestamp.AddMinutes(1));
            var series = project.AddSeries("Physics posters", "Poster series", timestamp.AddMinutes(2));
            var brief = series.AddCreativeBrief(
                "Physics classroom poster",
                "middle school teachers",
                ImageTextPolicy.DeterministicPostRender,
                "clean editorial science style",
                ["accurate formula area"],
                ["model-rendered small text"],
                timestamp.AddMinutes(2));
            var recommendation = PromptDirectionRecommendation.Create(
                ImageTypePresetCatalog.EducationalPoster,
                ImageTextPolicy.DeterministicPostRender,
                "clean editorial science style",
                new AspectRatio(4, 5),
                1024,
                1280,
                "draft",
                "png",
                ImageBackgroundMode.Opaque,
                ReviewRubricTemplateCatalog.TextHeavyPoster,
                draftCount: 2,
                finalCount: 1,
                "Educational poster output needs post-render text.",
                confidence: 0.9,
                ["model-rendered small text is risky"],
                ["reserve label space"]);
            brief.ReplaceDirections(
                [
                    PromptDirection.Create(
                        "conservative",
                        "Conservative faithful",
                        "Use for accurate classroom delivery.",
                        "Create a clean science background.",
                        "No unreadable formula text.",
                        "Accurate and easy to review.",
                        "Less dramatic than a cover image.",
                        timestamp.AddMinutes(3),
                        recommendation),
                ],
                timestamp.AddMinutes(4));
            var item = series.AddItem("Cover", "Opening image", timestamp.AddMinutes(3));
            item.AddPromptVersion(
                "A clean blue poster background",
                new GenerationSettings(1024, 1024, "standard", "png", 42),
                profile.Id,
                timestamp.AddMinutes(4));
            item.MarkReady(timestamp.AddMinutes(5));

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                db.Projects.Add(project);
                await db.SaveChangesAsync();
            }

            await using (var db = new AppDbContext(options))
            {
                var loaded = await db.Projects
                    .Include(project => project.ProviderProfiles)
                    .Include(project => project.Series)
                    .ThenInclude(series => series.CreativeBriefs)
                    .Include(project => project.Series)
                    .ThenInclude(series => series.Items)
                    .ThenInclude(item => item.PromptVersions)
                    .SingleAsync();

                var loadedSeries = Assert.Single(loaded.Series);
                var loadedItem = Assert.Single(loadedSeries.Items);
                var loadedPrompt = Assert.Single(loadedItem.PromptVersions);

                Assert.Equal("Sample project", loaded.Name);
                Assert.Equal("Physics posters", loadedSeries.Title);
                var loadedBrief = Assert.Single(loadedSeries.CreativeBriefs);
                Assert.Equal("Physics classroom poster", loadedBrief.Goal);
                var loadedDirection = Assert.Single(loadedBrief.PromptDirections);
                Assert.Equal("conservative", loadedDirection.Key);
                Assert.NotNull(loadedDirection.Recommendation);
                Assert.Equal(ImageTypePresetCatalog.EducationalPoster, loadedDirection.Recommendation.ImageTypePresetId);
                Assert.Equal(1024, loadedDirection.Recommendation.Width);
                Assert.Equal(1280, loadedDirection.Recommendation.Height);
                Assert.Equal("model-rendered small text is risky", Assert.Single(loadedDirection.Recommendation.CapabilityWarnings));
                Assert.Equal(SeriesItemStatus.Ready, loadedItem.Status);
                Assert.Equal("A clean blue poster background", loadedPrompt.PromptText);
                Assert.Single(loaded.ProviderProfiles);
            }
        }
        finally
        {
            if (Directory.Exists(databaseDirectory))
            {
                Directory.Delete(databaseDirectory, recursive: true);
            }
        }

        Assert.False(File.Exists(databasePath));
        Assert.DoesNotContain("ai-image-series-studio", databasePath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PersistsDocumentBriefsAndIllustrationPlans()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "document-illustration.sqlite");
        Directory.CreateDirectory(databaseDirectory);

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;

            var timestamp = DateTimeOffset.Parse("2026-06-02T10:00:00Z");
            var project = ImageProject.Create("Document persistence", timestamp);
            var brief = DocumentBrief.Create(
                project.Id,
                DocumentSourceKind.Markdown,
                "article.md",
                "Article",
                DocumentFamily.Editorial,
                "readers",
                ["intro"],
                ["central claim"],
                ["cover"],
                ["avoid fake data"],
                IllustrationStrictnessLevel.Editorial,
                timestamp);
            var target = IllustrationTarget.Create(
                brief.Id,
                "Cover",
                "intro",
                IllustrationPurpose.Cover,
                ["central claim"],
                ["fake data"],
                ["central claim"],
                ImageTypePresetCatalog.ArticleCover,
                ReviewRubricTemplateCatalog.EditorialIllustration,
                ImageTextPolicy.Hybrid,
                ["editorial"],
                timestamp);
            var plan = IllustrationPlan.Create(
                project.Id,
                brief.Id,
                "Cover plan",
                [target],
                ["intro covered"],
                ["no data chart"],
                timestamp);
            project.AddDocumentBrief(brief, timestamp);
            project.AddIllustrationPlan(plan, timestamp);

            await using (var setup = new AppDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
                setup.Projects.Add(project);
                await setup.SaveChangesAsync();
            }

            await using (var read = new AppDbContext(options))
            {
                var loaded = await read.Projects
                    .Include(value => value.DocumentBriefs)
                    .Include(value => value.IllustrationPlans)
                    .SingleAsync();

                Assert.Single(loaded.DocumentBriefs);
                Assert.Single(loaded.IllustrationPlans);
                Assert.Equal("Article", loaded.DocumentBriefs.Single().Title);
                Assert.Equal("Cover plan", loaded.IllustrationPlans.Single().Summary);
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
}
