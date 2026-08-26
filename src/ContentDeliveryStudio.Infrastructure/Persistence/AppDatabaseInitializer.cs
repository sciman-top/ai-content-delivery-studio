using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ContentDeliveryStudio.Infrastructure.Persistence;

public static class AppDatabaseInitializer
{
    // Additive-DDL schema level, tracked in SQLite PRAGMA user_version. A
    // database stamped at the current version skips the compatibility DDL on
    // later startups; an unstamped (pre-versioning) database reconciles the
    // full idempotent DDL once and is then stamped. Bump this constant and
    // append the matching upgrade steps when the schema changes again.
    // Version 2 added the unique ReviewResults.CandidateImageId index
    // enforcing the one-current-review-per-candidate invariant.
    public const int CurrentSchemaVersion = 2;

    public static async Task InitializeAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        var createdNewDatabase = await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State is not ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var schemaVersion = await GetSchemaVersionAsync(connection, cancellationToken);
            if (schemaVersion > CurrentSchemaVersion)
            {
                throw new InvalidOperationException(
                    $"Local database schema version {schemaVersion} is newer than this build supports ({CurrentSchemaVersion}). "
                    + "Open the database with a matching or newer application version.");
            }

            if (schemaVersion == CurrentSchemaVersion)
            {
                return;
            }

            // EnsureCreated does not update an existing database, so a database
            // that existed before this run reconciles the idempotent
            // compatibility DDL; a database EnsureCreated just built already
            // matches the current model and only needs the version stamp.
            if (!createdNewDatabase)
            {
                await EnsureImageProjectCompatibilityColumnsAsync(dbContext, cancellationToken);
                await EnsureGenerationTaskCompatibilityColumnsAsync(dbContext, cancellationToken);
                await EnsureCandidateImageCompatibilityColumnsAsync(dbContext, cancellationToken);
                await EnsureScientificFigureWorkflowTableAsync(connection, cancellationToken);
                if (schemaVersion < 2)
                {
                    await EnsureReviewResultUniquenessAsync(connection, cancellationToken);
                }
            }

            await SetSchemaVersionAsync(connection, CurrentSchemaVersion, cancellationToken);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    public static async Task<int> GetSchemaVersionAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State is not ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            return await GetSchemaVersionAsync(connection, cancellationToken);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task EnsureScientificFigureWorkflowTableAsync(
        System.Data.Common.DbConnection connection,
        CancellationToken cancellationToken)
    {
        await ExecuteConnectionCommandAsync(
            connection,
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
        await ExecuteConnectionCommandAsync(
            connection,
            """
            CREATE INDEX IF NOT EXISTS "IX_ScientificFigureWorkflows_ProjectId"
            ON "ScientificFigureWorkflows" ("ProjectId");
            """,
            cancellationToken);
        await ExecuteConnectionCommandAsync(
            connection,
            """
            CREATE UNIQUE INDEX IF NOT EXISTS
                "IX_ScientificFigureWorkflows_ProjectId_SpecificationId_SpecificationVersion"
            ON "ScientificFigureWorkflows"
                ("ProjectId", "SpecificationId", "SpecificationVersion");
            """,
            cancellationToken);
    }

    private static async Task<int> GetSchemaVersionAsync(
        System.Data.Common.DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is long value ? (int)value : 0;
    }

    private static async Task SetSchemaVersionAsync(
        System.Data.Common.DbConnection connection,
        int schemaVersion,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA user_version = {schemaVersion};";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureImageProjectCompatibilityColumnsAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State is not ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var inspect = connection.CreateCommand())
            {
                inspect.CommandText = "PRAGMA table_info('Projects');";
                await using var reader = await inspect.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    existingColumns.Add(reader.GetString(1));
                }
            }

            if (!existingColumns.Contains("ConcurrencyVersion"))
            {
                await using var addColumn = connection.CreateCommand();
                addColumn.CommandText =
                    "ALTER TABLE \"Projects\" ADD COLUMN \"ConcurrencyVersion\" INTEGER NOT NULL DEFAULT 0;";
                await addColumn.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task EnsureGenerationTaskCompatibilityColumnsAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State is not ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var inspect = connection.CreateCommand())
            {
                inspect.CommandText = "PRAGMA table_info('GenerationTasks');";
                await using var reader = await inspect.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    existingColumns.Add(reader.GetString(1));
                }
            }

            var missingColumns = new (string Name, string Definition)[]
            {
                ("ErrorMessage", "TEXT NULL"),
                ("QueuePosition", "INTEGER NULL"),
                ("RetryOfTaskId", "TEXT NULL"),
                ("ApprovalReceipt", "TEXT NULL"),
            };

