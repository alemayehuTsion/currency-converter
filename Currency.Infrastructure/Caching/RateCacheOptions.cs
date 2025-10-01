namespace Currency.Infrastructure.Caching;

public sealed class RateCacheOptions
{
    // 60s: good enough to avoid hammering, still “fresh”
    public int LatestTtlSeconds { get; init; } = 60;

    // 24h: historical daily snapshots don’t change often
    public int DailyTtlHours { get; init; } = 24;
}
