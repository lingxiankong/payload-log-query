using Microsoft.Extensions.Caching.Memory;
using PayloadLogQuery.Abstractions;

namespace PayloadLogQuery.Services;

public class ServiceSessionCache
{
    private readonly IMemoryCache _cache;
    private readonly ILogProvider _provider;

    public ServiceSessionCache(IMemoryCache cache, ILogProvider provider)
    {
        _cache = cache;
        _provider = provider;
    }

    public async Task<IReadOnlyDictionary<string, List<string>>> GetAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue<IReadOnlyDictionary<string, List<string>>>("service_sessions", out var cached) && cached is not null)
        {
            return cached;
        }

        var data = await _provider.ListServiceSessionsAsync(ct);
        _cache.Set("service_sessions", data, TimeSpan.FromMinutes(10));
        return data;
    }
}

