namespace CryptoTaxHelper.Application.Models;

public record YearSummary
{
    public required string Period { get; init; }
    public decimal Wins { get; set; }
    public decimal Losses { get; set; }
    public decimal Total { get; set; }
    public decimal TotalBuyVolume { get; set; }
    public decimal TotalSellVolume { get; set; }

    // Tax declaration breakdown (separated by profit/loss)
    public decimal ProfitSellVolume { get; set; }
    public decimal ProfitBuyVolume { get; set; }
    public decimal ProfitFees { get; set; }
    public decimal LossSellVolume { get; set; }
    public decimal LossBuyVolume { get; set; }
    public decimal LossFees { get; set; }
}
