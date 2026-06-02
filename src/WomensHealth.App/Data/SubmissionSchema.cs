namespace WomensHealth.App.Data;

public static class SubmissionSchema
{
    public static readonly string[] Columns = new[]
    {
        "id", "submitted_at", "session_date", "client_id", "age", "contact_mode", "country",
        "language", "income_source", "visa_type", "ethnicity", "disability", "chronic_illness",
        "presenting_issues", "service_provided", "service_type", "practitioner", "group_type",
        "evaluation_tools", "staff_member", "client_status", "visit_number", "carer",
        "financial_hardship", "social_isolation", "rural_postcode", "lgbtiq", "funding_stream",
        "funding_option"
    };

    public static readonly string[] InsertFields = new[]
    {
        "session_date", "client_id", "staff_member", "client_status", "visit_number",
        "age", "carer", "financial_hardship", "social_isolation", "rural_postcode", "lgbtiq",
        "funding_stream", "funding_option", "contact_mode", "country", "language",
        "income_source", "visa_type", "ethnicity", "disability", "chronic_illness",
        "presenting_issues", "service_provided", "service_type", "practitioner",
        "group_type", "evaluation_tools"
    };

    public static readonly string[] ExactFilters = new[] { "age", "contact_mode" };

    public static readonly string[] LikeFilters = new[]
    {
        "client_id", "staff_member", "client_status", "visit_number", "carer",
        "financial_hardship", "social_isolation", "rural_postcode", "lgbtiq", "funding_stream",
        "funding_option", "country", "language", "ethnicity", "visa_type", "income_source",
        "disability", "chronic_illness", "presenting_issues", "service_provided", "service_type",
        "practitioner", "group_type", "evaluation_tools"
    };

    public static readonly string[] SearchFields = new[]
    {
        "client_id", "staff_member", "client_status", "visit_number", "age", "contact_mode",
        "session_date", "carer", "financial_hardship", "social_isolation", "rural_postcode",
        "lgbtiq", "funding_stream", "funding_option", "country", "language", "ethnicity",
        "disability", "chronic_illness", "presenting_issues", "service_provided", "service_type",
        "practitioner", "evaluation_tools", "group_type", "visa_type", "income_source"
    };

    public static readonly IReadOnlyDictionary<string, string> MigrationColumns =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["staff_member"] = "staff_member TEXT",
            ["client_status"] = "client_status TEXT",
            ["visit_number"] = "visit_number TEXT",
            ["carer"] = "carer TEXT DEFAULT 'No'",
            ["financial_hardship"] = "financial_hardship TEXT DEFAULT 'No'",
            ["social_isolation"] = "social_isolation TEXT DEFAULT 'No'",
            ["rural_postcode"] = "rural_postcode TEXT DEFAULT 'No'",
            ["lgbtiq"] = "lgbtiq TEXT DEFAULT 'No'",
            ["funding_stream"] = "funding_stream TEXT",
            ["funding_option"] = "funding_option TEXT"
        };

    public const string CreateTableSql = @"
        CREATE TABLE IF NOT EXISTS submissions (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            submitted_at TEXT NOT NULL DEFAULT (datetime('now','localtime')),
            session_date TEXT NOT NULL,
            client_id TEXT,
            age TEXT,
            contact_mode TEXT,
            country TEXT,
            language TEXT,
            income_source TEXT,
            visa_type TEXT,
            ethnicity TEXT,
            disability TEXT,
            chronic_illness TEXT,
            presenting_issues TEXT,
            service_provided TEXT,
            service_type TEXT,
            practitioner TEXT,
            group_type TEXT,
            evaluation_tools TEXT
        )
        ";
}
