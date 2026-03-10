using FluentValidation;

namespace CurrencyConverter.Application.Features.ExchangeRates.Queries.GetHistoricalRates;

public class GetHistoricalRatesQueryValidator : AbstractValidator<GetHistoricalRatesQuery>
{
    private static readonly DateOnly EarliestDate = new(1999, 1, 4);

    public GetHistoricalRatesQueryValidator()
    {
        RuleFor(x => x.Date)
            .Must(d => d >= EarliestDate).WithMessage($"Date must be on or after {EarliestDate:yyyy-MM-dd} (Frankfurter earliest available date).")
            .Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Date cannot be in the future.");

        RuleFor(x => x.BaseCurrency)
            .NotEmpty().WithMessage("Base currency is required.")
            .Length(3).WithMessage("Base currency must be a 3-letter ISO 4217 code.")
            .Matches("^[A-Za-z]{3}$").WithMessage("Base currency must contain only letters.");
    }
}
