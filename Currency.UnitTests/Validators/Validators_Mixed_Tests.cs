using Currency.Application.Features.Convert;
using Currency.Application.Features.RatesHistory;
using Currency.Application.Features.RatesLatest;
using FluentAssertions;
using Xunit;

public class Validators_Mixed_Tests
{
    [Fact]
    public void Latest_rejects_excluded_base()
    {
        var v = new LatestRatesQueryValidator();
        v.Validate(new LatestRatesQuery("TRY")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Convert_rejects_excluded_in_from_or_to()
    {
        var v = new ConvertCurrencyCommandValidator();
        v.Validate(new ConvertCurrencyCommand("TRY", "USD", 10m)).IsValid.Should().BeFalse();
        v.Validate(new ConvertCurrencyCommand("USD", "TRY", 10m)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void History_validates_range_and_pageing_rules()
    {
        var v = new HistoryQueryValidator();

        // from must be <= to
        v.Validate(
                new HistoryQuery("USD", new DateOnly(2025, 9, 10), new DateOnly(2025, 9, 1), 1, 10)
            )
            .IsValid.Should()
            .BeFalse();

        // range must be <= 366 days
        v.Validate(
                new HistoryQuery("USD", new DateOnly(2023, 1, 1), new DateOnly(2025, 1, 2), 1, 10)
            )
            .IsValid.Should()
            .BeFalse();

        // happy path
        v.Validate(
                new HistoryQuery("USD", new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 10), 1, 10)
            )
            .IsValid.Should()
            .BeTrue();
    }

    [Fact]
    public void History_rejects_excluded_base()
    {
        var v = new HistoryQueryValidator();
        v.Validate(
                new HistoryQuery("TRY", new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 10), 1, 10)
            )
            .IsValid.Should()
            .BeFalse();
    }

    [Fact]
    public void History_rejects_invalid_paging()
    {
        var v = new HistoryQueryValidator();
        v.Validate(
                new HistoryQuery("USD", new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 10), 0, 10)
            )
            .IsValid.Should()
            .BeFalse();
        v.Validate(
                new HistoryQuery("USD", new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 10), 1, 0)
            )
            .IsValid.Should()
            .BeFalse();
    }
}
