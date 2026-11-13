namespace CryptoTaxHelper.Domain.Fifo;

public record FifoResult(decimal CostOfGoodsSold, decimal AssumedCost, IReadOnlyList<ConsumedLot> ConsumedLots);
