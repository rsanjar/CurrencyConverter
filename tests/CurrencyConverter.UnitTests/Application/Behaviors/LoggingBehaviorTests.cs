using CurrencyConverter.Application.Behaviors;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CurrencyConverter.UnitTests.Application.Behaviors;

public class LoggingBehaviorTests
{
    public record TestRequest : IRequest<string>;

    private static LoggingBehavior<TestRequest, string> CreateBehavior() =>
        new(Substitute.For<ILogger<LoggingBehavior<TestRequest, string>>>());

    [Fact]
    public async Task Handle_SuccessfulRequest_ReturnsHandlerResponse()
    {
        var behavior = CreateBehavior();
        RequestHandlerDelegate<string> next = _ => Task.FromResult("expected-response");

        var result = await behavior.Handle(new TestRequest(), next, CancellationToken.None);

        result.Should().Be("expected-response");
    }

    [Fact]
    public async Task Handle_ExceptionInNext_RethrowsException()
    {
        var behavior = CreateBehavior();
        RequestHandlerDelegate<string> next = _ => throw new InvalidOperationException("downstream failure");

        var act = () => behavior.Handle(new TestRequest(), next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("downstream failure");
    }

    [Fact]
    public async Task Handle_ExceptionInNext_PropagatesWithOriginalType()
    {
        var behavior = CreateBehavior();
        RequestHandlerDelegate<string> next = _ => throw new ArgumentNullException("param", "param cannot be null");

        var act = () => behavior.Handle(new TestRequest(), next, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_SlowRequest_StillReturnsSuccessfully()
    {
        var behavior = CreateBehavior();
        RequestHandlerDelegate<string> next = async _ =>
        {
            await Task.Delay(600); // exceeds 500ms slow-request threshold
            return "slow-but-ok";
        };

        var result = await behavior.Handle(new TestRequest(), next, CancellationToken.None);

        result.Should().Be("slow-but-ok");
    }

    [Fact]
    public async Task Handle_CancellationToken_IsPassedToNext()
    {
        var behavior = CreateBehavior();
        CancellationToken capturedToken = default;
        using var cts = new CancellationTokenSource();

        RequestHandlerDelegate<string> next = ct =>
        {
            capturedToken = ct;
            return Task.FromResult("ok");
        };

        await behavior.Handle(new TestRequest(), next, cts.Token);

        capturedToken.Should().Be(cts.Token);
    }
}
