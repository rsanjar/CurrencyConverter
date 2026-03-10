using FluentValidation;

namespace CurrencyConverter.Application.Features.ExchangeRates.Queries.ConvertCurrency;

public class ConvertCurrencyQueryValidator : AbstractValidator<ConvertCurrencyQuery>
{
    //NOTE: restricted currencies could be enabled in validation, but it's required to send 400 bad request,
    // that's why it's handed in the handler with a custom exception and mapped to 400 in the API layer.
    // private static readonly HashSet<string> RestrictedCurrencies = ["TRY", "PLN", "THB", "MXN"];
    // private static readonly string RestrictedCurrenciesMessage = 
    //     $"Currency conversion is not supported for: {string.Join(", ", 
    //         RestrictedCurrencies.OrderBy(c => c, StringComparer.OrdinalIgnoreCase))}.";

    public ConvertCurrencyQueryValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.FromCurrency)
            .NotEmpty().WithMessage("Source currency is required.")
            .Length(3).WithMessage("Source currency must be a 3-letter ISO 4217 code.")
            .Matches("^[A-Za-z]{3}$").WithMessage("Source currency must contain only letters.");
            // .Must(x => !RestrictedCurrencies.Any(c => c.Equals(x.Trim(), StringComparison.OrdinalIgnoreCase)))
            // .WithMessage(RestrictedCurrenciesMessage);


        RuleFor(x => x.ToCurrencies)
            .NotNull().WithMessage("Target currencies are required.")
            .Must(c => c.Any()).WithMessage("At least one target currency must be provided.")
            .ForEach(rule => rule
                .NotEmpty().WithMessage("Target currency cannot be empty.")
                .Length(3).WithMessage("Each target currency must be a 3-letter ISO 4217 code.")
                .Matches("^[A-Za-z]{3}$").WithMessage("Each target currency must contain only letters.")
                // .Must(x => !RestrictedCurrencies.Any(c => c.Equals(x.Trim(), StringComparison.OrdinalIgnoreCase)))
                // .WithMessage(RestrictedCurrenciesMessage)
            );
    }
}
