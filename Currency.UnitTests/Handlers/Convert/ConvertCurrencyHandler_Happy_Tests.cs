using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Currency.Application.Abstractions;
using Currency.Application.Features.Convert;
using Currency.Domain;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class ConvertCurrencyHandler_Happy_Tests
{
    [Fact]
    public async Task Converts_using_latest_rate()
    {
        var provider = Substitute.For<IExchangeRateProvider>();
        provider
            .GetRatesAsync("USD", Arg.Any<CancellationToken>())
            .Returns(
                new RatesSnapshot(
                    "USD",
                    new DateOnly(2025, 10, 1),
                    new Dictionary<string, decimal> { ["EUR"] = 0.9m }
                )
            );

        var sut = new ConvertCurrencyHandler(provider);

        var res = await sut.Handle(
            new ConvertCurrencyCommand("usd", "eur", 100m),
            CancellationToken.None
        );

        res.From.Should().Be("USD");
        res.To.Should().Be("EUR");
        res.Rate.Should().Be(0.9m);
        res.ConvertedAmount.Should().Be(90m);
        res.Date.Should().Be(new DateOnly(2025, 10, 1));
    }

    [Fact]
    public async Task Same_currency_short_circuits_with_rate_1()
    {
        var provider = Substitute.For<IExchangeRateProvider>();
        var sut = new ConvertCurrencyHandler(provider);

        var res = await sut.Handle(
            new ConvertCurrencyCommand("EUR", "EUR", 42m),
            CancellationToken.None
        );

        res.Rate.Should().Be(1m);
        res.ConvertedAmount.Should().Be(42m);
    }
}
