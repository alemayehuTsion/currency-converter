using System.Net;
using System.Net.Http.Json;
using Currency.Application.Features.Convert;
using Currency.Application.Features.RatesLatest;
using Currency.Application.Features.RatesHistory;
using FluentAssertions;
using Xunit;

public class ApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public ApiTests(CustomWebApplicationFactory factory) => _factory = factory;

    private HttpClient ClientWithRoles(string rolesCsv = "reader,converter")
    {
        var c = _factory.CreateClient(new() { AllowAutoRedirect = false });
        c.DefaultRequestHeaders.Add("X-Test-Roles", rolesCsv);
        return c;
    }

    [Fact]
    public async Task Latest_requires_reader_and_returns_filtered_rates()
    {
        var client = ClientWithRoles("reader");

        var res = await client.GetAsync("/api/v1/rates/latest?base=USD");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<LatestRatesResponse>();
        body!.Base.Should().Be("USD");
        body.Rates.Should().ContainKey("EUR");
        body.Rates.Should().NotContainKey("TRY"); // excluded by handler
    }

    [Fact]
    public async Task Convert_requires_converter_role()
    {
        var readerOnly = ClientWithRoles("reader");
        var payload = JsonContent.Create(new ConvertCurrencyCommand("USD", "EUR", 100m));

        var res1 = await readerOnly.PostAsync("/api/v1/convert", payload);
        res1.StatusCode.Should().Be(HttpStatusCode.Forbidden); // 403

        var converter = ClientWithRoles("converter");
        var res2 = await converter.PostAsync("/api/v1/convert", payload);
        res2.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res2.Content.ReadFromJsonAsync<ConvertCurrencyResult>();
        body!.ConvertedAmount.Should().Be(90m); // 100 * 0.9 (from stub)
    }

    [Fact]
    public async Task History_paginates_desc_by_date()
    {
        var client = ClientWithRoles("reader");
        var res = await client.GetAsync("/api/v1/rates/history?base=USD&from=2025-09-28&to=2025-10-01&page=1&pageSize=2");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<HistoryResponse>();
        body!.Page.Should().Be(1);
        body.PageSize.Should().Be(2);
        body.Total.Should().Be(4);
        body.Items.Should().HaveCount(2);
        body.Items[0].Date.Should().Be(new DateOnly(2025,10,1)); // desc sort
        body.Items[1].Date.Should().Be(new DateOnly(2025,9,30));
    }
}
