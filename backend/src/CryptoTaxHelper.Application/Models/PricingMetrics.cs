namespace CryptoTaxHelper.Application.Models;

public record PricingMetrics
{
    public int TotalSalesTransactions { get; init; }
    public decimal TotalSalesVolumeEur { get; init; }
    public decimal TotalProfitLossEur { get; init; }
}
