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

    public async Task<IReadOnlyList<RatesSnapshot>> GetHistoryAsync(
        string baseCurrency,
        DateOnly from,
        DateOnly to,
        CancellationToken ct
    )
    {
        var b = string.IsNullOrWhiteSpace(baseCurrency)
            ? "EUR"
            : baseCurrency.Trim().ToUpperInvariant();

        if (from > to)
            throw new InvalidOperationException("'from' date must be on or before 'to' date.");

        var url = $"{from:yyyy-MM-dd}..{to:yyyy-MM-dd}?base={Uri.EscapeDataString(b)}";
        using var resp = await _client.RawGetAsync(url, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Frankfurter returned {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {err}"
            );
        }

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var dto =
            await JsonSerializer.DeserializeAsync<HistoryDto>(stream, HistoryDto.Json, ct)
            ?? throw new InvalidOperationException("Empty response from Frankfurter.");

        if (string.IsNullOrWhiteSpace(dto.Base) || dto.Rates is null)
            throw new InvalidOperationException("Invalid history response from Frankfurter.");

        var list = new List<RatesSnapshot>(dto.Rates.Count);
        foreach (var kvp in dto.Rates)
        {
            if (!DateOnly.TryParse(kvp.Key, out var day))
                continue; // skip malformed dates gracefully

            var rates = new Dictionary<string, decimal>(
                kvp.Value,
                StringComparer.OrdinalIgnoreCase
            );
            list.Add(new RatesSnapshot(dto.Base.ToUpperInvariant(), day, rates));
        }

        // sort by date DESC so paging is stable for clients
        list.Sort((a, b2) => b2.Date.CompareTo(a.Date));
        return list;
    }

    private sealed class HistoryDto
    {
        [JsonPropertyName("base")]
        public string Base { get; set; } = "";

        [JsonPropertyName("start_date")]
        public string? Start { get; set; }

        [JsonPropertyName("end_date")]
        public string? End { get; set; }

        [JsonPropertyName("rates")]
        public Dictionary<string, Dictionary<string, decimal>> Rates { get; set; } = new();

        public static readonly JsonSerializerOptions Json = new()
        {
            PropertyNameCaseInsensitive = true,
        };
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
