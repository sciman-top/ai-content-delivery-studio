using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Application.RepairRouting;
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
    [Fact]
    public async Task EfProjectRepository_PersistsRoutedRepairPatches()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "project.sqlite");
        Directory.CreateDirectory(databaseDirectory);

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;

            var timestamp = new DateTimeOffset(2026, 6, 3, 21, 30, 0, TimeSpan.Zero);
            var project = ImageProject.Create("Repair patch EF project", timestamp);
            var repairPlan = new RepairPlan(
                Guid.NewGuid(),
                Guid.NewGuid(),
                RepairSeverity.Major,
                [
                    RepairPlanStep.Create(
                        1,
                        ReviewOutcomeTargetLayer.Blueprint,
                        RepairSeverity.Major,
                        ["Series style drift."],
                        ["Add a consistency rule to the promoted blueprint."],
                        requiresOperator: false),
                ],
                timestamp.AddMinutes(1));
            var patch = RoutedRepairPatch.FromRepairPlan(project.Id, repairPlan, timestamp.AddMinutes(2));
            project.AddRoutedRepairPatch(patch, timestamp.AddMinutes(3));

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                var repository = new EfProjectRepository(db);
                await repository.SaveAsync(project, CancellationToken.None);
            }

            await using (var db = new AppDbContext(options))
            {
                var repository = new EfProjectRepository(db);
                var loaded = await repository.LoadAsync(project.Id, CancellationToken.None);
                var loadedPatch = Assert.Single(loaded!.RoutedRepairPatches);
                var loadedItem = Assert.Single(loadedPatch.Items);

                Assert.Equal(project.Id, loadedPatch.ProjectId);
                Assert.Equal(repairPlan.Id, loadedPatch.RepairPlanId);
                Assert.Equal(ReviewOutcomeTargetLayer.Blueprint, loadedItem.TargetLayer);
                Assert.Equal("Add a consistency rule to the promoted blueprint.", loadedItem.ProposedChanges.Single());
                Assert.Equal(timestamp.AddMinutes(3), loaded.UpdatedAt);
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
    public async Task EfProjectRepository_PersistsAppliedBriefAndBlueprintRepairNotes()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "project.sqlite");
        Directory.CreateDirectory(databaseDirectory);

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;

            var timestamp = new DateTimeOffset(2026, 6, 3, 22, 15, 0, TimeSpan.Zero);
            var project = ImageProject.Create("Repair patch note EF project", timestamp);
            var series = project.AddSeries("Series", "Brief", timestamp.AddMinutes(1));
            var brief = series.AddCreativeBrief(
                "Clarify the route",
                "designers",
                ImageTextPolicy.Hybrid,
                "clean editorial route",
                ["clear visual hierarchy"],
                ["unreadable labels"],
                timestamp.AddMinutes(2));
            var blueprint = DesignBlueprint.Create(
                "article-illustration",
                "Article illustration",
                "article_illustration_pack",
                "Keep the route grounded in the brief.",
                "Best for short article-backed visual planning.",
                2,
                4,
                supportsPanelSequence: false,
                ImageTextPolicy.Hybrid,
                ReviewRubricTemplateCatalog.GeneralImage,
                ["Preserve the core claim across the set."],
                ["Vary composition rather than premise."],
                ["Avoid expanding beyond the article brief."],
                timestamp.AddMinutes(2));
            brief.ReplaceBlueprints([blueprint], timestamp.AddMinutes(3));
            brief.PromoteBlueprint(blueprint.Id, timestamp.AddMinutes(4));

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                var repository = new EfProjectRepository(db);
                await repository.SaveAsync(project, CancellationToken.None);
            }

            RoutedRepairPatch patch;
            await using (var db = new AppDbContext(options))
            {
                var repository = new EfProjectRepository(db);
                var repairService = new ReviewRepairApplicationService(repository);
                var repairPlan = new RepairPlan(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    RepairSeverity.Major,
                    [
                        RepairPlanStep.Create(
                            1,
                            ReviewOutcomeTargetLayer.Brief,
                            RepairSeverity.Major,
                            ["Missing a clearer audience constraint."],
                            ["Tighten the brief before regenerating."],
                            requiresOperator: false),
                        RepairPlanStep.Create(
                            2,
                            ReviewOutcomeTargetLayer.Blueprint,
                            RepairSeverity.Major,
                            ["The promoted route needs stronger sequence rules."],
                            ["Update the promoted blueprint with better panel consistency rules."],
                            requiresOperator: false),
                    ],
                    timestamp.AddMinutes(5));

                patch = await repairService.CreateRoutedRepairPatchAsync(
                    new RoutedRepairPatchRequest(project.Id, repairPlan, timestamp.AddMinutes(6)),
                    CancellationToken.None);

                await repairService.ApplyRoutedRepairPatchAsync(
                    new RoutedRepairPatchApplicationRequest(
                        project.Id,
                        brief.Id,
                        patch.Id,
                        timestamp.AddMinutes(7)),
                    CancellationToken.None);

                var tracked = await repository.LoadAsync(project.Id, CancellationToken.None);
                var trackedBrief = Assert.Single(tracked!.Series.Single().CreativeBriefs);
                Assert.NotEmpty(trackedBrief.RepairNotesJson);
                Assert.Single(trackedBrief.RepairNotes);
                var trackedBlueprint = Assert.Single(trackedBrief.DesignBlueprints);
                Assert.NotEmpty(trackedBlueprint.RepairNotesJson);
                Assert.Single(trackedBlueprint.RepairNotes);
            }

            await using (var db = new AppDbContext(options))
            {
                var repository = new EfProjectRepository(db);
                var loaded = await repository.LoadAsync(project.Id, CancellationToken.None);
                var loadedBrief = Assert.Single(loaded!.Series.Single().CreativeBriefs);
                var loadedBlueprint = Assert.Single(loadedBrief.DesignBlueprints);

                Assert.Equal(patch.Id, Assert.Single(loadedBrief.RepairNotes).RepairPatchId);
                Assert.Equal(patch.Id, Assert.Single(loadedBlueprint.RepairNotes).RepairPatchId);
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
    public async Task AppDbContext_PersistsReviewRubricsAndReviewResults()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "project.sqlite");
        Directory.CreateDirectory(databaseDirectory);

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;

            var timestamp = new DateTimeOffset(2026, 6, 4, 11, 30, 0, TimeSpan.Zero);
            var project = ImageProject.Create("Review persistence project", timestamp);
            var profile = project.AddProviderProfile("Fake provider", ProviderKind.Fake, timestamp.AddMinutes(1));
            var series = project.AddSeries("Review series", "Review persistence coverage.", timestamp.AddMinutes(2));
            var item = series.AddItem("Opening", "Opening review candidate.", timestamp.AddMinutes(3));
            var prompt = item.AddPromptVersion(
                "A reviewable candidate image.",
                new GenerationSettings(1024, 1024, "standard", "png", 7),
                profile.Id,
                timestamp.AddMinutes(4));
            var task = new GenerationTask(
                Guid.NewGuid(),
                item.Id,
                prompt.Id,
                profile.Id,
                GenerationTaskStatus.Succeeded,
                attemptCount: 1,
                maxRetries: 2,
                timestamp.AddMinutes(5),
                timestamp.AddMinutes(6));
            var candidate = new CandidateImage(
                Guid.NewGuid(),
                item.Id,
                prompt.Id,
                task.Id,
                profile.Id,
                CandidateImageStatus.ReviewPending,
                "outputs/review/candidate.png",
                "outputs/review/candidate.json",
                timestamp.AddMinutes(7));
            var rubric = new ReviewRubric(
                Guid.NewGuid(),
                project.Id,
                "Delivery review",
                [
                    new ReviewRubricDimension("match", "Image should match the prompt.", 3),
                    new ReviewRubricDimension("text", "Text should be deterministic.", 2),
                ],
                timestamp.AddMinutes(8));
            var review = new ReviewResult(
                Guid.NewGuid(),
                candidate.Id,
                ReviewDecision.Fail,
                new Dictionary<string, int>
                {
                    ["match"] = 4,
                    ["text"] = 1,
                },
                ["text-rendering-risk"],
                "Text needs deterministic composition.",
                "Use post-render labels.",
                humanApproved: false,
                timestamp.AddMinutes(9),
                finalReviewer: "Teacher",
                finalApprovalNotes: "Needs deterministic text before approval.",
                finalApprovalDecidedAt: timestamp.AddMinutes(9));
            var deliveryPackage = new DeliveryPackage(
                Guid.NewGuid(),
                project.Id,
                version: 3,
                "outputs/delivery/package",
                "outputs/delivery/manifest.json",
                "outputs/delivery/manifest.csv",
                timestamp.AddMinutes(10));

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                db.Projects.Add(project);
                db.GenerationTasks.Add(task);
                db.CandidateImages.Add(candidate);
                db.ReviewRubrics.Add(rubric);
                db.ReviewResults.Add(review);
                db.DeliveryPackages.Add(deliveryPackage);
                await db.SaveChangesAsync();
            }

            await using (var db = new AppDbContext(options))
            {
                var loadedTask = await db.GenerationTasks.SingleAsync();
                var loadedCandidate = await db.CandidateImages.SingleAsync();
                var loadedRubric = await db.ReviewRubrics.SingleAsync();
                var loadedReview = await db.ReviewResults.SingleAsync();
                var loadedDeliveryPackage = await db.DeliveryPackages.SingleAsync();

                Assert.Equal(prompt.Id, loadedTask.PromptVersionId);
                Assert.Equal(GenerationTaskStatus.Succeeded, loadedTask.Status);
                Assert.Equal(1, loadedTask.AttemptCount);
                Assert.Equal(2, loadedTask.MaxRetries);
                Assert.Equal(task.Id, loadedCandidate.GenerationTaskId);
                Assert.Equal(CandidateImageStatus.ReviewPending, loadedCandidate.Status);
                Assert.Equal("outputs/review/candidate.png", loadedCandidate.AssetPath);
                Assert.Equal("outputs/review/candidate.json", loadedCandidate.MetadataPath);
                Assert.Equal("Delivery review", loadedRubric.Name);
                Assert.Equal(2, loadedRubric.Dimensions.Count);
                Assert.Equal("match", loadedRubric.Dimensions[0].Name);
                Assert.Equal("Image should match the prompt.", loadedRubric.Dimensions[0].Requirement);
                Assert.Equal(3, loadedRubric.Dimensions[0].Weight);
                Assert.Equal(candidate.Id, loadedReview.CandidateImageId);
                Assert.Equal(ReviewDecision.Fail, loadedReview.Decision);
                Assert.Equal(4, loadedReview.Scores["match"]);
                Assert.Equal(1, loadedReview.Scores["text"]);
                Assert.Equal("text-rendering-risk", Assert.Single(loadedReview.HardFailures));
                Assert.Equal("Use post-render labels.", loadedReview.SuggestedFix);
                Assert.False(loadedReview.HumanApproved);
                Assert.Equal("Teacher", loadedReview.FinalReviewer);
                Assert.Equal("Needs deterministic text before approval.", loadedReview.FinalApprovalNotes);
                Assert.Equal(timestamp.AddMinutes(9), loadedReview.FinalApprovalDecidedAt);
                Assert.Equal(project.Id, loadedDeliveryPackage.ProjectId);
                Assert.Equal(3, loadedDeliveryPackage.Version);
                Assert.Equal("outputs/delivery/package", loadedDeliveryPackage.OutputPath);
                Assert.Equal("outputs/delivery/manifest.json", loadedDeliveryPackage.ManifestJsonPath);
                Assert.Equal("outputs/delivery/manifest.csv", loadedDeliveryPackage.ManifestCsvPath);
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
            DesignBlueprint[] blueprints =
            [
                DesignBlueprint.Create(
                    "poster-series",
                    "Poster series",
                    "poster_series",
                    "Use a stable teaching-poster route across the set.",
                    "Best for text-heavy educational image series.",
                    3,
                    6,
                    supportsPanelSequence: false,
                    ImageTextPolicy.DeterministicPostRender,
                    ReviewRubricTemplateCatalog.TextHeavyPoster,
                    ["keep the same visual grammar"],
                    ["change the key concept per item"],
                    ["reserve deterministic text zones"],
                    timestamp.AddMinutes(2)),
                DesignBlueprint.Create(
                    "concept-sequence",
                    "Concept explainer sequence",
                    "concept_explainer_sequence",
                    "Use a lighter explanatory route with clearer concept progression.",
                    "Best for article-backed or slide-backed concept teaching.",
                    2,
                    5,
                    supportsPanelSequence: false,
                    ImageTextPolicy.DeterministicPostRender,
                    ReviewRubricTemplateCatalog.EducationalAccuracy,
                    ["preserve the same concept vocabulary"],
                    ["let each frame introduce one new relation"],
                    ["avoid text baked into the model image"],
                    timestamp.AddMinutes(2)),
            ];
            brief.ReplaceBlueprints(blueprints, timestamp.AddMinutes(2));
            brief.PromoteBlueprint(blueprints[0].Id, timestamp.AddMinutes(2));
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
            var item = series.AddItem("Cover", "Opening image", SeriesItemKind.Cover, timestamp.AddMinutes(3));
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
                Assert.Equal(2, loadedBrief.DesignBlueprints.Count);
                Assert.Equal(
                    "poster-series",
                    loadedBrief.DesignBlueprints.Single(blueprint => blueprint.Id == loadedBrief.PromotedBlueprintId).Key);
                var loadedDirection = Assert.Single(loadedBrief.PromptDirections);
                Assert.Equal("conservative", loadedDirection.Key);
                Assert.NotNull(loadedDirection.Recommendation);
                Assert.Equal(ImageTypePresetCatalog.EducationalPoster, loadedDirection.Recommendation.ImageTypePresetId);
                Assert.Equal(1024, loadedDirection.Recommendation.Width);
                Assert.Equal(1280, loadedDirection.Recommendation.Height);
                Assert.Equal("model-rendered small text is risky", Assert.Single(loadedDirection.Recommendation.CapabilityWarnings));
                Assert.Equal(SeriesItemStatus.Ready, loadedItem.Status);
                Assert.Equal(SeriesItemKind.Cover, loadedItem.Kind);
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
