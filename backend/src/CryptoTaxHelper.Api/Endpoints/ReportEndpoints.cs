using CryptoTaxHelper.Application.Services;

namespace CryptoTaxHelper.Api.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this WebApplication app)
    {
        app.MapPost("/report/generate", GenerateReport)
            .DisableAntiforgery();

        app.MapGet("/report/download/{reportId}", DownloadReport);
    }

    private static async Task<IResult> GenerateReport(
        IFormFile file,
        int? year,
        ReportService reportService,
        CancellationToken ct)
    {
        if (file is null || !file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { detail = "Upload a .csv file" });

        if (file.Length == 0)
            return Results.BadRequest(new { detail = "File is empty" });

        try
        {
            var parser = reportService.GetParser("coinmotion");

            using var stream = file.OpenReadStream();
            var transactions = await parser.ParseAsync(stream, ct);

            if (transactions.Count == 0)
                return Results.BadRequest(new { detail = "No valid transactions found in CSV" });

            var report = ReportService.ProcessTransactions(transactions);

            if (report.InventoryDeficits.Count > 0)
            {
                return Results.UnprocessableEntity(new
                {
                    code = "inventory_reconciliation_failed",
                    status = report.ValidationStatus,
                    detail = "Inventory reconciliation failed. The imported data does not contain enough crypto quantity for one or more sales.",
                    message_fi = "Tietomäärän täsmäytys ei täsmää. Tarkista alkuperäinen Coinmotion-vienti ja muut lompakot tai pörssit. Älä lisää keinotekoista nollariviä.",
                    message_en = "Inventory reconciliation failed. Check the original Coinmotion export and other wallets or exchanges. Do not add an artificial zero-cost row.",
                    deficits = report.InventoryDeficits
                });
            }

            if (year.HasValue)
            {
                report = ReportService.FilterByYear(report, year.Value);
                if (report.Currencies.Count == 0)
                    return Results.BadRequest(new { detail = $"No report data found for year {year}." });
            }

            var metrics = ReportService.CalculateMetrics(report);
            var reportId = await reportService.GenerateAndStoreReportAsync(report, ct);

            return Results.Ok(new
            {
                report_id = reportId,
                pricing_metrics = new
                {
                    total_sales_transactions = metrics.TotalSalesTransactions,
                    total_sales_volume_eur = metrics.TotalSalesVolumeEur,
                    total_profit_loss_eur = metrics.TotalProfitLossEur
                }
            });
        }
        catch (FormatException ex)
        {
            return Results.BadRequest(new { detail = ex.Message });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Results.BadRequest(new { detail = ex.Message });
        }
    }

    private static IResult DownloadReport(string reportId, ReportService reportService)
    {
        var data = reportService.RetrieveReport(reportId);
        if (data is null)
            return Results.NotFound(new { detail = "Report not found or has expired" });

        reportService.RemoveReport(reportId);

        return Results.File(data, "application/zip", "pdf_reports.zip");
    }
}
