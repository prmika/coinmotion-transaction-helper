namespace CryptoTaxHelper.Infrastructure.Reports;

/// <summary>
/// Display-only number formatting for the PDF report. Calculation values are never changed here.
/// Crypto quantities are shown with at most 8 decimals, never in scientific notation, and
/// floating-point residue (e.g. -4.3E-19 left after a full FIFO sell) is shown as 0, not -0.
/// </summary>
public static class PdfNumberFormatter
{
    private const int CryptoDecimals = 8;

    public static string Crypto(decimal value)
    {
        var rounded = Math.Round(value, CryptoDecimals, MidpointRounding.AwayFromZero);
        if (rounded == 0) rounded = 0; // normalizes -0.0 and sub-precision residue to +0
        return rounded.ToString($"F{CryptoDecimals}");
    }

    public static string Money(decimal value)
    {
        var rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);
        if (rounded == 0) rounded = 0;
        return rounded.ToString("F2");
    }
}
