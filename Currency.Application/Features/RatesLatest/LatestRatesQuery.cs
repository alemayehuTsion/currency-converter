using MediatR;

namespace Currency.Application.Features.RatesLatest;

public sealed record LatestRatesQuery(string BaseCurrency) : IRequest<LatestRatesResponse>;

public sealed record LatestRatesResponse(
    string Base,
    DateOnly Date,
    IDictionary<string, decimal> Rates
);
