namespace CryptoTaxHelper.Application.Models;

public sealed record InventoryDeficit
{
    public required string Asset { get; init; }
    public required decimal AvailableQuantity { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public required decimal Difference { get; init; }
    public decimal NumericTolerance { get; init; } = 0.0000000000001m;
    public required int? SourceRow { get; init; }
    public required DateTimeOffset SourceTime { get; init; }
    public string Severity { get; init; } = "blocking";
}