            foreach (var column in missingColumns.Where(column => !existingColumns.Contains(column.Name)))
            {
                await using var addColumn = connection.CreateCommand();
                addColumn.CommandText =
                    $"ALTER TABLE \"GenerationTasks\" ADD COLUMN \"{column.Name}\" {column.Definition};";
                await addColumn.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task EnsureCandidateImageCompatibilityColumnsAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State is not ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var generationTaskIdIsRequired = false;
            await using (var inspect = connection.CreateCommand())
            {
                inspect.CommandText = "PRAGMA table_info('CandidateImages');";
                await using var reader = await inspect.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var name = reader.GetString(1);
                    existingColumns.Add(name);
                    if (name.Equals("GenerationTaskId", StringComparison.OrdinalIgnoreCase))
                    {
                        generationTaskIdIsRequired = reader.GetInt32(3) != 0;
                    }
                }
            }

            if (generationTaskIdIsRequired)
            {
                await RebuildCandidateImagesForNullableGenerationTaskAsync(connection, cancellationToken);
            }
            else if (!existingColumns.Contains("EditProvenance"))
            {
                await using var addColumn = connection.CreateCommand();
                addColumn.CommandText =
                    "ALTER TABLE \"CandidateImages\" ADD COLUMN \"EditProvenance\" TEXT NULL;";
                await addColumn.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task RebuildCandidateImagesForNullableGenerationTaskAsync(
        System.Data.Common.DbConnection connection,
        CancellationToken cancellationToken)
    {
        await ExecuteConnectionCommandAsync(connection, "PRAGMA foreign_keys=OFF;", cancellationToken);
        await ExecuteConnectionCommandAsync(connection, "PRAGMA legacy_alter_table=ON;", cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await ExecuteConnectionCommandAsync(
                connection,
                "ALTER TABLE \"CandidateImages\" RENAME TO \"CandidateImages_LegacyEditUpgrade\";",
                cancellationToken,
                transaction);
            await ExecuteConnectionCommandAsync(
                connection,
                """
                CREATE TABLE "CandidateImages" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_CandidateImages" PRIMARY KEY,
                    "SeriesItemId" TEXT NOT NULL,
                    "PromptVersionId" TEXT NOT NULL,
                    "GenerationTaskId" TEXT NULL,
                    "ProviderProfileId" TEXT NOT NULL,
                    "Status" INTEGER NOT NULL,
                    "AssetPath" TEXT NOT NULL,
                    "MetadataPath" TEXT NOT NULL,
                    "EditProvenance" TEXT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    CONSTRAINT "FK_CandidateImages_SeriesItems_SeriesItemId"
                        FOREIGN KEY ("SeriesItemId") REFERENCES "SeriesItems" ("Id") ON DELETE CASCADE
                );
                """,
                cancellationToken,
                transaction);
            await ExecuteConnectionCommandAsync(
                connection,
                """
                INSERT INTO "CandidateImages" (
                    "Id", "SeriesItemId", "PromptVersionId", "GenerationTaskId",
                    "ProviderProfileId", "Status", "AssetPath", "MetadataPath", "CreatedAt")
                SELECT
                    "Id", "SeriesItemId", "PromptVersionId", "GenerationTaskId",
                    "ProviderProfileId", "Status", "AssetPath", "MetadataPath", "CreatedAt"
                FROM "CandidateImages_LegacyEditUpgrade";
                """,
                cancellationToken,
                transaction);
            await ExecuteConnectionCommandAsync(
                connection,
                "DROP TABLE \"CandidateImages_LegacyEditUpgrade\";",
                cancellationToken,
                transaction);
            await ExecuteConnectionCommandAsync(
                connection,
                "CREATE INDEX \"IX_CandidateImages_SeriesItemId\" ON \"CandidateImages\" (\"SeriesItemId\");",
                cancellationToken,
                transaction);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            await ExecuteConnectionCommandAsync(connection, "PRAGMA legacy_alter_table=OFF;", CancellationToken.None);
            await ExecuteConnectionCommandAsync(connection, "PRAGMA foreign_keys=ON;", CancellationToken.None);
        }
    }

    private static async Task EnsureReviewResultUniquenessAsync(
        System.Data.Common.DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            // Legacy databases may hold duplicate review rows per candidate
            // from pre-constraint writes; keep the lowest Id to match the
            // repository's deterministic OrderBy(Id) pick, then enforce the
            // one-current-review invariant with the same unique index the
            // current EF model generates for fresh databases.
            await ExecuteConnectionCommandAsync(
                connection,
                """
                DELETE FROM "ReviewResults"
                WHERE "Id" NOT IN (
                    SELECT MIN("Id") FROM "ReviewResults" GROUP BY "CandidateImageId");
                """,
                cancellationToken,
                transaction);
            await ExecuteConnectionCommandAsync(
                connection,
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_ReviewResults_CandidateImageId"
                ON "ReviewResults" ("CandidateImageId");
                """,
                cancellationToken,
                transaction);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task ExecuteConnectionCommandAsync(
        System.Data.Common.DbConnection connection,
        string commandText,
        CancellationToken cancellationToken,
        System.Data.Common.DbTransaction? transaction = null)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.Transaction = transaction;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
