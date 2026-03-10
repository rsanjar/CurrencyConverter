using System.Net;
using System.Text.Json;
using FluentValidation;

namespace CurrencyConverter.Api.Middleware;

/// <summary>
/// Global exception handling middleware that converts unhandled exceptions into
/// structured JSON error responses.
/// </summary>
public class GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Validation error occurred");
            await WriteErrorAsync(context, HttpStatusCode.UnprocessableEntity, "Validation failed",
                ex.Errors.Select(e => e.ErrorMessage).ToList());
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request to external service failed");
            await WriteErrorAsync(context, HttpStatusCode.BadGateway,
                "Failed to communicate with the currency data provider");
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Request was cancelled");
            context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred");
            await WriteErrorAsync(context, HttpStatusCode.InternalServerError, "An unexpected error occurred");
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string message,
        IEnumerable<string>? errors = null)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var body = new
        {
            error = message,
            errors = errors ?? []
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}
