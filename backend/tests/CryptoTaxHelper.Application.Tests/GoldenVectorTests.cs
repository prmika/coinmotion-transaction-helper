using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using CryptoTaxHelper.Application.Models;
using CryptoTaxHelper.Application.Services;
using CryptoTaxHelper.Infrastructure.Brokers.Coinmotion;
using FluentAssertions;
using Xunit;

namespace CryptoTaxHelper.Application.Tests;

// Golden vectors for tax-calculation numerics (issue #14 follow-up: double -> decimal migration).
// All inputs are synthetic. Expected outputs are committed under Golden/ and must not change
// during a numeric-type migration. Regenerate only with GOLDEN_UPDATE=1 after human review.
public class GoldenVectorTests
{
    public static IEnumerable<object[]> Scenarios()
    {
        foreach (var (name, _) in Inputs)
            yield return [name];
    }

    private static readonly (string Name, string Csv)[] Inputs =
    [
        ("basic_fifo_with_fees", """
            time,type,fromCurrency,toCurrency,eurAmount,cryptoAmount,rate,fee,feeCurrency
            2021-01-10T10:00:00+02:00,buy,EUR,BTC,1000,0.05,20000,5,EUR
            2021-02-10T10:00:00+02:00,buy,EUR,BTC,2000,0.08,25000,10,EUR
            2021-06-01T10:00:00+03:00,sell,BTC,EUR,3000,0.09,33333.33,7.5,EUR
            2021-09-01T10:00:00+03:00,sell,BTC,EUR,1200,0.02,60000,2,EUR
            """),
        ("long_holding_assumed_cost", """
            time,type,fromCurrency,toCurrency,eurAmount,cryptoAmount,rate,fee,feeCurrency
            2010-03-01T10:00:00+02:00,buy,EUR,BTC,100,1.0,100,0,EUR
            2024-03-01T10:00:00+02:00,sell,BTC,EUR,50000,1.0,50000,0,EUR
            """),
        ("half_cent_rounding", """
            time,type,fromCurrency,toCurrency,eurAmount,cryptoAmount,rate,fee,feeCurrency
            2022-01-01T10:00:00+02:00,buy,EUR,ETH,10.005,0.3,33.35,0,EUR
            2022-02-01T10:00:00+02:00,sell,ETH,EUR,5.005,0.1,50.05,0.005,EUR
            2022-03-01T10:00:00+02:00,sell,ETH,EUR,5.125,0.2,25.625,0,EUR
            """),
        ("fractional_residue_sells", """
            time,type,fromCurrency,toCurrency,eurAmount,cryptoAmount,rate,fee,feeCurrency
            2023-01-01T10:00:00+02:00,buy,EUR,BTC,3,0.3,10,0,EUR
            2023-02-01T10:00:00+02:00,sell,BTC,EUR,1.01,0.1,10.1,0,EUR
            2023-03-01T10:00:00+02:00,sell,BTC,EUR,2.03,0.2,10.15,0,EUR
            """),
        ("multi_currency_year_boundary", """
            time,type,fromCurrency,toCurrency,eurAmount,cryptoAmount,rate,fee,feeCurrency
            2024-12-30T10:00:00+02:00,buy,EUR,ETH,400,2.0,200,0,EUR
            2024-12-30T11:00:00+02:00,buy,EUR,BTC,30000,1.0,30000,0,EUR
            2025-01-02T10:00:00+02:00,sell,ETH,EUR,900,1.5,600,3,EUR
            2025-01-03T10:00:00+02:00,sell,BTC,EUR,31000,0.5,62000,0,EUR
            2026-02-01T10:00:00+02:00,sell,ETH,EUR,200,0.5,400,1,EUR
            """),
        ("insufficient_inventory_blocked", """
            time,type,fromCurrency,toCurrency,eurAmount,cryptoAmount,rate,fee,feeCurrency
            2022-01-01T10:00:00+02:00,buy,EUR,BTC,100,0.01,10000,0,EUR
            2022-02-01T10:00:00+02:00,sell,BTC,EUR,500,0.02,25000,0,EUR
            """),
    ];

