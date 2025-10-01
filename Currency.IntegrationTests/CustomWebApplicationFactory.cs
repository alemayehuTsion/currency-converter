using Currency.Api; // ensure your test project references Currency.Api
using Currency.Application.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Replace real auth with TestAuth
            services
                .AddAuthentication(TestAuthHandler.Scheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.Scheme,
                    _ => { }
                );
            services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(o =>
            {
                o.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                o.DefaultChallengeScheme = TestAuthHandler.Scheme;
            });

            // Replace IExchangeRateProvider with stub
            services.RemoveAll(typeof(IExchangeRateProvider));
            services.AddSingleton<IExchangeRateProvider>(
                new StubExchangeRateProvider(
                    new DateOnly(2025, 10, 1),
                    new Dictionary<string, decimal>
                    {
                        ["EUR"] = 0.9m,
                        ["JPY"] = 159.2m,
                        ["TRY"] = 30m,
                    } // TRY will be filtered
                )
            );
        });
    }
}
