namespace CurrencyConverter.Application.Common.Exceptions;

/// <summary>
/// Thrown when an authenticated user lacks the required roles to execute a request.
/// </summary>
public sealed class ForbiddenAccessException(
    string message = "User does not have permission to perform this action.")
    : Exception(message);
