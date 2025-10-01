using System.Threading;
using System.Threading.Tasks;
using Currency.Application.Abstractions;
using Currency.Application.Features.RatesLatest;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class LatestRatesHandler_Error_Tests
{
    [Fact]
    public async Task Bubbles_provider_exception()
    {
        var provider = Substitute.For<IExchangeRateProvider>();
        provider
            .GetRatesAsync("USD", Arg.Any<CancellationToken>())
            .Returns<Task<Currency.Domain.RatesSnapshot>>(_ =>
                throw new HttpRequestException("boom")
            );

        var sut = new LatestRatesHandler(provider);

        var act = async () => await sut.Handle(new LatestRatesQuery("USD"), CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>().WithMessage("boom");
    }

    [Fact]
    public async Task Bubbles_upstream_error_for_unknown_base_currency()
    {
        var provider = Substitute.For<IExchangeRateProvider>();
        provider
            .GetRatesAsync("XXX", Arg.Any<CancellationToken>())
            .Returns<Task<Currency.Domain.RatesSnapshot>>(_ =>
                throw new HttpRequestException("Frankfurter returned 400 Bad Request")
            );

        var sut = new LatestRatesHandler(provider);

        var act = async () => await sut.Handle(new LatestRatesQuery("XXX"), CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>().WithMessage("*400*");
    }
}
