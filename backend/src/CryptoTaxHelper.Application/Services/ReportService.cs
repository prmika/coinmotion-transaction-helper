using CryptoTaxHelper.Application.Interfaces;
using CryptoTaxHelper.Application.Models;
using CryptoTaxHelper.Domain;
using CryptoTaxHelper.Domain.Fifo;

namespace CryptoTaxHelper.Application.Services;

public class ReportService
{
    private readonly IEnumerable<IBrokerFileParser> _parsers;
    private readonly IReportGenerator _reportGenerator;
    private readonly IReportStore _reportStore;

    public ReportService(
        IEnumerable<IBrokerFileParser> parsers,
        IReportGenerator reportGenerator,
        IReportStore reportStore)
    {
        _parsers = parsers;
        _reportGenerator = reportGenerator;
        _reportStore = reportStore;
    }

    public IBrokerFileParser GetParser(string brokerId)
    {
        return _parsers.FirstOrDefault(p => p.BrokerId == brokerId)
            ?? throw new ArgumentException($"No parser registered for broker: {brokerId}");
    }

    public static TaxReport ProcessTransactions(IReadOnlyList<NormalizedTransaction> transactions)
    {
        if (transactions.Count == 0)
            return new TaxReport { Currencies = new Dictionary<string, CurrencyReport>() };

        var brokerName = transactions[0].Source;
        var grouped = GroupByCurrency(transactions);
        var result = new Dictionary<string, CurrencyReport>();

        foreach (var (currency, txList) in grouped)
        {
            var fifo = new FifoQueue();
            var years = new Dictionary<string, YearSummary>();
            var processed = new List<ProcessedTransaction>();

            foreach (var tx in txList)
            {
                ValidateFinancialInputs(tx);
                var txYear = tx.Time.Year.ToString();
                EnsureYearEntry(years, txYear);

                if (tx.Type == TransactionType.Buy)
                {
                    HandleBuy(fifo, tx);
                    processed.Add(ToProcessed(tx));
                }
                else if (tx.Type == TransactionType.Sell)
                {
                    years[txYear].TotalSellVolume += tx.FiatAmount;
                    processed.AddRange(HandleSell(fifo, years, tx, txYear));
                }
                else
                {
                    processed.Add(ToProcessed(tx));
                }
            }

            result[currency] = new CurrencyReport
            {
                Years = years,
                Transactions = processed
            };
        }

        return new TaxReport { Currencies = result, BrokerName = brokerName };
    }

    public static PricingMetrics CalculateMetrics(TaxReport report)
    {
        var totalTransactions = 0;
        var totalVolume = 0.0;
        var totalProfit = 0.0;

        foreach (var (_, data) in report.Currencies)
        {
            totalTransactions += data.Transactions.Count;
            foreach (var (_, yearData) in data.Years)
            {
                totalVolume += yearData.TotalBuyVolume + yearData.TotalSellVolume;
                totalProfit += yearData.Total;
            }
        }

        return new PricingMetrics
        {
            TotalSalesTransactions = totalTransactions,
            TotalSalesVolumeEur = Math.Round(totalVolume, 2),
            TotalProfitLossEur = Math.Round(totalProfit, 2)
        };
    }

    public static TaxReport FilterByYear(TaxReport report, int year)
    {
        var yearStr = year.ToString();
        var filtered = new Dictionary<string, CurrencyReport>();

        foreach (var (currency, data) in report.Currencies)
        {
            if (!data.Years.ContainsKey(yearStr))
                continue;

            filtered[currency] = new CurrencyReport
            {
                Years = new Dictionary<string, YearSummary> { [yearStr] = data.Years[yearStr] },
                Transactions = data.Transactions
            };
        }

        return new TaxReport { Currencies = filtered };
    }

    public async Task<string> GenerateAndStoreReportAsync(TaxReport report, string ownerId, CancellationToken ct = default)
    {
        var zipBytes = await _reportGenerator.GeneratePdfZipAsync(report, ct);
        return _reportStore.Store(zipBytes, ownerId);
    }

    public byte[]? ConsumeReport(string reportId, string ownerId) => _reportStore.Consume(reportId, ownerId);

    private static Dictionary<string, List<NormalizedTransaction>> GroupByCurrency(
        IReadOnlyList<NormalizedTransaction> transactions)
    {
        var groups = new Dictionary<string, List<NormalizedTransaction>>();

        foreach (var tx in transactions)
        {
            if (tx.ToCurrency != "EUR")
            {
                if (!groups.ContainsKey(tx.ToCurrency))
                    groups[tx.ToCurrency] = new List<NormalizedTransaction>();
                groups[tx.ToCurrency].Add(tx);
            }

            if (tx.FromCurrency != "EUR")
            {
                if (!groups.ContainsKey(tx.FromCurrency))
                    groups[tx.FromCurrency] = new List<NormalizedTransaction>();
                groups[tx.FromCurrency].Add(tx);
            }
        }

        return groups;
    }

    private static void EnsureYearEntry(Dictionary<string, YearSummary> years, string year)
    {
        if (!years.ContainsKey(year))
        {
            years[year] = new YearSummary
            {
                Period = $"1.1.{year}-31.12.{year}",
                Wins = 0,
                Losses = 0,
                Total = 0,
                TotalBuyVolume = 0,
                TotalSellVolume = 0
            };
        }
    }

