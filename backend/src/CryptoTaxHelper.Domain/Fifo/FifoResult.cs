namespace CryptoTaxHelper.Domain.Fifo;

public record FifoResult(double CostOfGoodsSold, double AssumedCost, IReadOnlyList<ConsumedLot> ConsumedLots);
