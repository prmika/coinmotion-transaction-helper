namespace CryptoTaxHelper.Application.Models;

public record ProcessedTransaction
{
    public required DateTimeOffset Time { get; init; }
    public required TransactionType Type { get; init; }
    public required string FromCurrency { get; init; }
    public required string ToCurrency { get; init; }
    public required double CryptoAmount { get; init; }
    public required double EurAmount { get; init; }
    public required double Rate { get; init; }
    public required double Fee { get; init; }
    public required string FeeCurrency { get; init; }
    public required string Source { get; init; }

    // Sell-specific fields (populated after FIFO processing)
    public double? CostBasis { get; init; }
    public double? AssumedCost { get; init; }
    public double? CostBasisUsed { get; init; }
    public string? CostBasisMethod { get; init; }
    public double? ProfitLoss { get; init; }
    public double? RemainingQuantity { get; init; }
}
