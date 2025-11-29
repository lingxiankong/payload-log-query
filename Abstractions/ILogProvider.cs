using PayloadLogQuery.Models;

namespace PayloadLogQuery.Abstractions;

public interface ILogProvider
{
    Task<LogReadResult> ReadAsync(string serviceName, string sessionId, LogQuery query, CancellationToken ct = default);
    IAsyncEnumerable<LogEntry> StreamAsync(string serviceName, string sessionId, LogQuery query, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, List<string>>> ListServiceSessionsAsync(CancellationToken ct = default);
}

