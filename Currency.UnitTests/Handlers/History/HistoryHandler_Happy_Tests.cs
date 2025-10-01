using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Currency.Application.Abstractions;
using Currency.Application.Features.RatesHistory;
using Currency.Domain;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class HistoryHandler_Happy_Tests
{
    [Fact]
    public async Task Filters_excluded_and_paginates_desc()
    {
        var provider = Substitute.For<IExchangeRateProvider>();
        var baseCur = "USD";

        var dayRates = new Dictionary<string, decimal>
        {
            ["EUR"] = 0.9m,
            ["JPY"] = 159.2m,
            ["TRY"] = 30m, // excluded
        };

        var snapshots = new List<RatesSnapshot>
        {
            new(baseCur, new DateOnly(2025, 10, 01), new Dictionary<string, decimal>(dayRates)),
            new(baseCur, new DateOnly(2025, 09, 30), new Dictionary<string, decimal>(dayRates)),
            new(baseCur, new DateOnly(2025, 09, 29), new Dictionary<string, decimal>(dayRates)),
            new(baseCur, new DateOnly(2025, 09, 28), new Dictionary<string, decimal>(dayRates)),
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
            new HistoryQuery(baseCur, new DateOnly(2025, 09, 28), new DateOnly(2025, 10, 01), 1, 2),
            CancellationToken.None
        );

        res.Total.Should().Be(4);
        res.Items.Should().HaveCount(2);
        res.Items[0].Date.Should().Be(new DateOnly(2025, 10, 01));
        res.Items[1].Date.Should().Be(new DateOnly(2025, 09, 30));
        res.Items.Should().OnlyContain(d => !d.Rates.ContainsKey("TRY"));
    }

    [Fact]
    public async Task Handles_no_data_gracefully()
    {
        var provider = Substitute.For<IExchangeRateProvider>();
        var baseCur = "USD";

        provider
            .GetHistoryAsync(
                baseCur,
                Arg.Any<DateOnly>(),
                Arg.Any<DateOnly>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                Task.FromResult(
                    (IReadOnlyList<RatesSnapshot>)Enumerable.Empty<RatesSnapshot>().ToList()
                )
            );

        var sut = new HistoryHandler(provider);

        var res = await sut.Handle(
            new HistoryQuery(baseCur, new DateOnly(2025, 09, 28), new DateOnly(2025, 10, 01), 1, 2),
            CancellationToken.None
        );

        res.Total.Should().Be(0);
        res.Items.Should().BeEmpty();
    }
}
