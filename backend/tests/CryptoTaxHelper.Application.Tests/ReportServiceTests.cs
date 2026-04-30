using CryptoTaxHelper.Application.Interfaces;
using CryptoTaxHelper.Application.Models;
using CryptoTaxHelper.Application.Services;
using CryptoTaxHelper.Infrastructure.Brokers.Coinmotion;
using CryptoTaxHelper.Infrastructure.Reports;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CryptoTaxHelper.Application.Tests;

public class ReportServiceTests
{
    private static DateTimeOffset Ts(string iso) => DateTimeOffset.Parse(iso);

    private static ReportService CreateService()
    {
        var parsers = new IBrokerFileParser[] { new CoinmotionCsvParser() };
        var reportGenerator = Substitute.For<IReportGenerator>();
        reportGenerator.GeneratePdfZipAsync(Arg.Any<TaxReport>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Array.Empty<byte>()));
        var reportStore = new InMemoryReportStore();
        return new ReportService(parsers, reportGenerator, reportStore);
    }

    [Fact]
    public void ProcessTransactions_FifoPerCurrency_CalculatesCorrectly()
    {
        var service = CreateService();
        var transactions = new List<NormalizedTransaction>
        {
            new()
            {
                Time = Ts("2024-01-01T10:00:00+02:00"), Type = TransactionType.Buy,
                FromCurrency = "EUR", ToCurrency = "BTC",
                CryptoAmount = 1.0, FiatAmount = 10000.0, Rate = 10000.0,
                Fee = 0.0, FeeCurrency = "EUR", Source = "Coinmotion Oy"
            },
            new()
            {
                Time = Ts("2024-02-01T10:00:00+02:00"), Type = TransactionType.Sell,
                FromCurrency = "BTC", ToCurrency = "EUR",
                CryptoAmount = 0.4, FiatAmount = 6000.0, Rate = 15000.0,
                Fee = 0.0, FeeCurrency = "EUR", Source = "Coinmotion Oy"
            },
            new()
            {
                Time = Ts("2024-01-10T10:00:00+02:00"), Type = TransactionType.Buy,
                FromCurrency = "EUR", ToCurrency = "ETH",
                CryptoAmount = 2.0, FiatAmount = 2000.0, Rate = 1000.0,
                Fee = 0.0, FeeCurrency = "EUR", Source = "Coinmotion Oy"
            },
            new()
            {
                Time = Ts("2024-03-01T10:00:00+02:00"), Type = TransactionType.Sell,
                FromCurrency = "ETH", ToCurrency = "EUR",
                CryptoAmount = 1.0, FiatAmount = 1500.0, Rate = 1500.0,
                Fee = 0.0, FeeCurrency = "EUR", Source = "Coinmotion Oy"
            },
        };

        var report = ReportService.ProcessTransactions(transactions);

        report.Currencies.Should().ContainKey("BTC");
        report.Currencies.Should().ContainKey("ETH");

        var btc2024 = report.Currencies["BTC"].Years["2024"];
        btc2024.Wins.Should().Be(2000.0);
        btc2024.Losses.Should().Be(0);
        btc2024.Total.Should().Be(2000.0);

        var eth2024 = report.Currencies["ETH"].Years["2024"];
        eth2024.Wins.Should().Be(500.0);
        eth2024.Losses.Should().Be(0);
        eth2024.Total.Should().Be(500.0);

        var btcSell = report.Currencies["BTC"].Transactions
            .First(t => t.Type == TransactionType.Sell);
        btcSell.RemainingQuantity.Should().Be(0.6);
        btcSell.CostBasisMethod.Should().Be("fifo");

        var ethSell = report.Currencies["ETH"].Transactions
            .First(t => t.Type == TransactionType.Sell);
        ethSell.RemainingQuantity.Should().Be(1.0);
        ethSell.CostBasisMethod.Should().Be("fifo");
    }

    [Fact]
    public void ProcessTransactions_SmallRounding_DoesNotThrow()
    {
        var service = CreateService();
        var transactions = new List<NormalizedTransaction>
        {
            new()
            {
                Time = Ts("2024-01-01T10:00:00+02:00"), Type = TransactionType.Buy,
                FromCurrency = "EUR", ToCurrency = "XRP",
                CryptoAmount = 0.27622000, FiatAmount = 0.27622000, Rate = 1.0,
                Fee = 0.0, FeeCurrency = "EUR", Source = "Coinmotion Oy"
            },
            new()
            {
                Time = Ts("2024-01-02T10:00:00+02:00"), Type = TransactionType.Sell,
                FromCurrency = "XRP", ToCurrency = "EUR",
                CryptoAmount = 0.27622000000001, FiatAmount = 0.27622000000001, Rate = 1.0,
                Fee = 0.0, FeeCurrency = "EUR", Source = "Coinmotion Oy"
            },
        };

        var report = ReportService.ProcessTransactions(transactions);

        report.Currencies.Should().ContainKey("XRP");
    }

    [Fact]
    public void ProcessTransactions_SplitSell_UsesAssumptionThenFifo()
    {
        var service = CreateService();
        var transactions = new List<NormalizedTransaction>
        {
            new()
            {
                Time = Ts("2010-01-01T10:00:00+02:00"), Type = TransactionType.Buy,
                FromCurrency = "EUR", ToCurrency = "BTC",
                CryptoAmount = 1.0, FiatAmount = 1.0, Rate = 1.0,
                Fee = 0.0, FeeCurrency = "EUR", Source = "Coinmotion Oy"
            },
            new()
            {
                Time = Ts("2022-01-01T10:00:00+02:00"), Type = TransactionType.Buy,
                FromCurrency = "EUR", ToCurrency = "BTC",
                CryptoAmount = 1.0, FiatAmount = 4.0, Rate = 4.0,
                Fee = 0.0, FeeCurrency = "EUR", Source = "Coinmotion Oy"
            },
            new()
            {
                Time = Ts("2024-12-01T10:00:00+02:00"), Type = TransactionType.Sell,
                FromCurrency = "BTC", ToCurrency = "EUR",
                CryptoAmount = 2.0, FiatAmount = 20.0, Rate = 10.0,
                Fee = 0.0, FeeCurrency = "EUR", Source = "Coinmotion Oy"
            },
        };

        var report = ReportService.ProcessTransactions(transactions);

        var sellTxs = report.Currencies["BTC"].Transactions
            .Where(t => t.Type == TransactionType.Sell).ToList();

        sellTxs.Should().HaveCount(2);

        var sell1 = sellTxs[0];
        sell1.CryptoAmount.Should().Be(1.0);
        sell1.CostBasisMethod.Should().Be("assumption");
        sell1.CostBasisUsed.Should().Be(4.0); // 0.4 * 10 = 4.0

        var sell2 = sellTxs[1];
        sell2.CryptoAmount.Should().Be(1.0);
        sell2.CostBasisMethod.Should().Be("fifo");
        sell2.CostBasisUsed.Should().Be(4.0); // actual cost = 4.0 > assumed 0.2*10=2.0
    }

    [Fact]
    public void FilterByYear_ReturnsOnlyMatchingYear()
    {
        var service = CreateService();
        var report = new TaxReport
        {
            Currencies = new Dictionary<string, CurrencyReport>
            {
                ["BTC"] = new CurrencyReport
                {
                    Years = new Dictionary<string, YearSummary>
                    {
                        ["2023"] = new YearSummary { Period = "1.1.2023-31.12.2023", Wins = 100, Total = 100 },
                        ["2024"] = new YearSummary { Period = "1.1.2024-31.12.2024", Wins = 500, Total = 500 }
                    },
                    Transactions = new List<ProcessedTransaction>()
                }
            }
        };

        var filtered = ReportService.FilterByYear(report, 2024);

        filtered.Currencies["BTC"].Years.Should().ContainKey("2024");
        filtered.Currencies["BTC"].Years.Should().NotContainKey("2023");
    }

    [Fact]
    public void CalculateMetrics_SumsCorrectly()
    {
        var service = CreateService();
        var report = new TaxReport
        {
            Currencies = new Dictionary<string, CurrencyReport>
            {
                ["BTC"] = new CurrencyReport
                {
                    Years = new Dictionary<string, YearSummary>
                    {
                        ["2024"] = new YearSummary { Period = "1.1.2024-31.12.2024", Wins = 2000, Losses = 500, Total = 1500 }
                    },
                    Transactions = new List<ProcessedTransaction>
                    {
                        new() { Time = DateTimeOffset.Now, Type = TransactionType.Buy, FromCurrency = "EUR", ToCurrency = "BTC", CryptoAmount = 1, EurAmount = 10000, Rate = 10000, Fee = 0, FeeCurrency = "EUR", Source = "Test" },
                        new() { Time = DateTimeOffset.Now, Type = TransactionType.Sell, FromCurrency = "BTC", ToCurrency = "EUR", CryptoAmount = 0.5, EurAmount = 7500, Rate = 15000, Fee = 0, FeeCurrency = "EUR", Source = "Test" },
                    }
                }
            }
        };

        var metrics = ReportService.CalculateMetrics(report);

        metrics.TotalSalesTransactions.Should().Be(2);
        metrics.TotalSalesVolumeEur.Should().Be(2000);
        metrics.TotalProfitLossEur.Should().Be(1500);
    }
}
