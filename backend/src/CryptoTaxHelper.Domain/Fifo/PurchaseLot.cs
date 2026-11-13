namespace CryptoTaxHelper.Domain.Fifo;

public record PurchaseLot(decimal Quantity, decimal PricePerUnit, DateTimeOffset Time);
