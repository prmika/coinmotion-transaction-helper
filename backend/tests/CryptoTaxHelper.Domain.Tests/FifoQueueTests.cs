using CryptoTaxHelper.Domain;
using CryptoTaxHelper.Domain.Exceptions;
using CryptoTaxHelper.Domain.Fifo;
using FluentAssertions;
using Xunit;

namespace CryptoTaxHelper.Domain.Tests;

public class FifoQueueTests
{
    private static DateTimeOffset Ts(string iso) => DateTimeOffset.Parse(iso);

    [Fact]
    public void AddPurchase_ValidInput_AddsToQueue()
    {
        var fifo = new FifoQueue();

        fifo.AddPurchase(1.0, 10000.0, Ts("2024-01-01T10:00:00+02:00"));

        fifo.RemainingQuantity().Should().Be(1.0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddPurchase_InvalidQuantity_Throws(double quantity)
    {
        var fifo = new FifoQueue();

        var act = () => fifo.AddPurchase(quantity, 10000.0, Ts("2024-01-01T10:00:00+02:00"));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddPurchase_NegativePrice_Throws()
    {
        var fifo = new FifoQueue();

        var act = () => fifo.AddPurchase(1.0, -10.0, Ts("2024-01-01T10:00:00+02:00"));

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    public void CalculateCogs_InvalidSellQuantity_Throws(double quantity)
    {
        var fifo = new FifoQueue();

        var act = () => fifo.CalculateCogs(quantity, Ts("2024-02-01T10:00:00+02:00"), 0.0);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalculateCogs_InsufficientInventory_Throws()
    {
        var fifo = new FifoQueue();
        fifo.AddPurchase(0.1, 10000.0, Ts("2024-01-01T10:00:00+02:00"));

        var act = () => fifo.CalculateCogs(0.2, Ts("2024-02-01T10:00:00+02:00"), 0.2 * 15000.0);

        act.Should().Throw<InsufficientInventoryException>();
    }

    [Fact]
    public void CalculateCogs_PartialSell_ConsumesCorrectly()
    {
        var fifo = new FifoQueue();
        fifo.AddPurchase(1.0, 10000.0, Ts("2024-01-01T10:00:00+02:00"));
        fifo.AddPurchase(0.5, 12000.0, Ts("2024-01-10T10:00:00+02:00"));

        var result = fifo.CalculateCogs(1.2, Ts("2024-02-01T10:00:00+02:00"), 1.2 * 15000.0);

        result.CostOfGoodsSold.Should().BeApproximately(10000.0 + (0.2 * 12000.0), Constants.Epsilon);
        result.AssumedCost.Should().BeApproximately((1.2 * 15000.0) * 0.2, Constants.Epsilon);
        result.ConsumedLots.Should().HaveCount(2);
        fifo.Queue.Should().HaveCount(1);
        fifo.Queue.First().Quantity.Should().BeApproximately(0.3, Constants.Epsilon);
        fifo.Queue.First().PricePerUnit.Should().Be(12000.0);
    }

    [Fact]
    public void CalculateCogs_AcquisitionCostAssumption_CalculatesCorrectly()
    {
        var fifo = new FifoQueue();
        fifo.AddPurchase(1.0, 1, Ts("2022-01-01T10:00:00+02:00"));
        fifo.AddPurchase(1.0, 4, Ts("2023-01-01T10:00:00+02:00"));

        var result = fifo.CalculateCogs(2.0, Ts("2024-02-01T10:00:00+02:00"), 20);

        result.CostOfGoodsSold.Should().BeApproximately(5.0, Constants.Epsilon);
        result.AssumedCost.Should().BeApproximately(4.0, Constants.Epsilon); // 0.2 * 20
        result.ConsumedLots.Should().HaveCount(2);
        fifo.Queue.Should().BeEmpty();
    }

    [Fact]
    public void CalculateCogs_LongHoldingPeriod_Uses40PercentRate()
    {
        var fifo = new FifoQueue();
        fifo.AddPurchase(1.0, 1, Ts("2010-06-01T10:00:00+02:00"));
        fifo.AddPurchase(1.0, 4, Ts("2024-01-01T10:00:00+02:00"));

        var result = fifo.CalculateCogs(2.0, Ts("2024-12-01T10:00:00+02:00"), 20);

        result.CostOfGoodsSold.Should().BeApproximately(5.0, Constants.Epsilon);
        // First lot: 0.4 * 10 = 4.0, second lot: 0.2 * 10 = 2.0
        result.AssumedCost.Should().BeApproximately(6.0, Constants.Epsilon);
        result.ConsumedLots.Should().HaveCount(2);
    }

    [Fact]
    public void CalculateCogs_SmallRounding_DoesNotThrow()
    {
        var fifo = new FifoQueue();
        fifo.AddPurchase(0.27622000, 1.0, Ts("2024-01-01T10:00:00+02:00"));

        // Sell with tiny floating point excess
        var act = () => fifo.CalculateCogs(0.27622000000001, Ts("2024-01-02T10:00:00+02:00"), 0.27622000000001);

        act.Should().NotThrow();
    }
}
