using CryptoTaxHelper.Domain.Exceptions;

namespace CryptoTaxHelper.Domain.Fifo;

public class FifoQueue
{
    private readonly LinkedList<PurchaseLot> _queue = new();

    public IReadOnlyCollection<PurchaseLot> Queue => _queue;

    public void AddPurchase(double quantity, double pricePerUnit, DateTimeOffset time)
    {
        if (quantity <= 0)
            throw new ArgumentException("Purchase quantity must be positive", nameof(quantity));
        if (pricePerUnit < 0)
            throw new ArgumentException("Purchase price cannot be negative", nameof(pricePerUnit));

        _queue.AddLast(new PurchaseLot(quantity, pricePerUnit, time));
    }

    public FifoResult CalculateCogs(double quantitySold, DateTimeOffset soldTime, double totalRevenue)
    {
        if (quantitySold <= 0)
            throw new ArgumentException("Sell quantity must be positive", nameof(quantitySold));
        if (totalRevenue < 0)
            throw new ArgumentException("Total revenue cannot be negative", nameof(totalRevenue));

        var cogs = 0.0;
        var assumedCost = 0.0;
        var consumedLots = new List<ConsumedLot>();
        var pricePerUnit = quantitySold > 0 ? totalRevenue / quantitySold : 0.0;
        var remainingToSell = quantitySold;

        while (remainingToSell > Constants.Epsilon)
        {
            if (_queue.First is null)
            {
                if (remainingToSell <= Constants.Epsilon)
                    break;

                throw new InsufficientInventoryException(
                    "unknown", quantitySold, quantitySold - remainingToSell);
            }

            var lot = _queue.First.Value;
            _queue.RemoveFirst();

            var heldDays = (soldTime - lot.Time).TotalDays;
            var assumedRate = heldDays >= Constants.LongHoldingPeriodDays
                ? Constants.LongHoldingAssumedRate
                : Constants.ShortHoldingAssumedRate;

            if (lot.Quantity <= remainingToSell + Constants.Epsilon)
            {
                var sellQty = lot.Quantity;
                var proceedsPortion = sellQty * pricePerUnit;

                cogs += sellQty * lot.PricePerUnit;
                assumedCost += proceedsPortion * assumedRate;
                consumedLots.Add(new ConsumedLot(sellQty, lot.PricePerUnit, lot.Time));
                remainingToSell -= sellQty;
            }
            else
            {
                var proceedsPortion = remainingToSell * pricePerUnit;

                cogs += remainingToSell * lot.PricePerUnit;
                assumedCost += proceedsPortion * assumedRate;
                consumedLots.Add(new ConsumedLot(remainingToSell, lot.PricePerUnit, lot.Time));

                var remainingLot = new PurchaseLot(
                    lot.Quantity - remainingToSell, lot.PricePerUnit, lot.Time);
                _queue.AddFirst(remainingLot);
                remainingToSell = 0.0;
            }
        }

        return new FifoResult(cogs, assumedCost, consumedLots);
    }

    public double RemainingQuantity()
    {
        return _queue.Sum(lot => lot.Quantity);
    }
}
