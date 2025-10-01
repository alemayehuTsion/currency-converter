# Currency Converter

A clean, testable .NET 8 service for currency rates and conversion.
- Layers: Api, Application (CQRS), Domain, Infrastructure, Observability.
- Resilience: HttpClientFactory + Polly (retry, circuit breaker).
- Caching: Memory (dev) / Redis (scale).
- AuthZ: JWT w/ roles. 
- Docs: Swagger (dev).

## Getting Started
- Build: `dotnet build CurrencyConverter.sln`
- Run API: `dotnet run --project ./Currency.Api`
- Swagger: `/swagger` (dev only)

## Roadmap
- [ ] Frankfurter provider + Polly policies
- [ ] Convert endpoint (CQRS + FluentValidation)
- [ ] Latest & History endpoints + pagination
- [ ] Caching strategy (latest + daily)
- [ ] JWT + rate limiting
- [ ] Serilog + OTEL traces/metrics
- [ ] Unit & integration tests
