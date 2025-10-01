using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
// Use your real namespaces:
using Currency.Application.Abstractions;
using Currency.Application.Features.Convert;
using Currency.Domain;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Currency.UnitTests.Handlers.Convert;

public class ConvertCurrencyHandler_ProviderError_Tests
{
    [Fact]
    public async Task Provider_Throws_Is_Propagated_Or_Wrapped()
    {
        var provider = Substitute.For<IExchangeRateProvider>();
        provider
            .GetRatesAsync("USD", Arg.Any<CancellationToken>())
            .Returns<Task<RatesSnapshot>>(_ => throw new TimeoutException("boom"));

        var handler = new ConvertCurrencyHandler(provider);

        var act = async () =>
            await handler.Handle(new ConvertCurrencyCommand("USD", "ETB", 10m), default);
        await act.Should().ThrowAsync<TimeoutException>(); // or your wrapper type if you wrap it
    }

    [Fact]
    public async Task Base_Mismatch_Is_Accepted_And_Conversion_Succeeds()
    {
        // provider returns EUR base though we asked USD
        var snap = new RatesSnapshot(
            Base: "EUR",
            Date: DateOnly.FromDateTime(DateTime.Today),
            Rates: new Dictionary<string, decimal> { ["ETB"] = 57m }
        );

        var provider = Substitute.For<IExchangeRateProvider>();
        provider.GetRatesAsync("USD", Arg.Any<CancellationToken>()).Returns(Task.FromResult(snap));

        var handler = new ConvertCurrencyHandler(provider);

        var res = await handler.Handle(new ConvertCurrencyCommand("USD", "ETB", 10m), default);

        // Expect success (match your real result type/properties)
        res.ConvertedAmount.Should().Be(570m);
        res.To.Should().Be("ETB");
    }
}
