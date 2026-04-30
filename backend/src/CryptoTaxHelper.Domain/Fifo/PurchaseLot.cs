namespace CryptoTaxHelper.Domain.Fifo;

public record PurchaseLot(double Quantity, double PricePerUnit, DateTimeOffset Time);
