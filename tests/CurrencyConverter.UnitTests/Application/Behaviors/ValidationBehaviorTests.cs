using CurrencyConverter.Application.Behaviors;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;

namespace CurrencyConverter.UnitTests.Application.Behaviors;

public class ValidationBehaviorTests
{
    public record TestRequest : IRequest<string>;

    [Fact]
    public async Task Handle_NoValidators_CallsNextAndReturnsResult()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([]);
        var nextCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult("ok");
        };

        var result = await behavior.Handle(new TestRequest(), next, CancellationToken.None);

        nextCalled.Should().BeTrue();
        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_PassingValidator_CallsNext()
    {
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<TestRequest, string>([validator]);
        var nextCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult("ok");
        };

        await behavior.Handle(new TestRequest(), next, CancellationToken.None);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_FailingValidator_ThrowsValidationExceptionWithErrors()
    {
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Field", "Error message")]));

        var behavior = new ValidationBehavior<TestRequest, string>([validator]);
        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        var act = () => behavior.Handle(new TestRequest(), next, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainSingle(e => e.ErrorMessage == "Error message");
    }

    [Fact]
    public async Task Handle_MultipleValidators_CollectsAllFailures()
    {
        var validator1 = Substitute.For<IValidator<TestRequest>>();
        validator1.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("F1", "Error1")]));

        var validator2 = Substitute.For<IValidator<TestRequest>>();
        validator2.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("F2", "Error2")]));

        var behavior = new ValidationBehavior<TestRequest, string>([validator1, validator2]);
        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        var act = () => behavior.Handle(new TestRequest(), next, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ValidatorWithMultipleErrors_ThrowsWithAllErrors()
    {
        var failures = new[]
        {
            new ValidationFailure("F1", "Error1"),
            new ValidationFailure("F2", "Error2"),
            new ValidationFailure("F3", "Error3"),
        };
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));

        var behavior = new ValidationBehavior<TestRequest, string>([validator]);
        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        var act = () => behavior.Handle(new TestRequest(), next, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_FailingValidator_DoesNotCallNext()
    {
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("F", "Error")]));

        var behavior = new ValidationBehavior<TestRequest, string>([validator]);
        var nextCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult("ok");
        };

        await FluentActions.Invoking(() => behavior.Handle(new TestRequest(), next, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();

        nextCalled.Should().BeFalse();
    }
}
