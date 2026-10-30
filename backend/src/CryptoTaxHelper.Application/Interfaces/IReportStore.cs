namespace CryptoTaxHelper.Application.Interfaces;

public interface IReportStore
{
    string Store(byte[] reportData, string ownerId);
    byte[]? Consume(string reportId, string ownerId);
}
