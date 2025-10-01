using System;
// Use your real namespace:
using Currency.Application.Features.Convert;
using FluentAssertions;
using Xunit;

namespace Currency.UnitTests.Handlers.Convert;

public class ConvertCurrencyResult_Getters_Tests
{
    [Fact]
    public void Getters_Are_Accessible()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var r = new ConvertCurrencyResult("USD", "ETB", 10m, 57m, 570m, today);

        r.From.Should().Be("USD");
        r.To.Should().Be("ETB");
        r.Amount.Should().Be(10m);
        r.Rate.Should().Be(57m);
        r.ConvertedAmount.Should().Be(570m);
        r.Date.Should().Be(today);
    }
}
