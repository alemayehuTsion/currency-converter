using Currency.Application.Abstractions;
using Currency.Domain;
using MediatR;

namespace Currency.Application.Features.RatesHistory;

public sealed class HistoryHandler : IRequestHandler<HistoryQuery, HistoryResponse>
{
    private readonly IExchangeRateProvider _rates;

    public HistoryHandler(IExchangeRateProvider rates) => _rates = rates;

    public async Task<HistoryResponse> Handle(HistoryQuery request, CancellationToken ct)
    {
        var b = request.BaseCurrency.Trim().ToUpperInvariant();

        var snapshots = await _rates.GetHistoryAsync(b, request.From, request.To, ct);

        // strip excluded currencies from each day's map
        var cleaned = snapshots
            .Select(s => new DailyRates(
                s.Date,
                s.Rates.Where(kvp => !CurrencyRules.Excluded.Contains(kvp.Key))
                    .ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase)
            ))
            .ToList();

        var total = cleaned.Count;
        var skip = (request.Page - 1) * request.PageSize;
        var items = cleaned.Skip(skip).Take(request.PageSize).ToList();

        return new HistoryResponse(request.Page, request.PageSize, total, items);
    }
}
