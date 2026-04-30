namespace CryptoTaxHelper.Application.Models;

public record CurrencyReport
{
    public required Dictionary<string, YearSummary> Years { get; init; }
    public required List<ProcessedTransaction> Transactions { get; init; }
}
