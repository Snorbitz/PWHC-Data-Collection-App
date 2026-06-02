using Microsoft.Data.Sqlite;

namespace WomensHealth.App.Data;

public sealed class DatabaseInitializer
{
    private readonly AppPaths _paths;

    public DatabaseInitializer(AppPaths paths)
    {
        _paths = paths;
    }

    public void Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_paths.DatabasePath)!);

        using var connection = OpenConnection();
        using var createCommand = connection.CreateCommand();
        createCommand.CommandText = SubmissionSchema.CreateTableSql;
        createCommand.ExecuteNonQuery();

        var columns = ReadColumns(connection);
        foreach (var migration in SubmissionSchema.MigrationColumns)
        {
            if (columns.Contains(migration.Key))
            {
                continue;
            }

            using var alter = connection.CreateCommand();
            alter.CommandText = $"ALTER TABLE submissions ADD COLUMN {migration.Value}";
            alter.ExecuteNonQuery();
        }
    }

    public SqliteConnection OpenConnection()
    {
        var builder = new SqliteConnectionStringBuilder { DataSource = _paths.DatabasePath };
        var connection = new SqliteConnection(builder.ToString());
        connection.Open();

        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA journal_mode=WAL";
        pragma.ExecuteNonQuery();

        return connection;
    }

    private static HashSet<string> ReadColumns(SqliteConnection connection)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(submissions)";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            columns.Add(reader.GetString(reader.GetOrdinal("name")));
        }

        return columns;
    }
}
