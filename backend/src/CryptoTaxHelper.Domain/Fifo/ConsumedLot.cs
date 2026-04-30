namespace CryptoTaxHelper.Domain.Fifo;

public record ConsumedLot(double Quantity, double PricePerUnit, DateTimeOffset PurchaseTime);
