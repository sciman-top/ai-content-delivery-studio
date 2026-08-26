using System.Data.Common;
using ContentDeliveryStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentDeliveryStudio.Tests;

public sealed class AppDatabaseInitializerSchemaVersionTests
{
    [Fact]
    public async Task FreshDatabase_MatchesCurrentModelAndIsStamped()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.InitializeAsync();

        var version = await database.GetSchemaVersionAsync();
        Assert.Equal(AppDatabaseInitializer.CurrentSchemaVersion, version);
        Assert.Contains("ConcurrencyVersion", await database.GetColumnsAsync("Projects"));
        Assert.Contains("ApprovalReceipt", await database.GetColumnsAsync("GenerationTasks"));
        Assert.Contains("EditProvenance", await database.GetColumnsAsync("CandidateImages"));
        Assert.True(await database.TableExistsAsync("ScientificFigureWorkflows"));
        Assert.True(await database.IndexExistsAsync("IX_ReviewResults_CandidateImageId"));
    }

    [Fact]
    public async Task VersionOneDatabase_WithDuplicateReviewResults_DedupesToLowestIdAndEnforcesUniqueIndex()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.InitializeAsync();

        // Simulate a version-1 database written before the one-review
        // invariant existed: no unique index, duplicate rows per candidate.
        await database.ExecuteRawAsync("DROP INDEX \"IX_ReviewResults_CandidateImageId\";");
        var candidateId = Guid.NewGuid();
        var keptId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var removedId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        string InsertLegacyReview(Guid id, string comments) =>
            "INSERT INTO \"ReviewResults\" (\"Id\",\"CandidateImageId\",\"Decision\",\"Scores\",\"HardFailures\",\"Comments\",\"HumanApproved\",\"CreatedAt\") VALUES ('"
            + id + "', '" + candidateId + "', 0, '{}', '[]', '" + comments
            + "', 0, '2026-01-01 00:00:00+00:00');";
        await database.ExecuteRawAsync(
            "PRAGMA foreign_keys=OFF;\n"
            + InsertLegacyReview(keptId, "legacy-duplicate") + "\n"
            + InsertLegacyReview(removedId, "legacy-duplicate") + "\n"
            + "PRAGMA user_version = 1;");
        Assert.Equal(1, await database.GetSchemaVersionAsync());

        await database.InitializeAsync();

        Assert.Equal(AppDatabaseInitializer.CurrentSchemaVersion, await database.GetSchemaVersionAsync());
        Assert.Equal(
            keptId,
            Guid.Parse(await database.ExecuteScalarAsync<string>("SELECT \"Id\" FROM \"ReviewResults\";")));
        Assert.True(await database.IndexExistsAsync("IX_ReviewResults_CandidateImageId"));

        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(
            () => database.ExecuteRawAsync(
                "PRAGMA foreign_keys=OFF;\n"
                + InsertLegacyReview(Guid.NewGuid(), "post-migration")));

        Assert.Equal(
            1L,
            await database.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM \"ReviewResults\";"));
    }

    [Fact]
    public async Task LegacyUnstampedDatabase_ReconcilesMissingColumnsAndStamps()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.InitializeAsync();
        await database.DropColumnAsync("Projects", "ConcurrencyVersion");
        await database.DropColumnAsync("GenerationTasks", "ApprovalReceipt");
        await database.ExecuteRawAsync("PRAGMA user_version = 0;");

        await database.InitializeAsync();

        Assert.Equal(AppDatabaseInitializer.CurrentSchemaVersion, await database.GetSchemaVersionAsync());
        Assert.Contains("ConcurrencyVersion", await database.GetColumnsAsync("Projects"));
        Assert.Contains("ApprovalReceipt", await database.GetColumnsAsync("GenerationTasks"));
    }

    [Fact]
    public async Task NewerSchemaVersion_FailsClosed()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.InitializeAsync();
        await database.ExecuteRawAsync(
            $"PRAGMA user_version = {AppDatabaseInitializer.CurrentSchemaVersion + 1};");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => database.InitializeAsync());

        Assert.Contains("newer than this build supports", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StampedDatabase_TrustsStampAndSkipsCompatibilityDdl()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.InitializeAsync();
        Assert.Equal(
            AppDatabaseInitializer.CurrentSchemaVersion,
            await database.GetSchemaVersionAsync());

        // A stamped database skips the DDL entirely, so a manually drifted
        // column is not repaired; recovery for stamped databases is restore
        // from backup, not silent reconciliation.
        await database.DropColumnAsync("Projects", "ConcurrencyVersion");
        await database.InitializeAsync();

        Assert.DoesNotContain("ConcurrencyVersion", await database.GetColumnsAsync("Projects"));
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private TestDatabase(DbContextOptions<AppDbContext> options, string databasePath)
        {
            Options = options;
            DatabasePath = databasePath;
        }

        private DbContextOptions<AppDbContext> Options { get; }

        private string DatabasePath { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var databaseDirectory = Path.Combine(
                Path.GetTempPath(),
                "ContentDeliveryStudio.Tests",
                Guid.NewGuid().ToString("N"));
            var databasePath = Path.Combine(databaseDirectory, "schema-version.sqlite");
            Directory.CreateDirectory(databaseDirectory);

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;
            return new TestDatabase(options, databasePath);
        }

        public async Task InitializeAsync()
        {
            await using var db = new AppDbContext(Options);
            await AppDatabaseInitializer.InitializeAsync(db, CancellationToken.None);
        }

        public async Task<int> GetSchemaVersionAsync()
        {
            await using var db = new AppDbContext(Options);
            return await AppDatabaseInitializer.GetSchemaVersionAsync(db, CancellationToken.None);
        }

        public async Task<List<string>> GetColumnsAsync(string tableName)
        {
            var columns = new List<string>();
            await ExecuteAsync(async connection =>
            {
                await using var command = connection.CreateCommand();
                command.CommandText = $"PRAGMA table_info('{tableName}');";
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    columns.Add(reader.GetString(1));
                }
            });

            return columns;
        }

    public async Task<bool> TableExistsAsync(string tableName)
    {
        var exists = false;
        await ExecuteAsync(async connection =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "$name";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);
            exists = Convert.ToInt64(await command.ExecuteScalarAsync()) == 1;
        });

        return exists;
    }

    public async Task<bool> IndexExistsAsync(string indexName)
    {
        var exists = false;
        await ExecuteAsync(async connection =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = $name;";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "$name";
            parameter.Value = indexName;
            command.Parameters.Add(parameter);
            exists = Convert.ToInt64(await command.ExecuteScalarAsync()) == 1;
        });

        return exists;
    }

    public async Task<T> ExecuteScalarAsync<T>(string commandText)
    {
        T result = default!;
        await ExecuteAsync(async connection =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText = commandText;
            var scalar = await command.ExecuteScalarAsync();
            if (scalar is not null)
            {
                result = (T)Convert.ChangeType(scalar, typeof(T));
            }
        });

        return result;
    }

        public Task DropColumnAsync(string tableName, string columnName)
            => ExecuteRawAsync($"ALTER TABLE \"{tableName}\" DROP COLUMN \"{columnName}\";");

        public Task ExecuteRawAsync(string commandText)
            => ExecuteAsync(connection => ExecuteConnectionAsync(connection, commandText));

        public async ValueTask DisposeAsync()
        {
            var directory = Path.GetDirectoryName(DatabasePath);
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        private async Task ExecuteAsync(Func<DbConnection, Task> action)
        {
            await using var db = new AppDbContext(Options);
            var connection = db.Database.GetDbConnection();
            await connection.OpenAsync();
            try
            {
                await action(connection);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        private static async Task ExecuteConnectionAsync(DbConnection connection, string commandText)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = commandText;
            await command.ExecuteNonQueryAsync();
        }
    }
}
