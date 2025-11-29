namespace PayloadLogQuery.Models;

public class LogReadResult
{
    public IReadOnlyList<LogEntry> Entries { get; set; } = Array.Empty<LogEntry>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
    public long? TotalMatched { get; set; }
}

