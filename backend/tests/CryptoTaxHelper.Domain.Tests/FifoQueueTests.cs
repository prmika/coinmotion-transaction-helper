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

        fifo.AddPurchase(1.0m, 10000.0m, Ts("2024-01-01T10:00:00+02:00"));

        fifo.RemainingQuantity().Should().Be(1.0m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddPurchase_InvalidQuantity_Throws(int quantity)
    {
        var fifo = new FifoQueue();

        var act = () => fifo.AddPurchase(quantity, 10000.0m, Ts("2024-01-01T10:00:00+02:00"));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddPurchase_NegativePrice_Throws()
    {
        var fifo = new FifoQueue();

        var act = () => fifo.AddPurchase(1.0m, -10.0m, Ts("2024-01-01T10:00:00+02:00"));

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CalculateCogs_InvalidSellQuantity_Throws(int quantity)
    {
        var fifo = new FifoQueue();

        var act = () => fifo.CalculateCogs(quantity, Ts("2024-02-01T10:00:00+02:00"), 0.0m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalculateCogs_InsufficientInventory_Throws()
    {
        var fifo = new FifoQueue();
        fifo.AddPurchase(0.1m, 10000.0m, Ts("2024-01-01T10:00:00+02:00"));

        var act = () => fifo.CalculateCogs(0.2m, Ts("2024-02-01T10:00:00+02:00"), 0.2m * 15000.0m);

        act.Should().Throw<InsufficientInventoryException>();
    }

    [Fact]
    public void CalculateCogs_PartialSell_ConsumesCorrectly()
    {
        var fifo = new FifoQueue();
        fifo.AddPurchase(1.0m, 10000.0m, Ts("2024-01-01T10:00:00+02:00"));
        fifo.AddPurchase(0.5m, 12000.0m, Ts("2024-01-10T10:00:00+02:00"));

        var result = fifo.CalculateCogs(1.2m, Ts("2024-02-01T10:00:00+02:00"), 1.2m * 15000.0m);

        result.CostOfGoodsSold.Should().BeInRange((10000.0m + (0.2m * 12000.0m)) - Constants.Epsilon, (10000.0m + (0.2m * 12000.0m)) + Constants.Epsilon);
        result.AssumedCost.Should().BeInRange(((1.2m * 15000.0m) * 0.2m) - Constants.Epsilon, ((1.2m * 15000.0m) * 0.2m) + Constants.Epsilon);
        result.ConsumedLots.Should().HaveCount(2);
        fifo.Queue.Should().HaveCount(1);
        fifo.Queue.First().Quantity.Should().BeInRange((0.3m) - Constants.Epsilon, (0.3m) + Constants.Epsilon);
        fifo.Queue.First().PricePerUnit.Should().Be(12000.0m);
    }

    [Fact]
    public void CalculateCogs_AcquisitionCostAssumption_CalculatesCorrectly()
    {
        var fifo = new FifoQueue();
        fifo.AddPurchase(1.0m, 1, Ts("2022-01-01T10:00:00+02:00"));
        fifo.AddPurchase(1.0m, 4, Ts("2023-01-01T10:00:00+02:00"));

        var result = fifo.CalculateCogs(2.0m, Ts("2024-02-01T10:00:00+02:00"), 20);

        result.CostOfGoodsSold.Should().BeInRange((5.0m) - Constants.Epsilon, (5.0m) + Constants.Epsilon);
        result.AssumedCost.Should().BeInRange((4.0m) - Constants.Epsilon, (4.0m) + Constants.Epsilon); // 0.2m * 20
        result.ConsumedLots.Should().HaveCount(2);
        fifo.Queue.Should().BeEmpty();
    }

    [Fact]
    public void CalculateCogs_LongHoldingPeriod_Uses40PercentRate()
    {
        var fifo = new FifoQueue();
        fifo.AddPurchase(1.0m, 1, Ts("2010-06-01T10:00:00+02:00"));
        fifo.AddPurchase(1.0m, 4, Ts("2024-01-01T10:00:00+02:00"));

        var result = fifo.CalculateCogs(2.0m, Ts("2024-12-01T10:00:00+02:00"), 20);

        result.CostOfGoodsSold.Should().BeInRange((5.0m) - Constants.Epsilon, (5.0m) + Constants.Epsilon);
        // First lot: 0.4m * 10 = 4.0m, second lot: 0.2m * 10 = 2.0m
        result.AssumedCost.Should().BeInRange((6.0m) - Constants.Epsilon, (6.0m) + Constants.Epsilon);
        result.ConsumedLots.Should().HaveCount(2);
    }

    [Fact]
    public void CalculateCogs_SmallRounding_DoesNotThrow()
    {
        var fifo = new FifoQueue();
        fifo.AddPurchase(0.27622000m, 1.0m, Ts("2024-01-01T10:00:00+02:00"));

        // Sell with tiny floating point excess
        var act = () => fifo.CalculateCogs(0.27622000000001m, Ts("2024-01-02T10:00:00+02:00"), 0.27622000000001m);

        act.Should().NotThrow();
    }
}
