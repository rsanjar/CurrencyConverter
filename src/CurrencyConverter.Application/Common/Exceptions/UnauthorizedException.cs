namespace CurrencyConverter.Application.Common.Exceptions;

/// <summary>
/// Thrown when a request requires authentication but the user is not authenticated.
/// </summary>
public sealed class UnauthorizedException() : Exception("User is not authenticated.");
