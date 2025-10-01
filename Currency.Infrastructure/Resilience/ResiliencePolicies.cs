using System.Net;
using Currency.Infrastructure.Providers.Frankfurter;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace Currency.Infrastructure.Resilience;

internal static class ResiliencePolicies
{
    public static IAsyncPolicy<HttpResponseMessage> Create(ILogger logger, FrankfurterOptions opts)
    {
        // per-try timeout (fast-fail each attempt)
        var perTryTimeout = Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromMilliseconds(opts.PerTryTimeoutMs),
            TimeoutStrategy.Optimistic
        );

        var transient = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r =>
                r.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
            );

        var retry = transient.WaitAndRetryAsync(
            retryCount: opts.Retry.MaxRetries,
            sleepDurationProvider: attempt =>
            {
                var backoff = TimeSpan.FromMilliseconds(
                    opts.Retry.MedianFirstDelayMs * Math.Pow(2, attempt - 1)
                );
                var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100));
                return backoff + jitter;
            },
            onRetry: (outcome, delay, attempt, _) =>
            {
                try
                {
                    var reason =
                        outcome.Exception?.GetType().Name
                        ?? outcome.Result?.StatusCode.ToString()
                        ?? "unknown";
                    logger.LogWarning(
                        "Frankfurter retry {Attempt} after {Delay} (reason: {Reason})",
                        attempt,
                        delay,
                        reason
                    );
                }
                catch { }
            }
        );

        var breaker = transient.AdvancedCircuitBreakerAsync(
            failureThreshold: opts.Breaker.FailureThreshold,
            samplingDuration: TimeSpan.FromSeconds(opts.Breaker.SamplingDurationSeconds),
            minimumThroughput: opts.Breaker.MinimumThroughput,
            durationOfBreak: TimeSpan.FromSeconds(opts.Breaker.BreakDurationSeconds)
        );

        // per-try timeout → retry → breaker
        return Policy.WrapAsync(breaker, retry, perTryTimeout);
    }
}
