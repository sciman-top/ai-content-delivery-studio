using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Core.Styles;
using ImageSeriesStudio.Infrastructure.Fakes;
using ImageSeriesStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImageSeriesStudio.Tests;

public sealed class PersistenceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EfProjectRepository_DocumentIllustrationWorkflow_PersistsProviderPlan(bool approveAllTargets)
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "project.sqlite");
        Directory.CreateDirectory(databaseDirectory);

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;

            var timestamp = new DateTimeOffset(2026, 6, 2, 11, 0, 0, TimeSpan.Zero);
            DocumentIllustrationWorkflowResult result;
            Guid projectId;

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();

                var repository = new EfProjectRepository(db);
                var service = new ProjectApplicationService(repository, new FakeTextPlanningProvider());
                var project = await service.CreateProjectAsync(
                    "EF document illustration workflow",
                    timestamp,
                    CancellationToken.None);

                projectId = project.Id;
                result = await service.CreateDocumentIllustrationPlanWithProviderAsync(
                    project.Id,
                    CreateDocumentIllustrationRequest(),
                    approveAllTargets,
                    timestamp.AddMinutes(1),
                    CancellationToken.None);
            }

            await using (var db = new AppDbContext(options))
            {
                var repository = new EfProjectRepository(db);
                var loaded = await repository.LoadAsync(projectId, CancellationToken.None);

                var brief = Assert.Single(loaded!.DocumentBriefs);
                var plan = Assert.Single(loaded.IllustrationPlans);

                Assert.Equal(result.DocumentBriefId, brief.Id);
                Assert.Equal(result.IllustrationPlanId, plan.Id);
                Assert.Equal(projectId, brief.ProjectId);
                Assert.Equal(projectId, plan.ProjectId);
                Assert.Equal(brief.Id, plan.DocumentBriefId);
                Assert.NotEmpty(plan.Targets);

                if (approveAllTargets)
                {
                    Assert.True(result.SeriesId.HasValue);
                    Assert.Equal(plan.Targets.Count, result.ApprovedTargetCount);
                    Assert.Single(loaded.Series);
                }
                else
                {
                    Assert.Null(result.SeriesId);
                    Assert.Equal(0, result.ApprovedTargetCount);
                    Assert.Empty(loaded.Series);
                }
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

    private static DocumentIllustrationPlanningRequest CreateDocumentIllustrationRequest()
    {
        return new DocumentIllustrationPlanningRequest(
            "Quantum teaching note",
            "Teachers need an intuitive explanation of superposition.",
            "teachers",
            DocumentFamily.Educational,
            IllustrationStrictnessLevel.Educational,
            ["Introduction", "Classroom analogy"],
            ["Superposition needs a visual analogy."],
            ["avoid fake lab data"]);
    }
}
