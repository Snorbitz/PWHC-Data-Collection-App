namespace WomensHealth.App.Models;

public sealed record RecordsResponse(
    int Total,
    int Page,
    int PerPage,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Records);
