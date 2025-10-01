// using System.Collections.Generic;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;
// // Use your real namespace for the behavior:
// using Currency.Application.Behaviors;
// using FluentAssertions;
// using FluentValidation;
// using FluentValidation.Results;
// using NSubstitute;
// using Xunit;

// namespace Currency.UnitTests.Behaviors;

// public class ValidationBehavior_Coverage_Tests
// {
//     [Fact]
//     public async Task Handle_Valid_Request_Invokes_Next()
//     {
//         var v = Substitute.For<IValidator<DummyRequest>>();
//         // Configure BOTH sync and async
//         v.Validate(Arg.Any<DummyRequest>()).Returns(new ValidationResult());
//         v.ValidateAsync(Arg.Any<DummyRequest>(), Arg.Any<CancellationToken>())
//             .Returns(new ValidationResult());

//         var behavior = new ValidationBehavior<DummyRequest, int>(new[] { v });

//         var called = false;
//         Task<int> Next(CancellationToken _)
//         {
//             called = true;
//             return Task.FromResult(42);
//         }

//         var result = await behavior.Handle(new DummyRequest("ok"), Next, default);

//         called.Should().BeTrue();
//         result.Should().Be(42);
//         v.Received(1).Validate(Arg.Any<DummyRequest>());
//         await v.Received(1).ValidateAsync(Arg.Any<DummyRequest>(), Arg.Any<CancellationToken>());
//     }

//     [Fact]
//     public async Task Handle_Invalid_Request_Aggregates_Errors_And_Throws()
//     {
//         var failures1 = new List<ValidationFailure>
//         {
//             new("BaseCurrency", "required"),
//             new("BaseCurrency", "format"),
//         };
//         var failures2 = new List<ValidationFailure> { new("Symbols[0]", "invalid") };

//         var v1 = Substitute.For<IValidator<DummyRequest>>();
//         v1.Validate(Arg.Any<DummyRequest>()).Returns(new ValidationResult(failures1));
//         v1.ValidateAsync(Arg.Any<DummyRequest>(), Arg.Any<CancellationToken>())
//             .Returns(new ValidationResult(failures1));

//         var v2 = Substitute.For<IValidator<DummyRequest>>();
//         v2.Validate(Arg.Any<DummyRequest>()).Returns(new ValidationResult(failures2));
//         v2.ValidateAsync(Arg.Any<DummyRequest>(), Arg.Any<CancellationToken>())
//             .Returns(new ValidationResult(failures2));

//         var behavior = new ValidationBehavior<DummyRequest, int>(new[] { v1, v2 });

//         var act = async () =>
//             await behavior.Handle(
//                 new DummyRequest("bad"),
//                 (
//                     _ /*ct*/
//                 ) => Task.FromResult(0),
//                 default
//             );

//         await act.Should().ThrowAsync<ValidationException>();
//         v1.Received(1).Validate(Arg.Any<DummyRequest>());
//         v2.Received(1).Validate(Arg.Any<DummyRequest>());
//         await v1.Received(1).ValidateAsync(Arg.Any<DummyRequest>(), Arg.Any<CancellationToken>());
//         await v2.Received(1).ValidateAsync(Arg.Any<DummyRequest>(), Arg.Any<CancellationToken>());
//     }

//     public record DummyRequest(string Value);
// }
