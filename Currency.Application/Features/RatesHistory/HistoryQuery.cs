using MediatR;

namespace Currency.Application.Features.RatesHistory;

public sealed record HistoryQuery(
    string BaseCurrency,
    DateOnly From,
    DateOnly To,
    int Page = 1,
    int PageSize = 30
) : IRequest<HistoryResponse>;

public sealed record HistoryResponse(
    int Page,
    int PageSize,
    int Total,
    IReadOnlyList<DailyRates> Items
);

public sealed record DailyRates(DateOnly Date, IDictionary<string, decimal> Rates);
