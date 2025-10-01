using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Currency.Application.Abstractions;
using Currency.Application.Features.RatesHistory;
using Currency.Domain;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class HistoryHandler_Bounds_Tests
{
    [Fact]
    public async Task Page_beyond_total_returns_empty_items()
    {
        var provider = Substitute.For<IExchangeRateProvider>();
        var baseCur = "USD";

        var snapshots = new List<RatesSnapshot>
        {
            new(
                baseCur,
                new DateOnly(2025, 10, 01),
                new Dictionary<string, decimal> { ["EUR"] = 0.9m }
            ),
            new(
                baseCur,
                new DateOnly(2025, 09, 30),
                new Dictionary<string, decimal> { ["EUR"] = 0.9m }
            ),
        };

        provider
            .GetHistoryAsync(
                baseCur,
                Arg.Any<DateOnly>(),
                Arg.Any<DateOnly>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(snapshots);

        var sut = new HistoryHandler(provider);

        var res = await sut.Handle(
            new HistoryQuery(baseCur, new DateOnly(2025, 09, 30), new DateOnly(2025, 10, 01), 3, 2),
            CancellationToken.None
        );

        res.Total.Should().Be(2);
        res.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task PageSize_zero_returns_empty_items()
    {
        var provider = Substitute.For<IExchangeRateProvider>();
        var baseCur = "USD";

        var snapshots = new List<RatesSnapshot>
        {
            new(
                baseCur,
                new DateOnly(2025, 10, 01),
                new Dictionary<string, decimal> { ["EUR"] = 0.9m }
            ),
            new(
                baseCur,
                new DateOnly(2025, 09, 30),
                new Dictionary<string, decimal> { ["EUR"] = 0.9m }
            ),
        };

        provider
            .GetHistoryAsync(
                baseCur,
                Arg.Any<DateOnly>(),
                Arg.Any<DateOnly>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(snapshots);

        var sut = new HistoryHandler(provider);

        var res = await sut.Handle(
            new HistoryQuery(baseCur, new DateOnly(2025, 09, 30), new DateOnly(2025, 10, 01), 1, 0),
            CancellationToken.None
        );

        res.Total.Should().Be(2);
        res.Items.Should().BeEmpty();
    }
}
