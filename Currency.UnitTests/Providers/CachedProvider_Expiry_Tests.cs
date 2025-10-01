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

public class CachedProvider_Expiry_Tests
{
    [Fact]
    public async Task Latest_expires_after_ttl_and_rehits_inner()
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
        // Tiny TTL so we can observe expiry in the test
        var opts = Options.Create(
            new RateCacheOptions { LatestTtlSeconds = 1, DailyTtlHours = 24 }
        );

        var sut = new CachedExchangeRateProvider(inner, cache, opts);

        await sut.GetRatesAsync(baseCur, CancellationToken.None);
        inner.LatestCalls.Should().Be(1);

        // wait past TTL
        await Task.Delay(1100);

        await sut.GetRatesAsync(baseCur, CancellationToken.None);
        inner.LatestCalls.Should().Be(2); // re-fetched after expiry
    }

    // [Fact]
    // public async Task Daily_expired_ttl_refetches_on_next_call()
    // {
    //     var today = new DateOnly(2025, 10, 1);
    //     var yesterday = today.AddDays(-1);
    //     var baseCur = "USD";

    //     var daily = new RatesSnapshot(
    //         baseCur,
    //         yesterday,
    //         new Dictionary<string, decimal> { ["EUR"] = 0.9m }
    //     );

    //     var inner = new CountingProvider(
    //         new RatesSnapshot("", new DateOnly(), new Dictionary<string, decimal>()),
    //         new List<RatesSnapshot> { daily }
    //     );

    //     var cache = new MemoryCache(new MemoryCacheOptions());
    //     // TTL 0h => entry expires immediately after set
    //     var opts = Options.Create(
    //         new RateCacheOptions { LatestTtlSeconds = 60, DailyTtlHours = 1 }
    //     );

    //     var sut = new CachedExchangeRateProvider(inner, cache, opts);

    //     await sut.GetHistoryAsync(baseCur, yesterday, yesterday, CancellationToken.None);
    //     inner.HistoryCalls.Should().Be(1);

    //     // immediately refetches because the cached entry expired
    //     await sut.GetHistoryAsync(baseCur, yesterday, yesterday, CancellationToken.None);
    //     inner.HistoryCalls.Should().Be(2);
    // }
}
