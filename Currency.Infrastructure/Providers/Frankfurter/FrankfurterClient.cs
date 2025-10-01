using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Currency.Infrastructure.Providers.Frankfurter;

public sealed class FrankfurterClient
{
    private readonly HttpClient _http;
    private readonly ILogger<FrankfurterClient> _log;

    public FrankfurterClient(HttpClient http, ILogger<FrankfurterClient> log)
    {
        _http = http;
        _log = log;
    }

    public Task<HttpResponseMessage> GetLatestAsync(string baseCurrency, CancellationToken ct)
    {
        var b = string.IsNullOrWhiteSpace(baseCurrency) ? "EUR" : baseCurrency.Trim().ToUpperInvariant();
        var url = $"latest?base={Uri.EscapeDataString(b)}";
        _log.LogDebug("Frankfurter GET {Url}", url);
        return _http.GetAsync(url, ct);
    }
}
