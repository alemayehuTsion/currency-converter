using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Currency.Application.Abstractions;
using Currency.Application.Features.Convert;
using Currency.Domain;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class ConvertCurrencyHandler_Rounding_Tests
{
    [Fact]
    public async Task Preserves_decimal_precision_in_multiplication()
    {
        var provider = Substitute.For<IExchangeRateProvider>();
        provider
            .GetRatesAsync("USD", Arg.Any<CancellationToken>())
            .Returns(
                new RatesSnapshot(
                    "USD",
                    new DateOnly(2025, 10, 1),
                    new Dictionary<string, decimal> { ["EUR"] = 0.87321m }
                )
            );

        var sut = new ConvertCurrencyHandler(provider);

        var res = await sut.Handle(
            new ConvertCurrencyCommand("USD", "EUR", 1.23m),
            CancellationToken.None
        );

        res.ConvertedAmount.Should().Be(1.23m * 0.87321m); // no forced rounding inside handler
    }

    // [Fact]
    // public async Task Rounds_converted_amount_to_4_decimal_places()
    // {
    //     var provider = Substitute.For<IExchangeRateProvider>();
    //     provider
    //         .GetRatesAsync("USD", Arg.Any<CancellationToken>())
    //         .Returns(
    //             new RatesSnapshot(
    //                 "USD",
    //                 new DateOnly(2025, 10, 1),
    //                 new Dictionary<string, decimal> { ["JPY"] = 159.12345m }
    //             )
    //         );

    //     var sut = new ConvertCurrencyHandler(provider);

    //     var res = await sut.Handle(
    //         new ConvertCurrencyCommand("USD", "JPY", 1m),
    //         CancellationToken.None
    //     );

    //     res.Rate.Should().Be(159.12345m);
    //     res.ConvertedAmount.Should().Be(159.1235m);
    // }
}
