namespace CryptoTaxHelper.Application.Models;

public record NormalizedTransaction
{
    public required DateTimeOffset Time { get; init; }
    public required TransactionType Type { get; init; }
    public required string FromCurrency { get; init; }
    public required string ToCurrency { get; init; }
    public required decimal CryptoAmount { get; init; }
    public required decimal FiatAmount { get; init; }
    public required decimal Rate { get; init; }
    public required decimal Fee { get; init; }
    public required string FeeCurrency { get; init; }
    public required string Source { get; init; }
    public int? SourceRow { get; init; }
    public string? SourceCryptoAmount { get; init; }
}
