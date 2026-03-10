using CurrencyConverter.Application.ResultResponse;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.Api.Extensions;

/// <summary>
/// Extension methods for mapping application results to API responses.
/// </summary>
public static class ResultExtensions
{
    public static IActionResult ToOkResult<T>(this ControllerBase controller, Result<T> result)
    {
        if (!result.IsSuccess)
            return controller.ToErrorResult(result.Status, result.ErrorMessage, result.Errors);

        return controller.Ok(result.Data);
    }

    public static IActionResult ToOkResult(this ControllerBase controller, Result result)
    {
        if (!result.IsSuccess)
            return controller.ToErrorResult(result.Status, result.ErrorMessage, result.Errors);

        return controller.NoContent();
    }

    public static IActionResult ToCreatedResult<T>(
        this ControllerBase controller,
        Result<T> result,
        string actionName,
        object? routeValues)
    {
        if (!result.IsSuccess)
            return controller.ToErrorResult(result.Status, result.ErrorMessage, result.Errors);

        return controller.CreatedAtAction(actionName, routeValues, result.Data);
    }

    private static IActionResult ToErrorResult(
        this ControllerBase controller,
        ResultStatus status,
        string? errorMessage,
        List<string> errors)
    {
        var body = new { error = errorMessage, errors };

        return status switch
        {
            ResultStatus.NotFound => controller.NotFound(body),
            ResultStatus.Conflict => controller.Conflict(body),
            ResultStatus.ValidationError => controller.UnprocessableEntity(body),
            ResultStatus.Unauthorized => controller.Unauthorized(body),
            ResultStatus.Forbidden => controller.Forbid(),
            _ => controller.BadRequest(body)
        };
    }
}
