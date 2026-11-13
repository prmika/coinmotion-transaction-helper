namespace CryptoTaxHelper.Domain.Fifo;

public record ConsumedLot(decimal Quantity, decimal PricePerUnit, DateTimeOffset PurchaseTime);
