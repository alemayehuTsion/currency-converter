using FluentValidation;

namespace Currency.Application.Features.Convert;

public sealed class ConvertCurrencyCommandValidator : AbstractValidator<ConvertCurrencyCommand>
{
    private static readonly HashSet<string> Excluded = new(StringComparer.OrdinalIgnoreCase)
    {
        "TRY",
        "PLN",
        "THB",
        "MXN",
    };

    public ConvertCurrencyCommandValidator()
    {
        RuleFor(x => x.From)
            .NotEmpty()
            .Length(3)
            .Must(c => !Excluded.Contains(c))
            .WithMessage("From currency is excluded.");

        RuleFor(x => x.To)
            .NotEmpty()
            .Length(3)
            .Must(c => !Excluded.Contains(c))
            .WithMessage("To currency is excluded.");

        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
    }
}
