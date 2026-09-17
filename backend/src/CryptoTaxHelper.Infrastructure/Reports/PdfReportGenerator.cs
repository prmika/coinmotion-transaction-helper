using System.IO.Compression;
using CryptoTaxHelper.Application.Interfaces;
using CryptoTaxHelper.Application.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CryptoTaxHelper.Infrastructure.Reports;

public class PdfReportGenerator : IReportGenerator
{
    private const string DisclaimerFiTemplate =
        "Tämä raportti on automaattisesti muodostettu {0}n toimittamien transaktiotietojen sekä käyttäjän antamien lähtötietojen perusteella.\n\n" +
        "Raportti on suuntaa-antava eikä ole veroneuvontaa. Palvelu ei takaa raportin tietojen täydellisyyttä, oikeellisuutta tai soveltuvuutta käyttäjän yksittäiseen verotustilanteeseen. Käyttäjä vastaa itse tietojen oikeellisuudesta ja veroilmoitukselle ilmoitettavista luvuista.\n\n" +
        "Raportti ei välttämättä huomioi oikein tai kattavasti kaikkia seuraavia tapahtumia:\n" +
        "• lompakkojen välisiä siirtoja\n" +
        "• ulkopuolisista pörsseistä tai palveluista tehtyjä transaktioita\n" +
        "• staking-, lending- tai muita tuottotapahtumia\n" +
        "• DeFi-tapahtumia\n" +
        "• airdroppeja ja hard fork -tapahtumia\n" +
        "• NFT-kauppaa\n\n" +
        "Raportissa esitetyt laskelmat (esim. todellinen hankintahinta tai hankintameno-olettama) ovat laskennallisia. Hankintameno-olettaman käyttö ja lopullinen verotuksellinen valinta on aina käyttäjän vastuulla.\n\n" +
        "Palvelun tarjoaja ei vastaa mahdollisista veroseuraamuksista, veronkorotuksista tai muista vahingoista, jotka aiheutuvat raportin käytöstä.\n\n" +
        "Ajantasaiset ja sitovat ohjeet löytyvät Verohallinnon verkkosivuilta. Epäselvissä tilanteissa suositellaan ottamaan yhteyttä veroasiantuntijaan.";

    private const string DisclaimerEnTemplate =
        "This report has been automatically generated based on transaction data provided by {0} and information supplied by the user.\n\n" +
        "This report is for informational purposes only and does not constitute tax advice. The service does not guarantee the completeness, accuracy, or suitability of the report for the user's individual tax situation. The user is solely responsible for verifying the correctness of the information and the figures reported to the tax authorities.\n\n" +
        "The report may not fully or correctly account for the following events:\n" +
        "• transfers between wallets\n" +
        "• transactions from external exchanges or services\n" +
        "• staking, lending, or yield-related income\n" +
        "• DeFi transactions\n" +
        "• airdrops and hard forks\n" +
        "• NFT transactions\n\n" +
        "Any calculations presented in the report (e.g. actual acquisition cost or deemed acquisition cost) are estimates. The choice and applicability of the deemed acquisition cost method is always the responsibility of the user.\n\n" +
        "The service provider shall not be held liable for any tax consequences, penalties, or damages arising from the use of this report.\n\n" +
        "For official and binding guidance, please refer to the Finnish Tax Administration or consult a qualified tax professional.";

    private static readonly (string Key, string Description)[] DictionaryEntries =
    [
        ("Year", "Vuosi"),
        ("From Time", "Laskentajakso"),
        ("Wins €", "Voitot yhteensä euroina"),
        ("Losses €", "Tappiot yhteensä euroina"),
        ("Total €", "Nettovoitto/-tappio euroina"),
        ("Time", "Tapahtuma-aika"),
        ("type", "Tapahtumatyyppi"),
        ("buy", "Ostotapahtuma"),
        ("sell", "Myyntitapahtuma"),
        ("Crypto Amount", "Kryptovaluutan määrä"),
        ("Amount €", "Euro määrä"),
        ("Rate", "Kryptovaluutan kurssi euroissa"),
        ("From Currency", "Mistä valuutasta"),
        ("To Currency", "Mihin valuuttaan"),
        ("Remaining Quantity", "Jäljellä oleva määrä"),
        ("Cost Basis €", "Hankintameno"),
        ("Assumed Cost €", "Hankintameno-olettama 20% tai 40% omistusajan mukaan"),
        ("Cost Basis Used", "Käytetty hankintameno"),
        ("Cost Basis Method", "Hankintamenomenetelmä"),
        ("Profit/Loss €", "Myyntivoitto/-tappio"),
        ("fifo", "First In First Out -menetelmä (hankintameno)"),
        ("assumption", "Hankintameno-olettama"),
    ];
    public Task<byte[]> GeneratePdfZipAsync(TaxReport report, CancellationToken ct = default)
    {
        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (currency, data) in report.Currencies)
            {
                ct.ThrowIfCancellationRequested();

                var pdfBytes = GenerateCurrencyPdf(currency, data);
                var entry = archive.CreateEntry($"{currency}_report.pdf", CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                entryStream.Write(pdfBytes);
            }
        }

