using System.Net.Http.Headers;
using Currency.Infrastructure.Providers.Frankfurter;
using Currency.Infrastructure.Resilience;
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
        services.AddScoped<
            Application.Abstractions.IExchangeRateProvider,
            FrankfurterRateProvider
        >();

        return services;
    }
}
