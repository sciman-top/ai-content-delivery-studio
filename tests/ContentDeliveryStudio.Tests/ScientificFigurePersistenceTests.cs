using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.Fakes;
using ContentDeliveryStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificFigurePersistenceTests
{
    [Fact]
    public async Task InitializeAsync_UpgradesLegacyDatabaseAndKeepsProjectReadable()
    {
        await WithDatabaseAsync(async options =>
        {
            var project = ImageProject.Create(
                "Legacy project",
                DateTimeOffset.Parse("2026-07-26T08:00:00Z"));

            await using (var legacy = new AppDbContext(options))
            {
                await legacy.Database.EnsureCreatedAsync();
                await legacy.Database.ExecuteSqlRawAsync(
                    """DROP TABLE "ScientificFigureWorkflows";""");
                legacy.Projects.Add(project);
                await legacy.SaveChangesAsync();
            }

            await using (var upgrade = new AppDbContext(options))
            {
                await AppDatabaseInitializer.InitializeAsync(upgrade, CancellationToken.None);
            }

            await using var verify = new AppDbContext(options);
            var loaded = await new EfProjectRepository(verify)
                .LoadAsync(project.Id, CancellationToken.None);
            var scientificRecords = await new EfScientificFigureWorkflowRepository(verify)
                .ListByProjectAsync(project.Id, CancellationToken.None);

            Assert.Equal(project.Name, loaded!.Name);
            Assert.Empty(scientificRecords);
        });
    }

    [Fact]
    public async Task SaveAsync_ThrowsConcurrencyConflictWhenAggregateWasModifiedAfterLoad()
    {
        await WithDatabaseAsync(async options =>
        {
            var project = ImageProject.Create(
                "Concurrent project",
                DateTimeOffset.Parse("2026-08-23T08:00:00Z"));

            await using (var seed = new AppDbContext(options))
            {
                await AppDatabaseInitializer.InitializeAsync(seed, CancellationToken.None);
                seed.Projects.Add(project);
                await seed.SaveChangesAsync();
            }

            await using var firstContext = new AppDbContext(options);
            await using var secondContext = new AppDbContext(options);
            var first = await new EfProjectRepository(firstContext)
                .LoadAsync(project.Id, CancellationToken.None);
            var second = await new EfProjectRepository(secondContext)
                .LoadAsync(project.Id, CancellationToken.None);
            Assert.NotNull(first);
            Assert.NotNull(second);

            first!.AddSeries("First writer series", string.Empty, DateTimeOffset.Parse("2026-08-23T08:05:00Z"));
            await new EfProjectRepository(firstContext).SaveAsync(first, CancellationToken.None);

            second!.AddSeries("Second writer series", string.Empty, DateTimeOffset.Parse("2026-08-23T08:06:00Z"));
            var conflict = await Assert.ThrowsAsync<ProjectConcurrencyConflictException>(() =>
                new EfProjectRepository(secondContext).SaveAsync(second, CancellationToken.None));

            Assert.Equal(project.Id, conflict.ProjectId);
            await using var verify = new AppDbContext(options);
            var persisted = await new EfProjectRepository(verify)
                .LoadAsync(project.Id, CancellationToken.None);
            Assert.NotNull(persisted);
            var seriesTitles = persisted!.Series.Select(series => series.Title).ToArray();
            Assert.Contains("First writer series", seriesTitles);
            Assert.DoesNotContain("Second writer series", seriesTitles);
        });
    }

    [Fact]
    public async Task ExecuteQueue_TerminalSaveConflictReloadsFromDatabaseAndPersistsTerminalState()
    {
        await WithDatabaseAsync(async options =>
        {
            var timestamp = DateTimeOffset.Parse("2026-08-23T08:00:00Z");
            var project = ImageProject.Create("Queue concurrency project", timestamp);
            var series = project.AddSeries("Series", "Queue", timestamp.AddMinutes(1));
            var profile = project.AddProviderProfile("Fake provider", ProviderKind.Fake, timestamp.AddMinutes(2));
            var item = series.AddItem("Item 1", "Visual 1", timestamp.AddMinutes(3));
            item.AddPromptVersion(
                "Create visual 1.",
                new GenerationSettings(1024, 1024, "standard", "png"),
                profile.Id,
                timestamp.AddMinutes(4));

            await using (var seed = new AppDbContext(options))
            {
                await AppDatabaseInitializer.InitializeAsync(seed, CancellationToken.None);
                seed.Projects.Add(project);
                await seed.SaveChangesAsync();
            }

            // The provider races the executor's terminal save: by the time the
            // queue persists the terminal result, another context has already
            // advanced the aggregate's concurrency version.
            var concurrentWriteDone = false;
            var provider = new ConcurrentWritingImageGenerationProvider(async () =>
            {
                if (concurrentWriteDone)
                {
                    return;
                }

                concurrentWriteDone = true;
                await using var concurrentContext = new AppDbContext(options);
                var concurrentRepository = new EfProjectRepository(concurrentContext);
                var fresh = await concurrentRepository.LoadAsync(project.Id, CancellationToken.None);
                fresh!.AddSeries("concurrent write", string.Empty, timestamp.AddMinutes(5));
                await concurrentRepository.SaveAsync(fresh, CancellationToken.None);
            });

            var outputDirectory = Path.Combine(
                Path.GetTempPath(),
                "ContentDeliveryStudio.Tests",
                Guid.NewGuid().ToString("N"));
            try
            {
                // The executor reuses one context across the conflict and its
                // retry; the reload after the conflict must observe the
                // database rows (not stale tracked instances).
                await using var executionContext = new AppDbContext(options);
                var repository = new EfProjectRepository(executionContext);
                var service = new GenerationWorkflowApplicationService(repository, provider, imageEditProvider: null);

                var run = await service.RunGenerationQueueAsync(project.Id, outputDirectory, CancellationToken.None);

                var result = Assert.Single(run.Tasks);
                Assert.Equal(GenerationTaskStatus.Succeeded, result.Status);

                await using var verify = new AppDbContext(options);
                var persisted = await new EfProjectRepository(verify)
                    .LoadAsync(project.Id, CancellationToken.None);
                var persistedItem = persisted!.Series.Single(value => value.Title == "Series").Items.Single();
                var persistedTask = Assert.Single(persistedItem.GenerationTasks);
                Assert.Equal(GenerationTaskStatus.Succeeded, persistedTask.Status);
                var candidate = Assert.Single(persistedItem.CandidateImages);
                Assert.True(File.Exists(candidate.AssetPath));
                Assert.Contains("concurrent write", persisted.Series.Select(value => value.Title));
                // Version sequence: prepare 0→1, start 1→2, concurrent write
                // 2→3, and the terminal save only lands after the conflict
                // retry 3→4. A conflict-free run would leave the version at 3,
                // so 4 proves the executor hit and recovered from the conflict.
                Assert.Equal(4, persisted.ConcurrencyVersion);
            }
            finally
            {
                if (Directory.Exists(outputDirectory))
                {
                    Directory.Delete(outputDirectory, recursive: true);
                }
            }
        });
    }

    private sealed class ConcurrentWritingImageGenerationProvider : IImageGenerationProvider
    {
        private readonly FakeImageGenerationProvider _inner = new();
        private readonly Func<Task> _onGenerate;

        public ConcurrentWritingImageGenerationProvider(Func<Task> onGenerate)
        {
            _onGenerate = onGenerate;
        }

        public IProviderCapabilities Capabilities => _inner.Capabilities;

        public async Task<ImageGenerationResult> GenerateImageAsync(
            ImageGenerationRequest request,
            CancellationToken cancellationToken)
        {
            await _onGenerate();
            return await _inner.GenerateImageAsync(request, cancellationToken);
        }
    }

    [Fact]
    public async Task SaveReload_PreservesIdentifiersEvidenceGate1AndDownstreamApprovals()
    {
        await WithDatabaseAsync(async options =>
        {
            var createdAt = DateTimeOffset.Parse("2026-07-26T11:00:00Z");
            var project = ImageProject.Create("Scientific project", createdAt);
            var aggregate = CreateAggregate(
                includeDownstreamApprovals: true,
                project.Id);

            await using (var save = new AppDbContext(options))
            {
                await AppDatabaseInitializer.InitializeAsync(save, CancellationToken.None);
                save.Projects.Add(project);
                await save.SaveChangesAsync();
                await new EfScientificFigureWorkflowRepository(save)
                    .SaveAsync(aggregate, CancellationToken.None);
            }

            await using var reload = new AppDbContext(options);
            var loaded = await new EfScientificFigureWorkflowRepository(reload)
                .LoadAsync(aggregate.Id, CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal(aggregate.Id, loaded.Id);
            Assert.Equal(aggregate.ProjectId, loaded.ProjectId);
            Assert.Equal(aggregate.Extraction.SourceAssetId, loaded.Extraction.SourceAssetId);
            Assert.Equal(aggregate.Extraction.SourceSha256, loaded.Extraction.SourceSha256);
            Assert.Equal(aggregate.Understanding.UnderstandingId, loaded.Understanding.UnderstandingId);
            Assert.Equal(aggregate.Understanding.Version, loaded.Understanding.Version);
            Assert.Equal(
                aggregate.Understanding.Claims[0].EvidenceLinks[0].SourceBlockId,
                loaded.Understanding.Claims[0].EvidenceLinks[0].SourceBlockId);
            Assert.Equal(
                aggregate.Workflow.Specification.SpecificationId,
                loaded.Workflow.Specification.SpecificationId);
            Assert.Equal(
                aggregate.Workflow.Specification.Version,
                loaded.Workflow.Specification.Version);
            Assert.Equal(ScientificFigureWorkflowState.ReviewPassed, loaded.Workflow.State);
            Assert.Equal(
                aggregate.Workflow.Gate1Approval,
                loaded.Workflow.Gate1Approval);
            Assert.Equal(
                aggregate.Workflow.DownstreamApprovals,
                loaded.Workflow.DownstreamApprovals);
        });
    }

    [Fact]
    public async Task SaveReload_PreservesScientificRevisionInvalidationState()
    {
        await WithDatabaseAsync(async options =>
        {
            var createdAt = DateTimeOffset.Parse("2026-07-26T11:00:00Z");
            var project = ImageProject.Create("Scientific project", createdAt);
            var approved = CreateAggregate(
                includeDownstreamApprovals: true,
                project.Id);
            var claim = Assert.Single(approved.Understanding.Claims);
            var evidence = Assert.Single(claim.SupportingEvidence);
            var revisedWorkflow = approved.Workflow.ReviseScientificContent(
                approved.Understanding,
                "A revised scientific message with the stated condition.",
                [ScientificFigureTestFixture.RequiredElement(claim, evidence)],
                [],
                []);
            var revised = ScientificFigureWorkflowAggregate.Create(
                approved.Id,
                approved.ProjectId,
                approved.Extraction,
                approved.Understanding,
                revisedWorkflow,
                approved.CreatedAt,
                approved.UpdatedAt.AddMinutes(10));

            await using (var save = new AppDbContext(options))
            {
                await AppDatabaseInitializer.InitializeAsync(save, CancellationToken.None);
                save.Projects.Add(project);
                await save.SaveChangesAsync();
                var repository = new EfScientificFigureWorkflowRepository(save);
                await repository.SaveAsync(approved, CancellationToken.None);
                await repository.SaveAsync(revised, CancellationToken.None);
            }

            await using var reload = new AppDbContext(options);
            var loaded = await new EfScientificFigureWorkflowRepository(reload)
                .LoadAsync(revised.Id, CancellationToken.None);

            Assert.Equal(2, loaded!.Workflow.Specification.Version);
            Assert.Equal(ScientificFigureWorkflowState.FigureSpecDraft, loaded.Workflow.State);
            Assert.Null(loaded.Workflow.Gate1Approval);
            Assert.Empty(loaded.Workflow.DownstreamApprovals);
        });
    }

    [Fact]
    public async Task LoadAsync_RejectsUnsupportedPayloadSchema()
    {
        await WithDatabaseAsync(async options =>
        {
            var createdAt = DateTimeOffset.Parse("2026-07-26T11:00:00Z");
            var project = ImageProject.Create("Scientific project", createdAt);
            var aggregate = CreateAggregate(
                includeDownstreamApprovals: false,
                project.Id);

            await using (var save = new AppDbContext(options))
            {
                await AppDatabaseInitializer.InitializeAsync(save, CancellationToken.None);
                save.Projects.Add(project);
                await save.SaveChangesAsync();
                await new EfScientificFigureWorkflowRepository(save)
                    .SaveAsync(aggregate, CancellationToken.None);
                await save.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                    UPDATE "ScientificFigureWorkflows"
                    SET "PayloadSchemaVersion" = {"scientific-figure-workflow.v999"}
                    WHERE "Id" = {aggregate.Id};
                    """);
            }

            await using var reload = new AppDbContext(options);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new EfScientificFigureWorkflowRepository(reload)
                    .LoadAsync(aggregate.Id, CancellationToken.None));

            Assert.Contains("Unsupported scientific figure workflow schema", error.Message);
        });
    }

    [Fact]
    public async Task SaveReload_DistinguishesEvidenceDifferingOnlyByConfidence()
    {
        await WithDatabaseAsync(async options =>
        {
            var createdAt = DateTimeOffset.Parse("2026-07-26T11:00:00Z");
            var project = ImageProject.Create("Scientific project", createdAt);
            var aggregate = CreateAggregateWithConfidenceDistinctEvidence(
                project.Id,
                provenanceConfidence: 0.8);

            await using (var save = new AppDbContext(options))
            {
                await AppDatabaseInitializer.InitializeAsync(save, CancellationToken.None);
                save.Projects.Add(project);
                await save.SaveChangesAsync();
                await new EfScientificFigureWorkflowRepository(save)
                    .SaveAsync(aggregate, CancellationToken.None);
            }

            await using var reload = new AppDbContext(options);
            var loaded = await new EfScientificFigureWorkflowRepository(reload)
                .LoadAsync(aggregate.Id, CancellationToken.None);

            Assert.NotNull(loaded);
            var claim = Assert.Single(loaded.Understanding.Claims);
            Assert.Equal(2, claim.EvidenceLinks.Count);
            var restoredProvenance = loaded.Workflow.Specification.Elements
                .Single(element => element.ElementId == "element-force")
                .Provenance;
            Assert.NotNull(restoredProvenance);
            Assert.Equal(ScientificProvenanceKind.ClaimEvidence, restoredProvenance.Kind);
            Assert.NotNull(restoredProvenance.Evidence);
            Assert.Equal(0.8, restoredProvenance.Evidence!.Confidence);
        });
    }

    [Fact]
    public void ScientificClaimCreate_RejectsFullyDuplicateEvidenceLinks()
    {
        var block = ScientificSourceBlock.Create(
            "block-dynamics",
            ScientificSourceBlockKind.Paragraph,
            ScientificSourceLocation.Create(
                pageNumber: 4,
                section: "2.1 Dynamics",
                ScientificBoundingRegion.Create(72, 144, 320, 48),
                ScientificCharacterRange.Create(20, 68)),
            "Net force causes acceleration for constant mass.",
            isRequired: true,
            ScientificRecoveryStatus.NotRequired);
        var extraction = ScientificDocumentExtraction.Create(
            Guid.NewGuid(),
            "sha256:a3f75487411ef13d0270c65fc801ceff2b28e6b339afed9b407fe477f7e8453e",
            ScientificExtractorIdentity.Create("fixture-extractor", "1.0"),
            ScientificExtractionQuality.Create(
                isScanned: false,
                ocrApplied: false,
                ScientificReadingOrderStatus.Reliable,
                ScientificRequiredContentStatus.Complete),
            [block],
            []);
        var evidence = ClaimEvidenceLink.Create(
            extraction,
            block,
            "Net force causes acceleration",
            ClaimEvidenceRole.Support,
            confidence: 0.99,
            EvidenceValidationState.Validated);
        var duplicate = ClaimEvidenceLink.Create(
            extraction,
            block,
            evidence.QuotedText,
            evidence.Role,
            evidence.Confidence,
            evidence.ValidationState);

        var error = Assert.Throws<ArgumentException>(() =>
            ScientificClaim.Create(
                "claim-force-acceleration",
                ScientificClaimCategory.CausalRelation,
                "Net force causes acceleration when mass is constant.",
                "Net force causes acceleration for constant mass.",
                confidence: 0.98,
                ScientificClaimStatus.Accepted,
                [evidence, duplicate]));

        Assert.Contains("duplicate evidence links", error.Message);
    }

    private static ScientificFigureWorkflowAggregate CreateAggregateWithConfidenceDistinctEvidence(
        Guid projectId,
        double provenanceConfidence)
    {
        var block = ScientificSourceBlock.Create(
            "block-dynamics",
            ScientificSourceBlockKind.Paragraph,
            ScientificSourceLocation.Create(
                pageNumber: 4,
                section: "2.1 Dynamics",
                ScientificBoundingRegion.Create(72, 144, 320, 48),
                ScientificCharacterRange.Create(20, 68)),
            "Net force causes acceleration for constant mass.",
            isRequired: true,
            ScientificRecoveryStatus.NotRequired);
        var extraction = ScientificDocumentExtraction.Create(
            Guid.NewGuid(),
            "sha256:a3f75487411ef13d0270c65fc801ceff2b28e6b339afed9b407fe477f7e8453e",
            ScientificExtractorIdentity.Create("fixture-extractor", "1.0"),
            ScientificExtractionQuality.Create(
                isScanned: false,
                ocrApplied: false,
                ScientificReadingOrderStatus.Reliable,
                ScientificRequiredContentStatus.Complete),
            [block],
            []);
        var highConfidence = ClaimEvidenceLink.Create(
            extraction,
            block,
            "Net force causes acceleration",
            ClaimEvidenceRole.Support,
            confidence: 0.99,
            EvidenceValidationState.Validated);
        var lowConfidence = ClaimEvidenceLink.Create(
            extraction,
            block,
            "Net force causes acceleration",
            ClaimEvidenceRole.Support,
            confidence: provenanceConfidence,
            EvidenceValidationState.Validated);
        var claim = ScientificClaim.Create(
            "claim-force-acceleration",
            ScientificClaimCategory.CausalRelation,
            "Net force causes acceleration when mass is constant.",
            "Net force causes acceleration for constant mass.",
            confidence: 0.98,
            ScientificClaimStatus.Accepted,
            [highConfidence, lowConfidence]);
        var understanding = ScientificDocumentUnderstanding.Create(
            Guid.NewGuid(),
            extraction,
            "Explain Newton's second law.",
            version: 1,
            [],
            [claim],
            [],
            [ScientificCoverageRequirement.Create(
                "coverage-main-causal-claim",
                "The main causal claim is covered.",
                isRequired: true,
                ScientificCoverageStatus.Complete,
                [claim.ClaimId])]);
        var referencedEvidence = provenanceConfidence == highConfidence.Confidence
            ? highConfidence
            : lowConfidence;
        var spec = ScientificFigureTestFixture.CreateSpec(
            understanding,
            [ScientificFigureTestFixture.RequiredElement(claim, referencedEvidence)],
            [],
            []);
        var reviewedAt = DateTimeOffset.Parse("2026-07-26T12:00:00Z");
        var workflow = ScientificFigureWorkflow
            .Create(spec)
            .ApproveGate1("scientific-reviewer", "Evidence and specification verified.", reviewedAt);

        return ScientificFigureWorkflowAggregate.Create(
            Guid.NewGuid(),
            projectId,
            extraction,
            understanding,
            workflow,
            reviewedAt.AddHours(-1),
            reviewedAt);
    }

    private static ScientificFigureWorkflowAggregate CreateAggregate(
        bool includeDownstreamApprovals,
        Guid projectId)
    {
        var location = ScientificSourceLocation.Create(
            pageNumber: 4,
            section: "2.1 Dynamics",
            ScientificBoundingRegion.Create(72, 144, 320, 48),
            ScientificCharacterRange.Create(20, 68));
        var block = ScientificSourceBlock.Create(
            "block-dynamics",
            ScientificSourceBlockKind.Paragraph,
            location,
            "Net force causes acceleration for constant mass.",
            isRequired: true,
            ScientificRecoveryStatus.NotRequired);
        var extraction = ScientificDocumentExtraction.Create(
            Guid.NewGuid(),
            "sha256:a3f75487411ef13d0270c65fc801ceff2b28e6b339afed9b407fe477f7e8453e",
            ScientificExtractorIdentity.Create("fixture-extractor", "1.0"),
            ScientificExtractionQuality.Create(
                isScanned: false,
                ocrApplied: false,
                ScientificReadingOrderStatus.Reliable,
                ScientificRequiredContentStatus.Complete),
            [block],
            []);
        var evidence = ClaimEvidenceLink.Create(
            extraction,
            block,
            "Net force causes acceleration",
            ClaimEvidenceRole.Support,
            confidence: 0.99,
            EvidenceValidationState.Validated);
        var claim = ScientificClaim.Create(
            "claim-force-acceleration",
            ScientificClaimCategory.CausalRelation,
            "Net force causes acceleration when mass is constant.",
            "Net force causes acceleration for constant mass.",
            confidence: 0.98,
            ScientificClaimStatus.Accepted,
            [evidence]);
        var understanding = ScientificDocumentUnderstanding.Create(
            Guid.NewGuid(),
            extraction,
            "Explain Newton's second law.",
            version: 3,
            [ScientificTermDefinition.Create(
                "term-net-force",
                "net force",
                "The vector sum of forces.",
                ["resultant force"])],
            [claim],
            [],
            [ScientificCoverageRequirement.Create(
                "coverage-main-causal-claim",
                "The main causal claim is covered.",
                isRequired: true,
                ScientificCoverageStatus.Complete,
                [claim.ClaimId])]);
        var spec = ScientificFigureTestFixture.CreateSpec(
            understanding,
            [ScientificFigureTestFixture.RequiredElement(claim, evidence)],
            [],
            []);
        var reviewedAt = DateTimeOffset.Parse("2026-07-26T12:00:00Z");
        var workflow = ScientificFigureWorkflow
            .Create(spec)
            .ApproveGate1("scientific-reviewer", "Evidence and specification verified.", reviewedAt);
        if (includeDownstreamApprovals)
        {
            workflow = workflow
                .RecordDownstreamApproval(
                    ScientificDownstreamApprovalStage.RenderPlan,
                    "render-reviewer",
                    reviewedAt.AddMinutes(1))
                .RecordDownstreamApproval(
                    ScientificDownstreamApprovalStage.ScientificReview,
                    "science-reviewer",
                    reviewedAt.AddMinutes(2));
        }

        return ScientificFigureWorkflowAggregate.Create(
            Guid.NewGuid(),
            projectId,
            extraction,
            understanding,
            workflow,
            reviewedAt.AddHours(-1),
            reviewedAt.AddMinutes(2));
    }

    private static async Task WithDatabaseAsync(
        Func<DbContextOptions<AppDbContext>, Task> test)
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "ContentDeliveryStudio.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={Path.Combine(directory, "scientific.sqlite")};Pooling=False")
            .Options;

        try
        {
            await test(options);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
