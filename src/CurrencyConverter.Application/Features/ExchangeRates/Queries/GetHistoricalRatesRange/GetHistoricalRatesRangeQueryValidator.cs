using FluentValidation;

namespace CurrencyConverter.Application.Features.ExchangeRates.Queries.GetHistoricalRatesRange;

public class GetHistoricalRatesRangeQueryValidator : AbstractValidator<GetHistoricalRatesRangeQuery>
{
    private static readonly DateOnly EarliestDate = new(1999, 1, 4);

    public GetHistoricalRatesRangeQueryValidator()
    {
        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.")
            .Must(d => d >= EarliestDate).WithMessage($"Start date must be on or after {EarliestDate:yyyy-MM-dd} (Frankfurter earliest available date).")
            .Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Start date cannot be in the future.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .Must(d => d >= EarliestDate).WithMessage($"End date must be on or after {EarliestDate:yyyy-MM-dd} (Frankfurter earliest available date).")
            .Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("End date cannot be in the future.")
            .Must((request, endDate) => endDate >= request.StartDate)
            .WithMessage("End date must be greater than or equal to start date.");

        RuleFor(x => x.BaseCurrency)
            .NotEmpty().WithMessage("Base currency is required.")
            .Length(3).WithMessage("Base currency must be a 3-letter ISO 4217 code.")
            .Matches("^[A-Za-z]{3}$").WithMessage("Base currency must contain only letters.");

        RuleFor(x => x.TargetCurrencies)
            .Must(c => c is null || c.All(currency => !string.IsNullOrWhiteSpace(currency)))
            .WithMessage("Target currencies cannot contain empty values.")
            .ForEach(rule => rule
                .Length(3).WithMessage("Each target currency must be a 3-letter ISO 4217 code.")
                .Matches("^[A-Za-z]{3}$").WithMessage("Each target currency must contain only letters."));

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
    }
}
