namespace Currency.Domain;

public static class CurrencyRules
{
    public static readonly HashSet<string> Excluded = new(StringComparer.OrdinalIgnoreCase)
    {
        "TRY",
        "PLN",
        "THB",
        "MXN",
    };
}
