using Currency.Application.Abstractions;
using Currency.Domain;

public sealed class StubExchangeRateProvider : IExchangeRateProvider
{
    private readonly Dictionary<string, decimal> _rates;
    private readonly DateOnly _date;

    public StubExchangeRateProvider(DateOnly date, Dictionary<string, decimal> rates)
    {
        _date = date;
        _rates = rates;
    }

    public Task<RatesSnapshot> GetRatesAsync(string baseCurrency, CancellationToken ct) =>
        Task.FromResult(new RatesSnapshot(baseCurrency.ToUpperInvariant(), _date, _rates));

    public Task<IReadOnlyList<RatesSnapshot>> GetHistoryAsync(
        string baseCurrency,
        DateOnly from,
        DateOnly to,
        CancellationToken ct
    )
    {
        var list = new List<RatesSnapshot>();
        for (var d = from; d <= to; d = d.AddDays(1))
            list.Add(new RatesSnapshot(baseCurrency.ToUpperInvariant(), d, _rates));
        list.Sort((a, b) => b.Date.CompareTo(a.Date));
        return Task.FromResult<IReadOnlyList<RatesSnapshot>>(list);
    }
}
