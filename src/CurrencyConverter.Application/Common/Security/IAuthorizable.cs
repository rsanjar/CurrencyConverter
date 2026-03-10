namespace CurrencyConverter.Application.Common.Security;

/// <summary>
/// Marker interface for MediatR requests that require authorization.
/// Implement on queries or commands to enforce role-based access via
/// <see cref="CurrencyConverter.Application.Behaviors.AuthorizationBehavior{TRequest,TResponse}"/>.
/// </summary>
public interface IAuthorizable
{
    /// <summary>
    /// Roles allowed to execute this request.
    /// An empty list means any authenticated user is permitted.
    /// </summary>
    IReadOnlyList<string> RequiredRoles => [];
}
