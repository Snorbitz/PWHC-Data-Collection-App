using Microsoft.Data.Sqlite;
using WomensHealth.App.Data;

namespace WomensHealth.App.Tests;

public sealed class DatabaseInitializerTests
{
    [Fact]
    public void InitializeCreatesSubmissionsWithRequiredColumns()
    {
        var directory = TestPaths.CreateTempDirectory();
        var initializer = new DatabaseInitializer(TestPaths.CreateAppPaths(directory));

        initializer.Initialize();

        var columns = ReadColumns(Path.Combine(directory, "womenshealth.db"));
        foreach (var column in SubmissionSchema.Columns)
        {
            Assert.Contains(column, columns);
        }
    }

    [Fact]
    public void InitializeMigratesPreExistingDatabase()
    {
        var directory = TestPaths.CreateTempDirectory();
        var databasePath = Path.Combine(directory, "womenshealth.db");
        using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString()))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = SubmissionSchema.CreateTableSql;
            command.ExecuteNonQuery();
        }

        var initializer = new DatabaseInitializer(TestPaths.CreateAppPaths(directory));
        initializer.Initialize();

        var columns = ReadColumns(databasePath);
        foreach (var column in SubmissionSchema.MigrationColumns.Keys)
        {
            Assert.Contains(column, columns);
        }
    }

    private static HashSet<string> ReadColumns(string databasePath)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(submissions)";
        using var reader = command.ExecuteReader();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            columns.Add(reader.GetString(reader.GetOrdinal("name")));
        }
        return columns;
    }
}
