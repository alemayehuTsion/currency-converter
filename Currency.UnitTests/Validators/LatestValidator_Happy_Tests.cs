using Currency.Application.Features.RatesLatest;
using FluentAssertions;
using Xunit;

public class LatestValidator_Happy_Tests
{
    [Fact]
    public void Accepts_valid_code()
    {
        var v = new LatestRatesQueryValidator();
        v.Validate(new LatestRatesQuery("USD")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Accepts_lowercase_code()
    {
        var v = new LatestRatesQueryValidator();
        v.Validate(new LatestRatesQuery("usd")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Accepts_mixedcase_code()
    {
        var v = new LatestRatesQueryValidator();
        v.Validate(new LatestRatesQuery("uSd")).IsValid.Should().BeTrue();
    }
}
