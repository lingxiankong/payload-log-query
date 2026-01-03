using System.Text.RegularExpressions;
using PayloadLogQuery.Abstractions;
using PayloadLogQuery.Models;
using PayloadLogQuery.Options;
using Microsoft.Extensions.Options;

namespace PayloadLogQuery.Providers;

public class LocalLogProvider : ILogProvider
{
    private readonly LocalLogOptions _options;
    private readonly IDecryptionService _decryptionService;

    public LocalLogProvider(IOptions<LocalLogOptions> options, IDecryptionService decryptionService)
    {
        _options = options.Value;
        _decryptionService = decryptionService;
        Directory.CreateDirectory(_options.LogDirectory);
    }



    public async IAsyncEnumerable<LogEntry> StreamAsync(string serviceName, string sessionId, LogQuery query, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var path = GetPath(serviceName, sessionId);
        if (!File.Exists(path)) yield break;

        await foreach (var entry in EnumerateEntries(path, query, ct))
        {
            yield return entry;
        }
    }

    public Task<IReadOnlyDictionary<string, List<string>>> ListServiceSessionsAsync(CancellationToken ct = default)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(_options.LogDirectory)) return Task.FromResult((IReadOnlyDictionary<string, List<string>>)result);

        foreach (var file in Directory.EnumerateFiles(_options.LogDirectory, "*.log"))
        {
            var name = Path.GetFileName(file);
            var (service, session) = ParseName(name);
            if (service is null || session is null) continue;
            if (!result.TryGetValue(service, out var list))
            {
                list = new List<string>();
                result[service] = list;
            }
            if (!list.Contains(session)) list.Add(session);
        }
        return Task.FromResult((IReadOnlyDictionary<string, List<string>>)result);
    }

    private string GetPath(string serviceName, string sessionId) => Path.Combine(_options.LogDirectory, $"{serviceName}-{sessionId}.log");

    private static (string? service, string? session) ParseName(string fileName)
    {
        if (!fileName.EndsWith(".log", StringComparison.OrdinalIgnoreCase)) return (null, null);
        var baseName = fileName[..^4];
        var idx = baseName.LastIndexOf('-');
        if (idx <= 0) return (null, null);
        var service = baseName.Substring(0, idx);
        var session = baseName[(idx + 1)..];
        return (service, session);
    }

    private async IAsyncEnumerable<LogEntry> EnumerateEntries(string path, LogQuery query, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        await using var fs = File.OpenRead(path);
        using var sr = new StreamReader(fs);
        while (!sr.EndOfStream)
        {
            if (ct.IsCancellationRequested) yield break;
            var line = await sr.ReadLineAsync() ?? string.Empty;

            // Decrypt logic
            // Decrypt logic
            line = await _decryptionService.DecryptAsync(line, ct);

            var ts = ExtractTimestamp(line);
            if (query.From.HasValue && ts.HasValue)
            {
                if (query.ExcludeFrom)
                {
                    if (ts.Value <= query.From.Value) continue;
                }
                else
                {
                    if (ts.Value < query.From.Value) continue;
                }
            }
            if (query.To.HasValue && ts.HasValue && ts.Value > query.To.Value) continue;
            if (query.Keyword is not null && !line.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase)) continue;
            if (query.StatusCode.HasValue)
            {
                var code = ExtractStatusCode(line);
                if (code != query.StatusCode.Value) continue;
            }
            if (query.Limit.HasValue && query.Limit.Value <= 0) yield break;

            yield return new LogEntry { Timestamp = ts, Content = line };

            if (query.Limit.HasValue) query.Limit--;
        }
    }



    private static DateTimeOffset? ExtractTimestamp(string line)
    {
        var match = Regex.Match(line, "^(?:\\[)?(?<ts>\\d{4}-\\d{2}-\\d{2}[ T]\\d{2}:\\d{2}:\\d{2}(?:\\.\\d+)?(?:Z|[+-]\\d{2}:?\\d{2})?)(?:\\])?");
        if (match.Success)
        {
            if (DateTimeOffset.TryParse(match.Groups["ts"].Value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var dto)) return dto;
        }
        return null;
    }

    private static int? ExtractStatusCode(string line)
    {
        var m = Regex.Match(line, "status(?:Code)?[=:]\\s*(?<code>\\d{3})", RegexOptions.IgnoreCase);
        if (m.Success && int.TryParse(m.Groups["code"].Value, out var code)) return code;
        var m2 = Regex.Match(line, "[\"']status[\"']\\s*:\\s*(?<code>\\d{3})");
        if (m2.Success && int.TryParse(m2.Groups["code"].Value, out code)) return code;
        return null;
    }
}
