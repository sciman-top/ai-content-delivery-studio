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
                        timestamp.AddMinutes(3)),
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
                Assert.Equal("conservative", Assert.Single(loadedBrief.PromptDirections).Key);
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
}
