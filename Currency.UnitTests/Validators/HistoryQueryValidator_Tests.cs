using System;
using Currency.Application.Features.RatesHistory;
using Currency.Domain; // Assuming CurrencyRules is here
using FluentValidation.TestHelper;
using Xunit;

public class HistoryQueryValidatorTests
{
    private readonly HistoryQueryValidator _validator;

    public HistoryQueryValidatorTests()
    {
        // Initialize the validator once per test run
        _validator = new HistoryQueryValidator();
    }

    private HistoryQuery GetValidQuery()
    {
        // A standard valid query for testing against
        return new HistoryQuery(
            BaseCurrency: "USD", // Not in CurrencyRules.Excluded
            From: DateOnly.FromDateTime(DateTime.Today.AddDays(-7)),
            To: DateOnly.FromDateTime(DateTime.Today),
            Page: 1,
            PageSize: 50
        );
    }

    // --- Happy Path Test ---
    [Fact]
    public void Should_Pass_For_Valid_Query()
    {
        // Arrange
        var query = GetValidQuery();

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // --- BaseCurrency Validation Tests ---

    [Fact]
    public void Should_Fail_When_BaseCurrency_Is_Empty()
    {
        // Arrange
        var query = GetValidQuery() with
        {
            BaseCurrency = "",
        };

        // Act & Assert
        _validator
            .TestValidate(query)
            .ShouldHaveValidationErrorFor(x => x.BaseCurrency)
            .WithErrorMessage("'Base Currency' must not be empty.");
    }

    [Fact]
    public void Should_Fail_When_BaseCurrency_Length_Is_Incorrect()
    {
        // Arrange: 4 characters
        var query = GetValidQuery() with
        {
            BaseCurrency = "USDD",
        };

        // Act & Assert
        _validator
            .TestValidate(query)
            .ShouldHaveValidationErrorFor(x => x.BaseCurrency)
            // Corrected expected message to use "characters in length"
            .WithErrorMessage(
                "'Base Currency' must be 3 characters in length. You entered 4 characters."
            );
    }

    [Fact]
    public void Should_Fail_When_BaseCurrency_Is_Excluded()
    {
        // NOTE: This test requires knowing a value that is in CurrencyRules.Excluded.
        // Assuming "XCD" is in CurrencyRules.Excluded for this example.
        var excludedCode = CurrencyRules.Excluded.GetEnumerator().MoveNext()
            ? CurrencyRules.Excluded.First()
            : "XCD"; // Fallback if set is empty

        // Arrange
        var query = GetValidQuery() with
        {
            BaseCurrency = excludedCode,
        };

        // Act & Assert
        _validator
            .TestValidate(query)
            .ShouldHaveValidationErrorFor(x => x.BaseCurrency)
            .WithErrorMessage("Base currency is excluded.");
    }

    // --- Date Range Validation Tests ---

    [Fact]
    public void Should_Fail_When_From_Is_After_To()
    {
        // Arrange
        var query = GetValidQuery() with
        {
            From = DateOnly.FromDateTime(DateTime.Today),
            To = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
        };

        // Act & Assert
        _validator
            .TestValidate(query)
            .ShouldHaveValidationErrorFor(x => x.From)
            .WithErrorMessage("'from' must be on or before 'to'.");
    }

    [Fact]
    public void Should_Pass_When_From_Equals_To()
    {
        // Arrange
        var query = GetValidQuery() with
        {
            From = DateOnly.FromDateTime(DateTime.Today),
            To = DateOnly.FromDateTime(DateTime.Today),
        };

        // Act & Assert
        _validator.TestValidate(query).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Pass_For_Max_366_Day_Range()
    {
        // Arrange: 366 days (e.g., Feb 1, 2024 to Feb 1, 2025 covers the leap day)
        var query = GetValidQuery() with
        {
            From = new DateOnly(2024, 2, 1),
            To = new DateOnly(2025, 2, 1),
        };

        // Act & Assert
        _validator.TestValidate(query).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_For_367_Day_Range()
    {
        // Arrange: 367 days (e.g., Feb 1, 2024 to Feb 2, 2025)
        var query = GetValidQuery() with
        {
            From = new DateOnly(2024, 2, 1),
            To = new DateOnly(2025, 2, 2),
        };

        // Act & Assert
        _validator
            .TestValidate(query)
            .ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Date range cannot exceed 366 days.");
    }

    // --- Pagination Validation Tests ---

    [Fact]
    public void Should_Fail_When_Page_Is_Zero()
    {
        // Arrange
        var query = GetValidQuery() with
        {
            Page = 0,
        };

        // Act & Assert
        _validator
            .TestValidate(query)
            .ShouldHaveValidationErrorFor(x => x.Page)
            .WithErrorMessage("'Page' must be greater than or equal to '1'.");
    }

    [Fact]
    public void Should_Fail_When_PageSize_Is_Zero()
    {
        // Arrange
        var query = GetValidQuery() with
        {
            PageSize = 0,
        };

        // Act & Assert
        _validator
            .TestValidate(query)
            .ShouldHaveValidationErrorFor(x => x.PageSize)
            // Corrected expected message to include the default "You entered 0"
            .WithErrorMessage("'Page Size' must be between 1 and 200. You entered 0.");
    }

    [Fact]
    public void Should_Fail_When_PageSize_Is_Too_Large()
    {
        // Arrange
        var query = GetValidQuery() with
        {
            PageSize = 201,
        };

        // Act & Assert
        _validator
            .TestValidate(query)
            .ShouldHaveValidationErrorFor(x => x.PageSize)
            // Corrected expected message to include the default "You entered 201"
            .WithErrorMessage("'Page Size' must be between 1 and 200. You entered 201.");
    }
}
