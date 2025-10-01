using MediatR;

namespace Currency.Application.Features.Convert;

public sealed record ConvertCurrencyCommand(string From, string To, decimal Amount, DateOnly? Date)
    : IRequest<ConvertCurrencyResult>;

public sealed record ConvertCurrencyResult(
    string From,
    string To,
    decimal Amount,
    decimal Rate,
    decimal ConvertedAmount,
    DateOnly Date
);
