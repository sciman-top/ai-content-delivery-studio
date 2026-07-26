using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.Persistence;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;
using Microsoft.EntityFrameworkCore;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificGateOneWorkflowTests
{
    [Fact]
    public async Task CreateDraft_SavesReviewableVersionsWithoutAutoApproval()
    {
        await WithDatabaseAsync(async options =>
        {
            var project = ImageProject.Create(
                "Gate 1 project",
                DateTimeOffset.Parse("2026-07-26T14:00:00Z"));
            await using var db = new AppDbContext(options);
            await AppDatabaseInitializer.InitializeAsync(db, CancellationToken.None);
            db.Projects.Add(project);
            await db.SaveChangesAsync();
            var repository = new EfScientificFigureWorkflowRepository(db);
            var service = new ScientificFigureApplicationService(
                new FakeScientificUnderstandingProvider(),
                repository);

            var draft = await service.CreateDraftAsync(
                project.Id,
                ScientificUnderstandingProviderTests.Extraction(
                    ("block-force", "Net force causes acceleration for constant mass.")),
                ScientificUnderstandingProviderTests.DraftRequest(),
                DateTimeOffset.Parse("2026-07-26T14:01:00Z"),
                CancellationToken.None);
            var persisted = await repository.LoadAsync(draft.Id, CancellationToken.None);

            Assert.Equal(1, persisted!.Understanding.Version);
            Assert.Equal(1, persisted.Workflow.Specification.Version);
            Assert.Equal(ScientificFigureWorkflowState.FigureSpecDraft, persisted.Workflow.State);
            Assert.Null(persisted.Workflow.Gate1Approval);
        });
    }

    [Fact]
    public async Task DecideGateOneAsync_RequiresAffirmativeHumanDecisionAndPersistsFrozenVersions()
    {
        await WithDatabaseAsync(async options =>
        {
            var project = ImageProject.Create(
                "Gate 1 project",
                DateTimeOffset.Parse("2026-07-26T14:00:00Z"));
            await using var db = new AppDbContext(options);
            await AppDatabaseInitializer.InitializeAsync(db, CancellationToken.None);
            db.Projects.Add(project);
            await db.SaveChangesAsync();
            var repository = new EfScientificFigureWorkflowRepository(db);
            var service = new ScientificFigureApplicationService(
                new FakeScientificUnderstandingProvider(),
                repository);
            var draft = await service.CreateDraftAsync(
                project.Id,
                ScientificUnderstandingProviderTests.Extraction(
                    ("block-force", "Net force causes acceleration for constant mass.")),
                ScientificUnderstandingProviderTests.DraftRequest(),
                DateTimeOffset.Parse("2026-07-26T14:01:00Z"),
                CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.DecideGateOneAsync(
                    draft.Id,
                    new ScientificGateOneDecision(
                        Approved: false,
                        "human-reviewer",
                        "Not approved.",
                        DateTimeOffset.Parse("2026-07-26T14:05:00Z")),
                    CancellationToken.None));

            var approved = await service.DecideGateOneAsync(
                draft.Id,
                new ScientificGateOneDecision(
                    Approved: true,
                    "human-reviewer",
                    "Claims, evidence, and spec verified.",
                    DateTimeOffset.Parse("2026-07-26T14:10:00Z")),
                CancellationToken.None);
            var reloaded = await repository.LoadAsync(draft.Id, CancellationToken.None);

            Assert.Equal(ScientificFigureWorkflowState.Gate1Approved, approved.Workflow.State);
            Assert.Equal(
                approved.Understanding.Version,
                reloaded!.Workflow.Gate1Approval!.ApprovedUnderstandingVersion);
            Assert.Equal(
                approved.Workflow.Specification.Version,
                reloaded.Workflow.Gate1Approval.ApprovedSpecVersion);
            Assert.Equal("human-reviewer", reloaded.Workflow.Gate1Approval.Reviewer);
        });
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
            .UseSqlite($"Data Source={Path.Combine(directory, "gate-one.sqlite")};Pooling=False")
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
