using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Currency.Application.Abstractions;
using Currency.Application.Features.RatesLatest;
using Currency.Domain;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class LatestRatesHandler_Happy_Tests
{
    [Fact]
    public async Task Returns_filtered_rates_and_metadata()
    {
        var provider = Substitute.For<IExchangeRateProvider>();
        provider
            .GetRatesAsync("USD", Arg.Any<CancellationToken>())
            .Returns(
                new RatesSnapshot(
                    "USD",
                    new DateOnly(2025, 10, 1),
                    new Dictionary<string, decimal> { ["EUR"] = 0.9m, ["TRY"] = 30m }
                )
            );

        var sut = new LatestRatesHandler(provider);

        var res = await sut.Handle(new LatestRatesQuery("USD"), CancellationToken.None);

        res.Base.Should().Be("USD");
        res.Date.Should().Be(new DateOnly(2025, 10, 1));
        res.Rates.Should().ContainKey("EUR");
        res.Rates.Should().NotContainKey("TRY");
    }
}
