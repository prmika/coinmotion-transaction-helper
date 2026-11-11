namespace CryptoTaxHelper.Application.Models;

public record TaxReport
{
    public required Dictionary<string, CurrencyReport> Currencies { get; init; }
    public string BrokerName { get; init; } = "the broker";
    public string ValidationStatus { get; init; } = "complete";
    public IReadOnlyList<InventoryDeficit> InventoryDeficits { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
