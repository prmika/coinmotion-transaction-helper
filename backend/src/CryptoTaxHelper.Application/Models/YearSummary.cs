namespace CryptoTaxHelper.Application.Models;

public record YearSummary
{
    public required string Period { get; init; }
    public double Wins { get; set; }
    public double Losses { get; set; }
    public double Total { get; set; }
    public double TotalBuyVolume { get; set; }
    public double TotalSellVolume { get; set; }

    // Tax declaration breakdown (separated by profit/loss)
    public double ProfitSellVolume { get; set; }
    public double ProfitBuyVolume { get; set; }
    public double ProfitFees { get; set; }
    public double LossSellVolume { get; set; }
    public double LossBuyVolume { get; set; }
    public double LossFees { get; set; }
}