        zipStream.Position = 0;
        return Task.FromResult(zipStream.ToArray());
    }

    private static byte[] GenerateCurrencyPdf(string currency, CurrencyReport data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);

                page.Header().Column(header =>
                {
                    header.Item().Text($"Tax Report — {currency}").FontSize(20).Bold();
                    header.Item().Text($"Generated on {DateTime.UtcNow:dd.MM.yyyy HH:mm:ss}").FontSize(8).Italic();
                    header.Item().Text($"Tämä raportti on automaattisesti muodostettu välittäjän toimittamien transaktiotietojen sekä käyttäjän antamien lähtötietojen perusteella. / This report has been automatically generated based on transaction data provided by the broker and information supplied by the user.")
                        .FontSize(7);
                });

                page.Content().Column(col =>
                {
                    col.Spacing(15);

                    if (data.ValidationStatus != "complete")
                    {
                        col.Item().Background(Colors.Red.Lighten4).Padding(8).Text(
                            "RECONCILIATION REQUIRES REVIEW / TÄSMÄYTYS VAATII TARKISTUKSEN: " +
                            "This report must not be treated as complete. / Tätä raporttia ei tule pitää täydellisenä.").FontSize(10).Bold();
                    }

                    // Dictionary table
                    col.Item().Text("Dictionary").FontSize(14).Bold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(5);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Key").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Description").Bold().FontSize(8);
                        });

                        foreach (var (key, desc) in DictionaryEntries)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(key).FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(desc).FontSize(8);
                        }
                    });

                    // Year summary table
                    col.Item().Text("Yearly Summary *").FontSize(14).Bold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Period").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Gains (EUR)").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Losses (EUR)").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Net (EUR)").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Total Buy Volume (EUR)").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Total Sell Volume (EUR)").Bold().FontSize(8);
                        });

                        foreach (var (year, summary) in data.Years.OrderBy(y => y.Key))
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(summary.Period).FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{summary.Wins:F2}").FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{summary.Losses:F2}").FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{summary.Total:F2}").FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{summary.TotalBuyVolume:F2}").FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{summary.TotalSellVolume:F2}").FontSize(8);
                        }
                    });
                    col.Item().Text("* Luvuista on vähennetty mahdolliset osto- ja myyntikulut. / The figures have been reduced by possible purchase and sale fees.").FontSize(7).Italic();

                    // Tax declaration table
                    col.Item().Text("Tax Declaration / Veroilmoitus").FontSize(14).Bold();
                    col.Item().Text("Erittely verotusta varten: voitolliset ja tappiolliset myynnit erikseen. / Breakdown for tax declaration: profitable and loss-making sales separated.").FontSize(7).Italic();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2); // Year
                            columns.RelativeColumn(1); // Category
                            columns.RelativeColumn(2); // Sell Revenue
                            columns.RelativeColumn(2); // Acquisition Cost
                            columns.RelativeColumn(2); // Fees
                            columns.RelativeColumn(2); // Profit/Loss
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Year").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Category").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Sell Revenue (EUR)").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Acquisition Cost (EUR)").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Selling Fees (EUR)").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Profit/Loss (EUR)").Bold().FontSize(8);
                        });

                        foreach (var (year, summary) in data.Years.OrderBy(y => y.Key))
                        {
                            // Profit row
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(year).FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text("Profit").FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{summary.ProfitSellVolume:F2}").FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{summary.ProfitBuyVolume:F2}").FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{summary.ProfitFees:F2}").FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{summary.Wins:F2}").FontSize(8);

                            // Loss row
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text("").FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text("Loss").FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{summary.LossSellVolume:F2}").FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{summary.LossBuyVolume:F2}").FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{summary.LossFees:F2}").FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"-{summary.Losses:F2}").FontSize(8);
                        }
                    });

                    // Transactions table
                    col.Item().Text("Transactions **").FontSize(14).Bold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3); // Time
                            columns.RelativeColumn(1); // Type
                            columns.RelativeColumn(2); // Amount
                            columns.RelativeColumn(2); // EUR
                            columns.RelativeColumn(2); // Cost Basis
                            columns.RelativeColumn(2); // Method
                            columns.RelativeColumn(2); // P/L
                            columns.RelativeColumn(2); // Remaining
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Time").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Type").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Amount").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("EUR").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Cost Basis").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Method").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Profit/Loss").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Remaining").Bold().FontSize(8);
                        });

                        foreach (var tx in data.Transactions)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(tx.Time.ToString("dd.MM.yyyy HH:mm:ss")).FontSize(7);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(tx.Type.ToString()).FontSize(7);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(2).Text($"{tx.CryptoAmount:G}").FontSize(7);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(2).Text($"{tx.EurAmount:F2}").FontSize(7);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(tx.CostBasisUsed.HasValue ? $"{tx.CostBasisUsed:F2}" : "-").FontSize(7);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(tx.CostBasisMethod ?? "-").FontSize(7);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(tx.ProfitLoss.HasValue ? $"{tx.ProfitLoss:F2}" : "-").FontSize(7);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(tx.RemainingQuantity.HasValue ? $"{tx.RemainingQuantity:G}" : "-").FontSize(7);
                        }
                    });
                    col.Item().Text("** Voitto/tappio on laskettu Amount € - Cost Basis Used - Selling fee €. / Profit/loss is calculated as Amount € - Cost Basis Used - Selling fee €.").FontSize(7).Italic();

                    // Disclaimers
                    col.Item().PaddingTop(20).Text("Disclaimer").FontSize(14).Bold();
                    col.Item().Text(string.Format(DisclaimerFiTemplate, "välittäjän")).FontSize(7);
                    col.Item().PaddingTop(10).Text(string.Format(DisclaimerEnTemplate, "broker")).FontSize(7);
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generated by CryptoTaxHelper — ");
                    text.Span(DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm")).Italic();
                });
            });
        });

        return document.GeneratePdf();
    }
}
