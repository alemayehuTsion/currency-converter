using Currency.Application.Abstractions;
using Currency.Domain;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Currency.Infrastructure.Caching;

internal sealed class CachedExchangeRateProvider : IExchangeRateProvider
{
    private readonly IExchangeRateProvider _inner;
    private readonly IMemoryCache _cache;
    private readonly RateCacheOptions _opts;

    public CachedExchangeRateProvider(
        IExchangeRateProvider inner,
        IMemoryCache cache,
        IOptions<RateCacheOptions> opts
    )
    {
        _inner = inner;
        _cache = cache;
        _opts = opts.Value;
    }

    public async Task<RatesSnapshot> GetRatesAsync(string baseCurrency, CancellationToken ct)
    {
        var b = Normalize(baseCurrency);
        var latestKey = LatestKey(b);

        if (_cache.TryGetValue(latestKey, out RatesSnapshot cached))
            return cached;

        var snap = await _inner.GetRatesAsync(b, ct);

        // cache latest
        _cache.Set(
            latestKey,
            snap,
            new MemoryCacheEntryOptions().SetAbsoluteExpiration(
                TimeSpan.FromSeconds(_opts.LatestTtlSeconds)
            )
        );

        // seed the daily cache with date
        var dayKey = DayKey(b, snap.Date);
        _cache.Set(
            dayKey,
            snap,
            new MemoryCacheEntryOptions().SetAbsoluteExpiration(
                TimeSpan.FromHours(_opts.DailyTtlHours)
            )
        );

        return snap;
    }

    public async Task<IReadOnlyList<RatesSnapshot>> GetHistoryAsync(
        string baseCurrency,
        DateOnly from,
        DateOnly to,
        CancellationToken ct
    )
    {
        var b = Normalize(baseCurrency);
        if (from > to)
            throw new InvalidOperationException("'from' date must be on or before 'to' date.");

        // try build from cache
        var days = EachDay(from, to).ToArray();
        var hits = new List<RatesSnapshot>(capacity: days.Length);
        var missingDates = new List<DateOnly>();

        foreach (var d in days)
        {
            if (_cache.TryGetValue(DayKey(b, d), out RatesSnapshot? snap) && snap != null)
                hits.Add(snap);
            else
                missingDates.Add(d);
        }

        // if we’re missing anything, fetch a batched range from the inner provider
        if (missingDates.Count > 0)
        {
            var min = missingDates.Min();
            var max = missingDates.Max();

            var fetched = await _inner.GetHistoryAsync(b, min, max, ct);

            foreach (var s in fetched)
            {
                var k = DayKey(b, s.Date);
                _cache.Set(
                    k,
                    s,
                    new MemoryCacheEntryOptions().SetAbsoluteExpiration(
                        TimeSpan.FromHours(_opts.DailyTtlHours)
                    )
                );
            }

            // re-fill from cache to ensure we have the full requested span
            hits.Clear();
            foreach (var d in days)
            {
                if (_cache.TryGetValue(DayKey(b, d), out RatesSnapshot? snap2) && snap2 != null)
                    hits.Add(snap2);
            }
        }

        // stable order (desc)
        hits.Sort((a, b2) => b2.Date.CompareTo(a.Date));
        return hits;
    }

    private static string Normalize(string c) =>
        string.IsNullOrWhiteSpace(c) ? "EUR" : c.Trim().ToUpperInvariant();

    private static IEnumerable<DateOnly> EachDay(DateOnly from, DateOnly to)
    {
        for (var d = from; d <= to; d = d.AddDays(1))
            yield return d;
    }

    private static string LatestKey(string b) => $"rates:latest:{b}";

    private static string DayKey(string b, DateOnly d) => $"rates:day:{b}:{d:yyyy-MM-dd}";
}
