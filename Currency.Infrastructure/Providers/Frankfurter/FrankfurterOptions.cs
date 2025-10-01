namespace Currency.Infrastructure.Providers.Frankfurter;

public sealed class FrankfurterOptions
{
    public string BaseUrl { get; init; } = "https://api.frankfurter.app/";
    public int TimeoutSeconds { get; init; } = 10;
}
