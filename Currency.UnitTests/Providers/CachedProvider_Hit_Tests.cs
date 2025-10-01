using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Currency.Application.Abstractions;
using Currency.Domain;
using Currency.Infrastructure.Caching;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

public sealed class CountingProvider : IExchangeRateProvider
{
    public int LatestCalls;
    public int HistoryCalls;
    private readonly RatesSnapshot _latest;
    private readonly IReadOnlyList<RatesSnapshot> _history;

    public CountingProvider(RatesSnapshot latest, IReadOnlyList<RatesSnapshot> history)
    {
        _latest = latest;
        _history = history;
    }

    public Task<RatesSnapshot> GetRatesAsync(string baseCurrency, CancellationToken ct)
    {
        LatestCalls++;
        return Task.FromResult(_latest);
    }

    public Task<IReadOnlyList<RatesSnapshot>> GetHistoryAsync(
        string baseCurrency,
        DateOnly from,
        DateOnly to,
        CancellationToken ct
    )
    {
        HistoryCalls++;
        return Task.FromResult(_history);
    }
}

public class CachedProvider_Hit_Tests
{
    [Fact]
    public async Task Latest_is_cached_and_daily_seeded()
    {
        var today = new DateOnly(2025, 10, 1);
        var baseCur = "USD";

        var latest = new RatesSnapshot(
            baseCur,
            today,
            new Dictionary<string, decimal> { ["EUR"] = 0.9m }
        );

        var inner = new CountingProvider(latest, new List<RatesSnapshot>());
        var cache = new MemoryCache(new MemoryCacheOptions());
        var opts = Options.Create(
            new RateCacheOptions { LatestTtlSeconds = 60, DailyTtlHours = 24 }
        );

        var sut = new CachedExchangeRateProvider(inner, cache, opts);

        // 1st call hits inner; 2nd call should be served from cache
        var s1 = await sut.GetRatesAsync(baseCur, CancellationToken.None);
        var s2 = await sut.GetRatesAsync(baseCur, CancellationToken.None);

        inner.LatestCalls.Should().Be(1);
        s2.Date.Should().Be(today);

        // daily snapshot seeded
        var dayKey = $"rates:day:{baseCur}:{today:yyyy-MM-dd}";
        cache.TryGetValue(dayKey, out RatesSnapshot seeded).Should().BeTrue();
        seeded.Should().NotBeNull();
    }

    [Fact]
    public async Task Daily_is_cached()
    {
        var today = new DateOnly(2025, 10, 1);
        var yesterday = today.AddDays(-1);
        var baseCur = "USD";

        var daily = new RatesSnapshot(
            baseCur,
            yesterday,
            new Dictionary<string, decimal> { ["EUR"] = 0.9m }
        );

        var inner = new CountingProvider(
            new RatesSnapshot("", new DateOnly(), new Dictionary<string, decimal>()),
            new List<RatesSnapshot> { daily }
        );
        var cache = new MemoryCache(new MemoryCacheOptions());
        var opts = Options.Create(
            new RateCacheOptions { LatestTtlSeconds = 60, DailyTtlHours = 24 }
        );

        var sut = new CachedExchangeRateProvider(inner, cache, opts);

        // 1st call hits inner; 2nd call should be served from cache
        var s1 = await sut.GetHistoryAsync(baseCur, yesterday, yesterday, CancellationToken.None);
        var s2 = await sut.GetHistoryAsync(baseCur, yesterday, yesterday, CancellationToken.None);

        inner.HistoryCalls.Should().Be(1);
        s2.Should().HaveCount(1);
        s2[0].Date.Should().Be(yesterday);
    }
}
