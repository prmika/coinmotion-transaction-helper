namespace CryptoTaxHelper.Domain;

public static class Constants
{
    public const decimal Epsilon = 0.0000000000001m;
    public const int LongHoldingPeriodDays = 3650;
    public const decimal ShortHoldingAssumedRate = 0.2m;
    public const decimal LongHoldingAssumedRate = 0.4m;
}
