using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Styles;
using ImageSeriesStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImageSeriesStudio.Tests;

public sealed class PersistenceTests
{
    [Fact]
    public async Task PersistsDocumentBriefsAndIllustrationPlans()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "project.sqlite");
        Directory.CreateDirectory(databaseDirectory);

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;

            var timestamp = new DateTimeOffset(2026, 6, 2, 10, 0, 0, TimeSpan.Zero);
            var project = ImageProject.Create("Document planning project", timestamp);
            var brief = DocumentBrief.Create(
                project.Id,
                DocumentSourceKind.Markdown,
                "lesson.md",
                "Quantum primer",
                DocumentFamily.Educational,
                "science teachers",
                ["Introduction", "Analogy"],
                ["Superposition needs careful explanation."],
                ["Concept diagram"],
                ["Avoid fake lab evidence"],
                IllustrationStrictnessLevel.Educational,
                timestamp.AddMinutes(1));
            var target = IllustrationTarget.Create(
                brief.Id,
                "Superposition concept diagram",
                "Analogy",
                IllustrationPurpose.ConceptDiagram,
                ["two-state classroom analogy"],
                ["photorealistic lab equipment"],
                ["The source explains superposition as a classroom analogy."],
                ImageTypePresetCatalog.ConceptDiagram,
                ReviewRubricTemplateCatalog.EducationalAccuracy,
                ImageTextPolicy.DeterministicPostRender,
                ["Every visual claim must map back to the supplied text."],
                timestamp.AddMinutes(2));
            var plan = IllustrationPlan.Create(
                    project.Id,
                    brief.Id,
                    "Create a source-grounded classroom concept diagram.",
                    [target],
                    ["Covers the analogy section."],
                    ["Do not invent experimental evidence."],
                    timestamp.AddMinutes(3))
                .ApproveTarget(target.Id, timestamp.AddMinutes(4));

            project.AddDocumentBrief(brief, timestamp.AddMinutes(5));
            project.AddIllustrationPlan(plan, timestamp.AddMinutes(6));

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                db.Projects.Add(project);
                await db.SaveChangesAsync();
            }

            await using (var db = new AppDbContext(options))
            {
                var loaded = await db.Projects
                    .Include(project => project.DocumentBriefs)
                    .Include(project => project.IllustrationPlans)
                    .SingleAsync();

                var loadedBrief = Assert.Single(loaded.DocumentBriefs);
                var loadedPlan = Assert.Single(loaded.IllustrationPlans);
                var loadedTarget = Assert.Single(loadedPlan.Targets);

                Assert.Equal(project.Id, loadedBrief.ProjectId);
                Assert.Equal("lesson.md", loadedBrief.SourceDisplayName);
                Assert.Equal(["Introduction", "Analogy"], loadedBrief.Sections);
                Assert.Equal(project.Id, loadedPlan.ProjectId);
                Assert.Equal(loadedBrief.Id, loadedPlan.DocumentBriefId);
                Assert.Equal("Create a source-grounded classroom concept diagram.", loadedPlan.Summary);
                Assert.Equal(IllustrationTargetApprovalState.Approved, loadedTarget.ApprovalState);
                Assert.Equal(IllustrationPurpose.ConceptDiagram, loadedTarget.Purpose);
                Assert.Equal(ImageTextPolicy.DeterministicPostRender, loadedTarget.TextPolicy);
                Assert.Equal(["two-state classroom analogy"], loadedTarget.MustShow);
                Assert.Equal(["The source explains superposition as a classroom analogy."], loadedTarget.SourceEvidence);
                Assert.Equal(timestamp.AddMinutes(6), loaded.UpdatedAt);
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
                    .ThenInclude(series => series.Items)
                    .ThenInclude(item => item.PromptVersions)
                    .SingleAsync();

                var loadedSeries = Assert.Single(loaded.Series);
                var loadedItem = Assert.Single(loadedSeries.Items);
                var loadedPrompt = Assert.Single(loadedItem.PromptVersions);

                Assert.Equal("Sample project", loaded.Name);
                Assert.Equal("Physics posters", loadedSeries.Title);
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
