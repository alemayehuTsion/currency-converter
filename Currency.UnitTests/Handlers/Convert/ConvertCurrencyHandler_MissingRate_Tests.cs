using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Currency.Application.Abstractions;
using Currency.Application.Features.Convert;
using Currency.Domain;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class ConvertCurrencyHandler_MissingRate_Tests
{
    [Fact]
    public async Task Throws_when_target_currency_not_present_in_snapshot()
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
            ); // no GBP

        var sut = new ConvertCurrencyHandler(provider);

        var act = async () =>
            await sut.Handle(
                new ConvertCurrencyCommand("USD", "GBP", 100m),
                CancellationToken.None
            );

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("No rate available from USD to GBP*");
    }
    
}
