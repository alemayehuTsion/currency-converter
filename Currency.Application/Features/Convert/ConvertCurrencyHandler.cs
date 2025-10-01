using Currency.Application.Abstractions;
using MediatR;

namespace Currency.Application.Features.Convert;

public sealed class ConvertCurrencyHandler
    : IRequestHandler<ConvertCurrencyCommand, ConvertCurrencyResult>
{
    private readonly IExchangeRateProvider _rates;

    public ConvertCurrencyHandler(IExchangeRateProvider rates) => _rates = rates;

    public async Task<ConvertCurrencyResult> Handle(
        ConvertCurrencyCommand req,
        CancellationToken ct
    )
    {
        var from = req.From.Trim().ToUpperInvariant();
        var to = req.To.Trim().ToUpperInvariant();
        var amt = req.Amount;

        // same currency → short-circuit
        if (from == to)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return new ConvertCurrencyResult(from, to, amt, 1m, amt, today);
        }
        // get latest rates for the "from" currency
        var snapshot = await _rates.GetRatesAsync(from, ct);

        if (!snapshot.Rates.TryGetValue(to, out var rate))
            throw new InvalidOperationException(
                $"No rate available from {from} to {to} on {snapshot.Date:yyyy-MM-dd}."
            );

        var converted = amt * rate;

        return new ConvertCurrencyResult(
            From: from,
            To: to,
            Amount: amt,
            Rate: rate,
            ConvertedAmount: converted,
            Date: snapshot.Date
        );
    }
}
