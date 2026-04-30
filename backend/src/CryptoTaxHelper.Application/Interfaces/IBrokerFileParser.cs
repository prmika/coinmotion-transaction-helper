using CryptoTaxHelper.Application.Models;

namespace CryptoTaxHelper.Application.Interfaces;

public interface IBrokerFileParser
{
    string BrokerId { get; }
    string[] SupportedFileExtensions { get; }
    Task<IReadOnlyList<NormalizedTransaction>> ParseAsync(Stream fileStream, CancellationToken ct = default);
}
