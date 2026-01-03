using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text.RegularExpressions;
using PayloadLogQuery.Abstractions;
using PayloadLogQuery.Models;
using PayloadLogQuery.Options;
using Microsoft.Extensions.Options;

namespace PayloadLogQuery.Providers;

public class AzureBlobLogProvider : ILogProvider
{
    private readonly AzureBlobOptions _options;
    private readonly BlobContainerClient _container;
    private readonly IDecryptionService _decryptionService;

    // Use BlobServiceClient injected by DI
    public AzureBlobLogProvider(
        IOptions<AzureBlobOptions> options,
        BlobServiceClient blobServiceClient,
        IDecryptionService decryptionService)
    {
        _options = options.Value;
        _decryptionService = decryptionService;

        if (string.IsNullOrWhiteSpace(_options.ContainerName))
            throw new InvalidOperationException("Azure container name is required");

        _container = blobServiceClient.GetBlobContainerClient(_options.ContainerName);
    }

    public async IAsyncEnumerable<LogEntry> StreamAsync(string serviceName, string sessionId, LogQuery query, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var name = $"{serviceName}-{sessionId}.log";
        var blob = _container.GetBlobClient(name);
        if (!await blob.ExistsAsync(ct)) yield break;
        await foreach (var e in EnumerateEntries(blob, query, ct))
        {
            yield return e;
        }
    }

    public async Task<IReadOnlyDictionary<string, List<string>>> ListServiceSessionsAsync(CancellationToken ct = default)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        // Ensure container exists logic or handle if it doesn't?
        // For query tool, usually we expect it works.

        await foreach (BlobItem item in _container.GetBlobsAsync(prefix: null, cancellationToken: ct))
        {
            if (!item.Name.EndsWith(".log", StringComparison.OrdinalIgnoreCase)) continue;
            var baseName = item.Name[..^4];
            var idx = baseName.LastIndexOf('-');
            if (idx <= 0) continue;
            var service = baseName.Substring(0, idx);
            var session = baseName[(idx + 1)..];
            if (!result.TryGetValue(service, out var list))
            {
                list = new List<string>();
                result[service] = list;
            }
            if (!list.Contains(session)) list.Add(session);
        }
        return result;
    }

    private async IAsyncEnumerable<LogEntry> EnumerateEntries(BlobClient blob, LogQuery query, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        await using var s = await blob.OpenReadAsync(cancellationToken: ct);
        using var sr = new StreamReader(s);
        while (!sr.EndOfStream)
        {
            if (ct.IsCancellationRequested) yield break;
            var line = await sr.ReadLineAsync() ?? string.Empty;

            // Decrypt logic: pass the whole line to the service
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
