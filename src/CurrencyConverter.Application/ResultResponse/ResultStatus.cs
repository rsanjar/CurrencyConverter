namespace CurrencyConverter.Application.ResultResponse;

public enum ResultStatus
{
    Success,
    BadRequest,
    NotFound,
    Conflict,
    ValidationError,
    Unauthorized,
    Forbidden
}
