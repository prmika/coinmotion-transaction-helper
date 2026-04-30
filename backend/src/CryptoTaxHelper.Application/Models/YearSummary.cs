namespace CryptoTaxHelper.Application.Models;

public record YearSummary
{
    public required string Period { get; init; }
    public double Wins { get; set; }
    public double Losses { get; set; }
    public double Total { get; set; }
    public double TotalBuyVolume { get; set; }
    public double TotalSellVolume { get; set; }
}
