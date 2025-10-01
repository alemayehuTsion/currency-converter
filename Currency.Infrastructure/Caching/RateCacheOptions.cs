namespace Currency.Infrastructure.Caching;

public sealed class RateCacheOptions
{
    // 60s: can be a bit stale, but should be fresh enough for most use cases
    public int LatestTtlSeconds { get; init; } = 60;

    // 24h: historical daily snapshots don’t change often
    public int DailyTtlHours { get; init; } = 24;
}
