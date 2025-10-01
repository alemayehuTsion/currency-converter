using System.Text.Json;
using System.Text.Json.Serialization;
using Currency.Application.Abstractions;
using Currency.Domain;

namespace Currency.Infrastructure.Providers.Frankfurter;

internal sealed class FrankfurterRateProvider : IExchangeRateProvider
{
    private readonly FrankfurterClient _client;

    public FrankfurterRateProvider(FrankfurterClient client) => _client = client;

    public async Task<RatesSnapshot> GetRatesAsync(string baseCurrency, CancellationToken ct)
    {
        var b = string.IsNullOrWhiteSpace(baseCurrency)
            ? "EUR"
            : baseCurrency.Trim().ToUpperInvariant();

        using var resp = await _client.GetLatestAsync(b, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Frankfurter returned {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {err}"
            );
        }

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var dto =
            await JsonSerializer.DeserializeAsync<FrankfurterDto>(stream, FrankfurterDto.Json, ct)
            ?? throw new InvalidOperationException("Empty response from Frankfurter.");

        if (
            string.IsNullOrWhiteSpace(dto.Base)
            || string.IsNullOrWhiteSpace(dto.Date)
            || dto.Rates is null
        )
            throw new InvalidOperationException(
                "Invalid response from Frankfurter (missing fields)."
            );

        if (!DateOnly.TryParse(dto.Date, out var d))
            throw new InvalidOperationException($"Invalid date '{dto.Date}' from Frankfurter.");

        var rates = new Dictionary<string, decimal>(dto.Rates, StringComparer.OrdinalIgnoreCase);

        return new RatesSnapshot(dto.Base.ToUpperInvariant(), d, rates);
    }

    private sealed class FrankfurterDto
    {
        [JsonPropertyName("base")]
        public string Base { get; set; } = "";

        [JsonPropertyName("date")]
        public string Date { get; set; } = "";

        [JsonPropertyName("rates")]
        public Dictionary<string, decimal> Rates { get; set; } = new();

        public static readonly JsonSerializerOptions Json = new()
        {
            PropertyNameCaseInsensitive = true,
        };
    }
}
