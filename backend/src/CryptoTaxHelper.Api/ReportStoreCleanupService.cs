using CryptoTaxHelper.Application.Interfaces;

namespace CryptoTaxHelper.Api;

public sealed class ReportStoreCleanupService(IExpiringReportStore reportStore) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        do
        {
            reportStore.RemoveExpired();
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
