using FluentValidation;

namespace CurrencyConverter.Application.Features.ExchangeRates.Queries.GetLatestRates;

public class GetLatestRatesQueryValidator : AbstractValidator<GetLatestRatesQuery>
{
    public GetLatestRatesQueryValidator()
    {
        RuleFor(x => x.BaseCurrency)
            .NotEmpty().WithMessage("Base currency is required.")
            .Length(3).WithMessage("Base currency must be a 3-letter ISO 4217 code.")
            .Matches("^[A-Za-z]{3}$").WithMessage("Base currency must contain only letters.");
    }
}
