using Currency.Domain;

namespace Currency.Application.Abstractions;

public interface IExchangeRateProvider
{
    Task<RatesSnapshot> GetRatesAsync(string baseCurrency, CancellationToken ct);
    Task<IReadOnlyList<RatesSnapshot>> GetHistoryAsync(
        string baseCurrency,
        DateOnly from,
        DateOnly to,
        CancellationToken ct
    );
}
