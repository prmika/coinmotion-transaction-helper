using CryptoTaxHelper.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CryptoTaxHelper.Api.Endpoints;

public static class ReportEndpoints
{
    private const string ReportProcessingError = "Unable to process the uploaded file.";
    public static void MapReportEndpoints(this WebApplication app)
    {
        app.MapPost("/report/generate", GenerateReport)
            .RequireAuthorization("Reports")
            .DisableAntiforgery();

        app.MapGet("/report/download/{reportId}", DownloadReport)
            .RequireAuthorization("Reports");
    }

    private static async Task<IResult> GenerateReport(
        IFormFile file,
        int? year,
        ReportService reportService,
        [FromServices] ILoggerFactory loggerFactory,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ReportEndpoints");
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

            if (year.HasValue)
            {
                report = ReportService.FilterByYear(report, year.Value);
                if (report.Currencies.Count == 0)
                    return Results.BadRequest(new { detail = $"No report data found for year {year}." });
            }

            var metrics = ReportService.CalculateMetrics(report);
            var ownerId = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(ownerId)) return Results.Forbid();
            var reportId = await reportService.GenerateAndStoreReportAsync(report, ownerId, ct);

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
            logger.LogWarning(ex, "The uploaded report file could not be parsed.");
            return Results.BadRequest(new { detail = ReportProcessingError });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Report generation failed.");
            return Results.BadRequest(new { detail = ReportProcessingError });
        }
    }

    private static IResult DownloadReport(string reportId, ReportService reportService, ClaimsPrincipal user)
    {
        var ownerId = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId)) return Results.Forbid();
        var data = reportService.ConsumeReport(reportId, ownerId);
        if (data is null)
            return Results.NotFound(new { detail = "Report not found or has expired" });

        return Results.File(data, "application/zip", "pdf_reports.zip");
    }
}
