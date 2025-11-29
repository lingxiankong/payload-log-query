namespace PayloadLogQuery.Models;

public class LogQuery
{
    public string? Keyword { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int? StatusCode { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 100;
}

