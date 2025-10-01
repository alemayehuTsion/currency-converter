using System.Threading;
using System.Threading.Tasks;
using Currency.Application.Behaviors;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Xunit;

public class ValidationBehavior_Tests
{
    private sealed record DummyCmd(string Name) : IRequest<string>;

    private sealed class DummyValidator : AbstractValidator<DummyCmd>
    {
        public DummyValidator() => RuleFor(x => x.Name).NotEmpty();
    }

    [Fact]
    public async Task Throws_when_validation_fails()
    {
        var validators = new IValidator<DummyCmd>[] { new DummyValidator() };
        var behavior = new ValidationBehavior<DummyCmd, string>(validators);

        var act = async () =>
            await behavior.Handle(
                new DummyCmd(""),
                next: (
                    _ /*ct*/
                ) => Task.FromResult("OK"),
                ct: CancellationToken.None
            );

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Passes_through_when_no_validators()
    {
        var behavior = new ValidationBehavior<DummyCmd, string>(
            Array.Empty<IValidator<DummyCmd>>()
        );

        RequestHandlerDelegate<string> next = (
            _ /*ct*/
        ) => Task.FromResult("OK");

        var result = await behavior.Handle(new DummyCmd("anything"), next, CancellationToken.None);

        result.Should().Be("OK");
    }
}
