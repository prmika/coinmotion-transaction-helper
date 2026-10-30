using System.Globalization;
using CryptoTaxHelper.Application.Interfaces;
using CryptoTaxHelper.Application.Models;

namespace CryptoTaxHelper.Infrastructure.Brokers.Coinmotion;

public class CoinmotionCsvParser : IBrokerFileParser
{
    public string BrokerId => "coinmotion";
    public string[] SupportedFileExtensions => [".csv"];

    public async Task<IReadOnlyList<NormalizedTransaction>> ParseAsync(Stream fileStream, CancellationToken ct = default)
    {
        using var reader = new StreamReader(fileStream, System.Text.Encoding.UTF8);
        var content = await reader.ReadToEndAsync(ct);
        return Parse(content);
    }

    internal static IReadOnlyList<NormalizedTransaction> Parse(string csvContent)
    {
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
            return [];

        var headers = ParseCsvLine(lines[0]);
        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Length; i++)
            headerIndex[headers[i].Trim()] = i;

        var transactions = new List<NormalizedTransaction>();

        for (var lineNum = 1; lineNum < lines.Length; lineNum++)
        {
            var fields = ParseCsvLine(lines[lineNum]);
            if (fields.Length < headers.Length)
                continue;

            try
            {
                var fromCurrency = GetField(fields, headerIndex, "fromCurrency").Trim().ToUpperInvariant();
                var toCurrency = GetField(fields, headerIndex, "toCurrency").Trim().ToUpperInvariant();
                var type = GetField(fields, headerIndex, "type").Trim().ToLowerInvariant();
                var eurAmount = ParseDouble(GetField(fields, headerIndex, "eurAmount"));
                var cryptoAmount = ParseDouble(GetField(fields, headerIndex, "cryptoAmount"));
                var rate = ParseDouble(GetField(fields, headerIndex, "rate"));
                var fee = ParseDouble(GetField(fields, headerIndex, "fee"));
                var feeCurrency = GetField(fields, headerIndex, "feeCurrency").Trim().ToUpperInvariant();
                var time = DateTimeOffset.Parse(GetField(fields, headerIndex, "time"), CultureInfo.InvariantCulture);

                if (type is "deposit" or "withdrawal")
                    continue;

                NormalizedTransaction tx;

                if (type == "account_transfer_in")
                {
                    tx = new NormalizedTransaction
                    {
                        Time = time,
                        Type = TransactionType.Buy,
                        FromCurrency = "EUR",
                        ToCurrency = toCurrency,
                        CryptoAmount = cryptoAmount,
                        FiatAmount = 0,
                        Rate = rate,
                        Fee = fee,
                        FeeCurrency = feeCurrency,
                        Source = "Coinmotion Oy"
                    };
                }
                else if (fromCurrency == "EUR" && toCurrency != "EUR")
                {
                    tx = new NormalizedTransaction
                    {
                        Time = time,
                        Type = TransactionType.Buy,
                        FromCurrency = fromCurrency,
                        ToCurrency = toCurrency,
                        CryptoAmount = cryptoAmount,
                        FiatAmount = eurAmount,
                        Rate = rate,
                        Fee = fee,
                        FeeCurrency = feeCurrency,
                        Source = "Coinmotion Oy"
                    };
                }
                else if (toCurrency == "EUR" && fromCurrency != "EUR")
                {
                    tx = new NormalizedTransaction
                    {
                        Time = time,
                        Type = TransactionType.Sell,
                        FromCurrency = fromCurrency,
                        ToCurrency = toCurrency,
                        CryptoAmount = cryptoAmount,
                        FiatAmount = eurAmount,
                        Rate = rate,
                        Fee = fee,
                        FeeCurrency = feeCurrency,
                        Source = "Coinmotion Oy"
                    };
                }
                else
                {
                    continue;
                }

                transactions.Add(tx);
            }
            catch (Exception ex)
            {
                throw new FormatException($"Invalid CSV data on row {lineNum + 1}.", ex);
            }
        }

        return transactions.OrderBy(t => t.Time).ToList();
    }

    private static string GetField(string[] fields, Dictionary<string, int> headerIndex, string columnName)
    {
        if (!headerIndex.TryGetValue(columnName, out var index))
            throw new FormatException($"Missing required column: {columnName}");
        return fields[index];
    }

    private static double ParseDouble(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0.0;
        return double.Parse(value, CultureInfo.InvariantCulture);
    }

    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = "";
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current);
                current = "";
            }
            else if (c == '\r')
            {
                continue;
            }
            else
            {
                current += c;
            }
        }

        fields.Add(current);
        return fields.ToArray();
    }
}
