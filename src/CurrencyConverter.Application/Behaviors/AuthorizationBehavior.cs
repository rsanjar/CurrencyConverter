using CurrencyConverter.Application.Common.Exceptions;
using CurrencyConverter.Application.Common.Interfaces;
using CurrencyConverter.Application.Common.Security;
using MediatR;

namespace CurrencyConverter.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that enforces authorization on requests
/// implementing <see cref="IAuthorizable"/>. Checks authentication first,
/// then verifies role membership if required roles are specified.
/// </summary>
public sealed class AuthorizationBehavior<TRequest, TResponse>(ICurrentUserService currentUserService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IAuthorizable authorizable)
            return await next(cancellationToken);

        if (!currentUserService.IsAuthenticated)
            throw new UnauthorizedException();

        var requiredRoles = authorizable.RequiredRoles;

        if (requiredRoles.Count > 0 && !requiredRoles.Any(role => currentUserService.Roles.Contains(role)))
            throw new ForbiddenAccessException();

        return await next(cancellationToken);
    }
}
