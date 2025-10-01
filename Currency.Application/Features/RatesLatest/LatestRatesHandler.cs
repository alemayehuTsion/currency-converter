using Currency.Application.Abstractions;
using Currency.Domain;
using MediatR;

namespace Currency.Application.Features.RatesLatest;

public sealed class LatestRatesHandler : IRequestHandler<LatestRatesQuery, LatestRatesResponse>
{
    private readonly IExchangeRateProvider _rates;

    public LatestRatesHandler(IExchangeRateProvider rates) => _rates = rates;

    public async Task<LatestRatesResponse> Handle(LatestRatesQuery request, CancellationToken ct)
    {
        var b = request.BaseCurrency.Trim().ToUpperInvariant();

        var snapshot = await _rates.GetRatesAsync(b, ct);

        var filtered = snapshot
            .Rates.Where(kvp => !CurrencyRules.Excluded.Contains(kvp.Key))
            .ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);

        return new LatestRatesResponse(snapshot.Base, snapshot.Date, filtered);
    }
}
