namespace Currency.Infrastructure.Providers.Frankfurter;

public sealed class FrankfurterOptions
{
    public string BaseUrl { get; init; } = "https://api.frankfurter.app/";
    public int TimeoutSeconds { get; init; } = 10;

    // Polly
    public int PerTryTimeoutMs { get; init; } = 3000;

    public RetryOptions Retry { get; init; } = new();
    public BreakerOptions Breaker { get; init; } = new();

    public sealed class RetryOptions
    {
        public int MaxRetries { get; init; } = 3;
        public int MedianFirstDelayMs { get; init; } = 200; // backoff start
    }

    public sealed class BreakerOptions
    {
        public double FailureThreshold { get; init; } = 0.5; // 50% fail rate
        public int SamplingDurationSeconds { get; init; } = 30;
        public int MinimumThroughput { get; init; } = 10;
        public int BreakDurationSeconds { get; init; } = 30;
    }
}
