using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Currency.Application.Abstractions;
using Currency.Application.Features.Convert;
using Currency.Domain;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class ConvertCurrencyHandler_Unsupported_Tests
{
    [Fact]
    public async Task Trims_and_uppercases_currency_codes_before_lookup()
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
            new ConvertCurrencyCommand("  usd ", " eur ", 10m),
            CancellationToken.None
        );

        res.From.Should().Be("USD");
        res.To.Should().Be("EUR");
        res.ConvertedAmount.Should().Be(9m);
    }
}
