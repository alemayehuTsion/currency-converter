using Currency.Domain;
using FluentValidation;

namespace Currency.Application.Features.RatesHistory;

public sealed class HistoryQueryValidator : AbstractValidator<HistoryQuery>
{
    public HistoryQueryValidator()
    {
        RuleFor(x => x.BaseCurrency)
            .NotEmpty()
            .Length(3)
            .Must(c => !CurrencyRules.Excluded.Contains(c))
            .WithMessage("Base currency is excluded.");

        RuleFor(x => x.From)
            .LessThanOrEqualTo(x => x.To)
            .WithMessage("'from' must be on or before 'to'.");

        RuleFor(x => x)
            .Must(x =>
                (
                    x.To.ToDateTime(TimeOnly.MinValue) - x.From.ToDateTime(TimeOnly.MinValue)
                ).TotalDays <= 366
            )
            .WithMessage("Date range cannot exceed 366 days.");

        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
