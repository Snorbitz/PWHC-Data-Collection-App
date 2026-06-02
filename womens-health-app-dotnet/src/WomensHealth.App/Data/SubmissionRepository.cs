using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using WomensHealth.App.Models;

namespace WomensHealth.App.Data;

public sealed class SubmissionRepository
{
    private readonly DatabaseInitializer _database;

    public SubmissionRepository(DatabaseInitializer database)
    {
        _database = database;
    }

    public long Insert(SubmissionRequest request)
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        var fields = SubmissionSchema.InsertFields;
        var columns = string.Join(", ", fields);
        var placeholders = string.Join(", ", fields.Select((_, index) => $"@p{index}"));
        command.CommandText = $"INSERT INTO submissions ({columns}) VALUES ({placeholders}); SELECT last_insert_rowid();";

        var values = request.GetInsertValues();
        for (var i = 0; i < values.Count; i++)
        {
            command.Parameters.AddWithValue($"@p{i}", values[i]);
        }

        return (long)(command.ExecuteScalar() ?? 0L);
    }

    public RecordsResponse QueryRecords(IQueryCollection query)
    {
        var page = ReadPositiveInt(query, "page", 1);
        var perPage = ReadPositiveInt(query, "per_page", 50);
        var offset = (page - 1) * perPage;
        var where = BuildWhereClause(query);

        using var connection = _database.OpenConnection();
        var total = Count(connection, where);

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM submissions{where.Sql} ORDER BY session_date DESC, id DESC LIMIT @limit OFFSET @offset";
        AddParameters(command, where.Parameters);
        command.Parameters.AddWithValue("@limit", perPage);
        command.Parameters.AddWithValue("@offset", offset);

        using var reader = command.ExecuteReader();
        var records = ReadRows(reader);
        return new RecordsResponse(total, page, perPage, records);
    }

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> QueryForExport(IQueryCollection query)
    {
        var where = BuildWhereClause(query);
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM submissions{where.Sql} ORDER BY session_date DESC, id DESC";
        AddParameters(command, where.Parameters);
        using var reader = command.ExecuteReader();
        return ReadRows(reader);
    }

    public DeleteResult Delete(long id)
    {
        using var connection = _database.OpenConnection();
        using var exists = connection.CreateCommand();
        exists.CommandText = "SELECT id FROM submissions WHERE id = @id";
        exists.Parameters.AddWithValue("@id", id);
        if (exists.ExecuteScalar() is null)
        {
            return DeleteResult.NotFound;
        }

        using var delete = connection.CreateCommand();
        delete.CommandText = "DELETE FROM submissions WHERE id = @id";
        delete.Parameters.AddWithValue("@id", id);
        delete.ExecuteNonQuery();
        return DeleteResult.Deleted;
    }

    private static int Count(SqliteConnection connection, WhereClause where)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM submissions{where.Sql}";
        AddParameters(command, where.Parameters);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static WhereClause BuildWhereClause(IQueryCollection query)
    {
        var clauses = new List<string>();
        var parameters = new List<SqliteParameter>();
        var index = 0;

        void Add(string clause, object value)
        {
            var name = $"@p{index++}";
            clauses.Add(clause.Replace("?", name, StringComparison.Ordinal));
            parameters.Add(new SqliteParameter(name, value));
        }

        if (TryGet(query, "date_from", out var dateFrom))
        {
            Add("session_date >= ?", dateFrom);
        }

        if (TryGet(query, "date_to", out var dateTo))
        {
            Add("session_date <= ?", dateTo);
        }

        foreach (var field in SubmissionSchema.ExactFilters)
        {
            if (TryGet(query, field, out var value))
            {
                Add($"{field} = ?", value);
            }
        }

        foreach (var field in SubmissionSchema.LikeFilters)
        {
            if (TryGet(query, field, out var value))
            {
                Add($"{field} LIKE ?", $"%{value}%");
            }
        }

        if (TryGet(query, "search", out var search))
        {
            var searchParts = new List<string>();
            var searchTerm = $"%{search}%";
            foreach (var field in SubmissionSchema.SearchFields)
            {
                var name = $"@p{index++}";
                searchParts.Add($"{field} LIKE {name}");
                parameters.Add(new SqliteParameter(name, searchTerm));
            }

            clauses.Add("(" + string.Join(" OR ", searchParts) + ")");
        }

        var sql = clauses.Count == 0 ? "" : " WHERE " + string.Join(" AND ", clauses);
        return new WhereClause(sql, parameters);
    }

    private static bool TryGet(IQueryCollection query, string key, out string value)
    {
        value = "";
        if (!query.TryGetValue(key, out var values))
        {
            return false;
        }

        value = values.FirstOrDefault() ?? "";
        return !string.IsNullOrWhiteSpace(value);
    }

    private static int ReadPositiveInt(IQueryCollection query, string key, int fallback)
    {
        if (!TryGet(query, key, out var value) || !int.TryParse(value, out var parsed) || parsed < 1)
        {
            return fallback;
        }

        return parsed;
    }

    private static void AddParameters(SqliteCommand command, IEnumerable<SqliteParameter> parameters)
    {
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }
    }

    private static List<IReadOnlyDictionary<string, object?>> ReadRows(SqliteDataReader reader)
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            rows.Add(row);
        }

        return rows;
    }

    private sealed record WhereClause(string Sql, IReadOnlyList<SqliteParameter> Parameters);
}

public enum DeleteResult
{
    NotFound,
    Deleted
}
