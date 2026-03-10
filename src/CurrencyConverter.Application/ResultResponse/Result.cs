using System.Text.Json.Serialization;

namespace CurrencyConverter.Application.ResultResponse;

public class Result
{
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public List<string> Errors { get; private set; } = new();
    public ResultStatus Status { get; private set; }

    [JsonConstructor]
    private Result(ResultStatus status, string? errorMessage = null, List<string>? errors = null)
    {
        Status = status;
        IsSuccess = status == ResultStatus.Success;
        ErrorMessage = errorMessage;
        if (errors != null)
            Errors = errors;
    }

    public static Result Success() => new(ResultStatus.Success);

    public static Result Failure(string errorMessage) => new(ResultStatus.BadRequest, errorMessage);

    public static Result Failure(List<string> errors) => new(ResultStatus.ValidationError, null, errors);

    public static Result NotFound(string errorMessage) => new(ResultStatus.NotFound, errorMessage);

    public static Result Conflict(string errorMessage) => new(ResultStatus.Conflict, errorMessage);
}