    private static void HandleBuy(FifoQueue fifo, NormalizedTransaction tx)
    {
        if (tx.CryptoAmount <= 0) return;
        var acquisitionCost = tx.FiatAmount + (tx.FeeCurrency == "EUR" ? tx.Fee : 0.0);
        var pricePerUnit = acquisitionCost / tx.CryptoAmount;
        fifo.AddPurchase(tx.CryptoAmount, pricePerUnit, tx.Time);
    }

    private static List<ProcessedTransaction> HandleSell(
        FifoQueue fifo, Dictionary<string, YearSummary> years,
        NormalizedTransaction tx, string txYear)
    {
        if (tx.CryptoAmount <= 0) return [];

        var totalRevenue = tx.FiatAmount;
        var feeEur = tx.FeeCurrency == "EUR" ? tx.Fee : 0.0;

        var result = fifo.CalculateCogs(tx.CryptoAmount, tx.Time, totalRevenue);
        var pricePerUnit = totalRevenue / tx.CryptoAmount;
        var remainingBefore = fifo.RemainingQuantity() + tx.CryptoAmount;
        var cumulativeSold = 0.0;
        var splitTransactions = new List<ProcessedTransaction>();

        foreach (var lot in result.ConsumedLots)
        {
            var lotRevenue = lot.Quantity * pricePerUnit;
            var lotCostBasis = lot.Quantity * lot.PricePerUnit;
            var heldDays = (tx.Time - lot.PurchaseTime).TotalDays;
            var lotAssumedCost = lotRevenue * (heldDays >= Constants.LongHoldingPeriodDays
                ? Constants.LongHoldingAssumedRate
                : Constants.ShortHoldingAssumedRate);
            var lotCostBasisUsed = Math.Max(lotCostBasis, lotAssumedCost);
            var lotMethod = lotAssumedCost > lotCostBasis ? "assumption" : "fifo";

            var lotFee = 0.0;
            if (feeEur > 0 && totalRevenue > 0)
                lotFee = feeEur * (lotRevenue / totalRevenue);

            var lotProfitLoss = lotRevenue - lotFee - lotCostBasisUsed;
            var roundedRevenue = RoundMoney(lotRevenue);
            var roundedCostBasis = RoundMoney(lotCostBasis);
            var roundedAssumedCost = RoundMoney(lotAssumedCost);
            var roundedCostBasisUsed = RoundMoney(lotCostBasisUsed);
            var roundedFee = RoundMoney(lotFee);
            var roundedProfitLoss = RoundMoney(lotProfitLoss);

            cumulativeSold += lot.Quantity;
            var remainingAfter = remainingBefore - cumulativeSold;

            if (lotProfitLoss > 0)
            {
                years[txYear].Wins += (double)roundedProfitLoss;
                years[txYear].ProfitSellVolume += (double)roundedRevenue;
                years[txYear].ProfitBuyVolume += (double)roundedCostBasisUsed;
                years[txYear].ProfitFees += (double)roundedFee;
            }
            else
            {
                years[txYear].Losses += Math.Abs((double)roundedProfitLoss);
                years[txYear].LossSellVolume += (double)roundedRevenue;
                years[txYear].LossBuyVolume += (double)roundedCostBasisUsed;
                years[txYear].LossFees += (double)roundedFee;
            }

            years[txYear].TotalBuyVolume += (double)roundedCostBasisUsed;
            years[txYear].Total += (double)roundedProfitLoss;

            splitTransactions.Add(new ProcessedTransaction
            {
                Time = tx.Time,
                Type = TransactionType.Sell,
                FromCurrency = tx.FromCurrency,
                ToCurrency = tx.ToCurrency,
                CryptoAmount = lot.Quantity,
                EurAmount = (double)roundedRevenue,
                Rate = tx.Rate,
                Fee = (double)roundedFee,
                FeeCurrency = tx.FeeCurrency,
                Source = tx.Source,
                CostBasis = (double)roundedCostBasis,
                AssumedCost = (double)roundedAssumedCost,
                CostBasisUsed = (double)roundedCostBasisUsed,
                CostBasisMethod = lotMethod,
                ProfitLoss = (double)roundedProfitLoss,
                RemainingQuantity = remainingAfter
            });
        }

        return splitTransactions;
    }

    private static decimal RoundMoney(double value) =>
        decimal.Round(Convert.ToDecimal(value), 2, MidpointRounding.AwayFromZero);

    private static void ValidateFinancialInputs(NormalizedTransaction tx)
    {
        if (!double.IsFinite(tx.CryptoAmount) || tx.CryptoAmount < 0)
            throw new ArgumentException("Crypto amount must be finite and non-negative", nameof(tx));
        if (!double.IsFinite(tx.FiatAmount) || tx.FiatAmount < 0)
            throw new ArgumentException("Fiat amount must be finite and non-negative", nameof(tx));
        if (!double.IsFinite(tx.Fee) || tx.Fee < 0)
            throw new ArgumentException("Fee must be finite and non-negative", nameof(tx));
        if (!double.IsFinite(tx.Rate) || tx.Rate < 0)
            throw new ArgumentException("Rate must be finite and non-negative", nameof(tx));
    }

    private static ProcessedTransaction ToProcessed(NormalizedTransaction tx)
    {
        return new ProcessedTransaction
        {
            Time = tx.Time,
            Type = tx.Type,
            FromCurrency = tx.FromCurrency,
            ToCurrency = tx.ToCurrency,
            CryptoAmount = tx.CryptoAmount,
            EurAmount = tx.FiatAmount,
            Rate = tx.Rate,
            Fee = tx.Fee,
            FeeCurrency = tx.FeeCurrency,
            Source = tx.Source
        };
    }
}
