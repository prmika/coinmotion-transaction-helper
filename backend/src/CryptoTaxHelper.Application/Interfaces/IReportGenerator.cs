using CryptoTaxHelper.Application.Models;

namespace CryptoTaxHelper.Application.Interfaces;

public interface IReportGenerator
{
    Task<byte[]> GeneratePdfZipAsync(TaxReport report, CancellationToken ct = default);
}
