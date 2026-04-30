namespace CryptoTaxHelper.Application.Interfaces;

public interface IReportStore
{
    string Store(byte[] reportData);
    byte[]? Retrieve(string reportId);
    bool Remove(string reportId);
}
