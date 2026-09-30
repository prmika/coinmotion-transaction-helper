namespace CryptoTaxHelper.Api;

public static class UploadLimits
{
    public const long MaxCsvBytes = 10 * 1024 * 1024;
    public const long MultipartOverheadBytes = 64 * 1024;
    public const long MaxRequestBodyBytes = MaxCsvBytes + MultipartOverheadBytes;
}
