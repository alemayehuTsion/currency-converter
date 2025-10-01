namespace Currency.Domain;

public sealed record RatesSnapshot(
    string Base,
    DateOnly Date,
    IReadOnlyDictionary<string, decimal> Rates
);
