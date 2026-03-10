using FluentValidation;

namespace CurrencyConverter.Application.Features.ExchangeRates.Queries.ConvertCurrency;

public class ConvertCurrencyQueryValidator : AbstractValidator<ConvertCurrencyQuery>
{
    public ConvertCurrencyQueryValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.FromCurrency)
            .NotEmpty().WithMessage("Source currency is required.")
            .Length(3).WithMessage("Source currency must be a 3-letter ISO 4217 code.")
            .Matches("^[A-Za-z]{3}$").WithMessage("Source currency must contain only letters.");

        RuleFor(x => x.ToCurrencies)
            .NotNull().WithMessage("Target currencies are required.")
            .Must(c => c.Any()).WithMessage("At least one target currency must be provided.")
            .ForEach(rule => rule
                .NotEmpty().WithMessage("Target currency cannot be empty.")
                .Length(3).WithMessage("Each target currency must be a 3-letter ISO 4217 code.")
                .Matches("^[A-Za-z]{3}$").WithMessage("Each target currency must contain only letters."));
    }
}
