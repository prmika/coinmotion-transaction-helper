namespace CryptoTaxHelper.Application.Models;

public record ProcessedTransaction
{
    public required DateTimeOffset Time { get; init; }
    public required TransactionType Type { get; init; }
    public required string FromCurrency { get; init; }
    public required string ToCurrency { get; init; }
    public required decimal CryptoAmount { get; init; }
    public required decimal EurAmount { get; init; }
    public required decimal Rate { get; init; }
    public required decimal Fee { get; init; }
    public required string FeeCurrency { get; init; }
    public required string Source { get; init; }

    // Sell-specific fields (populated after FIFO processing)
    public decimal? CostBasis { get; init; }
    public decimal? AssumedCost { get; init; }
    public decimal? CostBasisUsed { get; init; }
    public string? CostBasisMethod { get; init; }
    public decimal? ProfitLoss { get; init; }
    public decimal? RemainingQuantity { get; init; }
}
