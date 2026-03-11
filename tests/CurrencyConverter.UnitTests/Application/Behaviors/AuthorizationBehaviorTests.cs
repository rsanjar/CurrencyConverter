using CurrencyConverter.Application.Behaviors;
using CurrencyConverter.Application.Common.Exceptions;
using CurrencyConverter.Application.Common.Interfaces;
using CurrencyConverter.Application.Common.Security;
using FluentAssertions;
using MediatR;
using NSubstitute;

namespace CurrencyConverter.UnitTests.Application.Behaviors;

public class AuthorizationBehaviorTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    public record RegularRequest : IRequest<string>;

    public record AuthorizableRequest : IRequest<string>, IAuthorizable;

    public record RoleRestrictedRequest(string[] Roles) : IRequest<string>, IAuthorizable
    {
        IReadOnlyList<string> IAuthorizable.RequiredRoles => Roles;
    }

    private AuthorizationBehavior<T, string> CreateBehavior<T>()
        where T : IRequest<string>
        => new(_currentUser);

    [Fact]
    public async Task Handle_NonAuthorizableRequest_CallsNextWithoutCheckingAuth()
    {
        var behavior = CreateBehavior<RegularRequest>();
        var nextCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult("ok");
        };

        await behavior.Handle(new RegularRequest(), next, CancellationToken.None);

        nextCalled.Should().BeTrue();
        _currentUser.DidNotReceive();
    }

    [Fact]
    public async Task Handle_AuthorizableRequest_NotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUser.IsAuthenticated.Returns(false);
        var behavior = CreateBehavior<AuthorizableRequest>();
        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        var act = () => behavior.Handle(new AuthorizableRequest(), next, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_AuthorizableRequest_NotAuthenticated_DoesNotCallNext()
    {
        _currentUser.IsAuthenticated.Returns(false);
        var behavior = CreateBehavior<AuthorizableRequest>();
        var nextCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult("ok");
        };

        await FluentActions.Invoking(() => behavior.Handle(new AuthorizableRequest(), next, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>();

        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_AuthorizableRequest_Authenticated_NoRolesRequired_CallsNext()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Roles.Returns([]);
        var behavior = CreateBehavior<AuthorizableRequest>();
        var nextCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult("ok");
        };

        await behavior.Handle(new AuthorizableRequest(), next, CancellationToken.None);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RoleRestrictedRequest_UserHasRequiredRole_CallsNext()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Roles.Returns(["admin", "user"]);
        var behavior = CreateBehavior<RoleRestrictedRequest>();
        var nextCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult("ok");
        };

        await behavior.Handle(new RoleRestrictedRequest(["admin"]), next, CancellationToken.None);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RoleRestrictedRequest_UserHasOneOfRequiredRoles_CallsNext()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Roles.Returns(["editor"]);
        var behavior = CreateBehavior<RoleRestrictedRequest>();
        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        var act = () => behavior.Handle(new RoleRestrictedRequest(["admin", "editor"]), next, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_RoleRestrictedRequest_UserMissingRequiredRole_ThrowsForbiddenException()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Roles.Returns(["user"]);
        var behavior = CreateBehavior<RoleRestrictedRequest>();
        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        var act = () => behavior.Handle(new RoleRestrictedRequest(["admin"]), next, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_RoleRestrictedRequest_UserHasNoRoles_ThrowsForbiddenException()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Roles.Returns([]);
        var behavior = CreateBehavior<RoleRestrictedRequest>();
        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        var act = () => behavior.Handle(new RoleRestrictedRequest(["admin"]), next, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
