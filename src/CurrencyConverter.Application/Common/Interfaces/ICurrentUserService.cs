namespace CurrencyConverter.Application.Common.Interfaces;

/// <summary>
/// Provides access to the current authenticated user's identity.
/// Implemented in the API layer using <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/>.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// The ID of the current authenticated user, or <c>null</c> when unauthenticated.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Whether the current HTTP request carries a valid, authenticated identity.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// The roles assigned to the current user.
    /// </summary>
    IReadOnlyList<string> Roles { get; }
}
