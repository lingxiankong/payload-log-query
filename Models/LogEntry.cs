namespace PayloadLogQuery.Models;

public class LogEntry
{
    public DateTimeOffset? Timestamp { get; set; }
    public string Content { get; set; } = string.Empty;
}

