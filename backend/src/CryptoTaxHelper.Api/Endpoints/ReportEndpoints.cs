using CryptoTaxHelper.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace CryptoTaxHelper.Api.Endpoints;

public static class ReportEndpoints
{
    private const string ReportProcessingError = "Unable to process the uploaded file.";
    public const string GenerateRateLimitPolicy = "report-generate";
    public const string DownloadRateLimitPolicy = "report-download";

    public static void MapReportEndpoints(this WebApplication app)
    {
        // Anonymous use is allowed. Logged-in users become the report owner; anonymous reports are fetched by report ID.
        app.MapPost("/report/generate", GenerateReport)
            .DisableAntiforgery()
            .RequireRateLimiting(GenerateRateLimitPolicy);

        app.MapGet("/report/download/{reportId}", DownloadReport)
            .RequireRateLimiting(DownloadRateLimitPolicy);
    }

    private static async Task<IResult> GenerateReport(
        HttpRequest request,
        int? year,
        ReportService reportService,
        [FromServices] ILoggerFactory loggerFactory,
        [FromServices] IOptions<HttpLimitsOptions> limits,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ReportEndpoints");
        var maxBytes = limits.Value.Upload.MaxBytes;

        // Fast reject when the client declares an oversize body; nothing is read.
        if (request.ContentLength is long declared && declared > maxBytes + MultipartOverheadBytes)
            return UploadTooLarge(maxBytes);

        // Read the multipart body. Kestrel's body cap (see Program.cs) rejects oversize chunked bodies
        // with BadHttpRequestException(413) during the read; the multipart reader spools large parts to disk.
        IFormCollection form;
        try
        {
            form = await request.ReadFormAsync(ct);
        }
        catch (BadHttpRequestException ex) when (ex.StatusCode == StatusCodes.Status413RequestEntityTooLarge)
        {
            return UploadTooLarge(maxBytes);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException)
        {
            logger.LogWarning(ex, "The uploaded multipart body could not be read.");
            return Results.BadRequest(new { detail = ReportProcessingError });
        }

        var file = form.Files.GetFile("file");
        if (file is null || !file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { detail = "Upload a .csv file" });

        if (file.Length == 0)
            return Results.BadRequest(new { detail = "File is empty" });

        if (file.Length > maxBytes)
            return UploadTooLarge(maxBytes);

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
            var ownerId = OwnerIdOrNull(user);
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

    private const long MultipartOverheadBytes = 64 * 1024;

    private static IResult UploadTooLarge(long maxBytes) =>
        Results.Json(new
        {
            code = "upload_too_large",
            detail = $"The uploaded file is larger than the {maxBytes / (1024 * 1024)} MB limit."
        }, statusCode: StatusCodes.Status413RequestEntityTooLarge);

    private static IResult DownloadReport(string reportId, ReportService reportService, ClaimsPrincipal user)
    {
        var ownerId = OwnerIdOrNull(user);
        var data = reportService.ConsumeReport(reportId, ownerId);
        if (data is null)
            return Results.NotFound(new { detail = "Report not found or has expired" });

        return Results.File(data, "application/zip", "pdf_reports.zip");
    }

    private static string? OwnerIdOrNull(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true) return null;
        var ownerId = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(ownerId) ? null : ownerId;
    }
}
