using System.Text.Json.Serialization;

namespace CurrencyConverter.Application.ResultResponse;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }
    public List<string> Errors { get; private set; } = new();
    public ResultStatus Status { get; private set; }

    [JsonConstructor]
    private Result(ResultStatus status, T? data, string? errorMessage = null, List<string>? errors = null)
    {
        Status = status;
        IsSuccess = status == ResultStatus.Success;
        Data = data;
        ErrorMessage = errorMessage;
        if (errors != null)
            Errors = errors;
    }

    public static Result<T> Success() => new(ResultStatus.Success, default);

    public static Result<T> Success(T data) => new(ResultStatus.Success, data);

    public static Result<T> Failure(string errorMessage) => new(ResultStatus.BadRequest, default, errorMessage);

    public static Result<T> Failure(List<string> errors) => new(ResultStatus.ValidationError, default, null, errors);

    public static Result<T> NotFound(string errorMessage) => new(ResultStatus.NotFound, default, errorMessage);

    public static Result<T> Conflict(string errorMessage) => new(ResultStatus.Conflict, default, errorMessage);
}
