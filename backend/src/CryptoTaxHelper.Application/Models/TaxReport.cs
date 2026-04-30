namespace CryptoTaxHelper.Application.Models;

public record TaxReport
{
    public required Dictionary<string, CurrencyReport> Currencies { get; init; }
    public string BrokerName { get; init; } = "the broker";
}
