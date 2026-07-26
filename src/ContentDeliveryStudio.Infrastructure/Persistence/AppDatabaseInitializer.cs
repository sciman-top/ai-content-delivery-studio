using Microsoft.EntityFrameworkCore;

namespace ContentDeliveryStudio.Infrastructure.Persistence;

public static class AppDatabaseInitializer
{
    public static async Task InitializeAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        // EnsureCreated does not update an existing database. This additive DDL is
        // intentionally idempotent so pre-scientific-workflow databases gain only
        // the new project-owned table and indexes.
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "ScientificFigureWorkflows" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_ScientificFigureWorkflows" PRIMARY KEY,
                "ProjectId" TEXT NOT NULL,
                "SourceAssetId" TEXT NOT NULL,
                "SourceSha256" TEXT NOT NULL,
                "UnderstandingId" TEXT NOT NULL,
                "UnderstandingVersion" INTEGER NOT NULL,
                "SpecificationId" TEXT NOT NULL,
                "SpecificationVersion" INTEGER NOT NULL,
                "WorkflowState" INTEGER NOT NULL,
                "Gate1ApprovedSpecVersion" INTEGER NULL,
                "PayloadSchemaVersion" TEXT NOT NULL,
                "PayloadJson" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                CONSTRAINT "FK_ScientificFigureWorkflows_Projects_ProjectId"
                    FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE
            );
            """,
            cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS "IX_ScientificFigureWorkflows_ProjectId"
            ON "ScientificFigureWorkflows" ("ProjectId");
            """,
            cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS
                "IX_ScientificFigureWorkflows_ProjectId_SpecificationId_SpecificationVersion"
            ON "ScientificFigureWorkflows"
                ("ProjectId", "SpecificationId", "SpecificationVersion");
            """,
            cancellationToken);
    }
}
