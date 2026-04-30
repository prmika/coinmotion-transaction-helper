using CryptoTaxHelper.Application.Interfaces;
using CryptoTaxHelper.Infrastructure.Brokers.Coinmotion;
using CryptoTaxHelper.Infrastructure.Reports;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoTaxHelper.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IBrokerFileParser, CoinmotionCsvParser>();
        services.AddSingleton<IReportGenerator, PdfReportGenerator>();
        services.AddSingleton<IReportStore, InMemoryReportStore>();
        return services;
    }
}
