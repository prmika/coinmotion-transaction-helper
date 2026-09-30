namespace CryptoTaxHelper.Application.Interfaces;

public interface IExpiringReportStore
{
    int RemoveExpired();
}
