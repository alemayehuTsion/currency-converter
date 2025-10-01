using Currency.Domain;
using FluentValidation;

namespace Currency.Application.Features.RatesLatest;

public sealed class LatestRatesQueryValidator : AbstractValidator<LatestRatesQuery>
{
    public LatestRatesQueryValidator()
    {
        RuleFor(x => x.BaseCurrency)
            .NotEmpty()
            .Length(3)
            .Must(c => !CurrencyRules.Excluded.Contains(c))
            .WithMessage("Base currency is excluded.");
    }
}
