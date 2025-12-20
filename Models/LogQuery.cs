namespace PayloadLogQuery.Models;

public class LogQuery
{
    public string? Keyword { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int? StatusCode { get; set; }

    public int? Limit { get; set; }
    public bool ExcludeFrom { get; set; }
}

