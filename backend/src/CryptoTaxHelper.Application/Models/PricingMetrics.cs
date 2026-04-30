namespace CryptoTaxHelper.Application.Models;

public record PricingMetrics
{
    public int TotalSalesTransactions { get; init; }
    public double TotalSalesVolumeEur { get; init; }
    public double TotalProfitLossEur { get; init; }
}
