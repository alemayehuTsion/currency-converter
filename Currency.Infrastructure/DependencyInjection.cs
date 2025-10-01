using System.Net.Http.Headers;
using Currency.Infrastructure.Caching;
using Currency.Infrastructure.Providers.Frankfurter;
using Currency.Infrastructure.Resilience;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Currency.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config
    )
    {
        services.Configure<FrankfurterOptions>(config.GetSection("Frankfurter"));
        services.Configure<RateCacheOptions>(config.GetSection("RateCache"));

        services
            .AddHttpClient<FrankfurterClient>(
                (sp, http) =>
                {
                    var opts = sp.GetRequiredService<IOptions<FrankfurterOptions>>().Value;

                    http.BaseAddress = new Uri(opts.BaseUrl);
                    http.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
                    http.DefaultRequestHeaders.UserAgent.Add(
                        new ProductInfoHeaderValue("CurrencyConverter", "1.0")
                    );
                }
            )
            .AddPolicyHandler(
                (sp, _) =>
                {
                    var logger = sp.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("Polly.Frankfurter");
                    var opts = sp.GetRequiredService<IOptions<FrankfurterOptions>>().Value;
                    return ResiliencePolicies.Create(logger, opts);
                }
            );

        services.AddScoped<FrankfurterRateProvider>();
        services.AddScoped<Application.Abstractions.IExchangeRateProvider>(sp =>
        {
            var inner = sp.GetRequiredService<FrankfurterRateProvider>();
            var cache = sp.GetRequiredService<IMemoryCache>();
            var opts = sp.GetRequiredService<IOptions<RateCacheOptions>>();
            return new CachedExchangeRateProvider(inner, cache, opts);
        });

        return services;
    }
}
