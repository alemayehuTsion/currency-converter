using Currency.Application.Features.Convert;
using FluentAssertions;
using Xunit;

public class ConvertValidator_Amount_Tests
{
    [Fact]
    public void Rejects_negative_amount()
    {
        var v = new ConvertCurrencyCommandValidator();
        v.Validate(new ConvertCurrencyCommand("USD", "EUR", -1m)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Accepts_zero_amount()
    {
        var v = new ConvertCurrencyCommandValidator();
        v.Validate(new ConvertCurrencyCommand("USD", "EUR", 0m)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Accepts_positive_amount()
    {
        var v = new ConvertCurrencyCommandValidator();
        v.Validate(new ConvertCurrencyCommand("USD", "EUR", 1m)).IsValid.Should().BeTrue();
    }
}