    [Theory]
    [MemberData(nameof(Scenarios))]
    public async Task Scenario_MatchesGoldenSnapshot(string name)
    {
        var csv = Inputs.Single(i => i.Name == name).Csv;
        var parser = new CoinmotionCsvParser();
        var txs = await parser.ParseAsync(new MemoryStream(Encoding.UTF8.GetBytes(csv)));
        var report = ReportService.ProcessTransactions(txs.ToList());
        var snapshot = Snapshot(report);

        var path = GoldenPath(name);
        if (Environment.GetEnvironmentVariable("GOLDEN_UPDATE") == "1")
        {
            File.WriteAllText(path, snapshot, new UTF8Encoding(false));
            return;
        }

        File.Exists(path).Should().BeTrue($"golden file missing: {path}");
        snapshot.Should().Be(File.ReadAllText(path, Encoding.UTF8).Replace("\r\n", "\n"));
    }

    private static string GoldenPath(string name, [CallerFilePath] string? src = null) =>
        Path.Combine(Path.GetDirectoryName(src)!, "Golden", name + ".golden.txt");

    // Canonical text form. Values are rounded to 2 dp (EUR) or 8 dp (crypto), invariant culture,
    // and never printed in scientific notation, so the same snapshot is produced for double and decimal.
    internal static string Snapshot(TaxReport report)
    {
        var sb = new StringBuilder();
        sb.Append("status=").Append(report.ValidationStatus ?? "ok").Append('\n');
        sb.Append("broker=").Append(report.BrokerName ?? "-").Append('\n');
        foreach (var d in report.InventoryDeficits ?? [])
            sb.Append($"deficit asset={d.Asset} row={d.SourceRow}\n");

        foreach (var (cur, data) in report.Currencies.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            sb.Append("currency=").Append(cur).Append('\n');
            foreach (var (year, y) in data.Years.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                sb.Append($"year={year} wins={E(y.Wins)} losses={E(y.Losses)} total={E(y.Total)} ")
                  .Append($"buyVol={E(y.TotalBuyVolume)} sellVol={E(y.TotalSellVolume)} ")
                  .Append($"profSell={E(y.ProfitSellVolume)} profBuy={E(y.ProfitBuyVolume)} profFees={E(y.ProfitFees)} ")
                  .Append($"lossSell={E(y.LossSellVolume)} lossBuy={E(y.LossBuyVolume)} lossFees={E(y.LossFees)}\n");
            }
            foreach (var t in data.Transactions)
            {
                var time = t.Time.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
                sb.Append($"tx {time} {t.Type} amt={C(t.CryptoAmount)} eur={E(t.EurAmount)} ")
                  .Append($"cb={O(t.CostBasis)} assumed={O(t.AssumedCost)} cbUsed={O(t.CostBasisUsed)} ")
                  .Append($"method={t.CostBasisMethod ?? "-"} pl={O(t.ProfitLoss)} rem={OC(t.RemainingQuantity)} fee={E(t.Fee)}\n");
            }
        }
        return sb.ToString();
    }

    private static string E(object v) => Money(v);
    private static string C(object v) => Crypto(v);
    private static string O(object? v) => v is null ? "-" : Money(v);
    private static string OC(object? v) => v is null ? "-" : Crypto(v);

    private static string Money(object v)
    {
        var d = Convert.ToDecimal(v, CultureInfo.InvariantCulture);
        d = Math.Round(d, 2, MidpointRounding.AwayFromZero);
        if (d == 0) d = 0m;
        return d.ToString("F2", CultureInfo.InvariantCulture);
    }

    private static string Crypto(object v)
    {
        var d = Convert.ToDecimal(v, CultureInfo.InvariantCulture);
        d = Math.Round(d, 8, MidpointRounding.AwayFromZero);
        if (d == 0) d = 0m;
        return d.ToString("F8", CultureInfo.InvariantCulture);
    }
}
