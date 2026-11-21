using System.Globalization;
using CryptoTaxHelper.Infrastructure.Reports;
using FluentAssertions;
using Xunit;

namespace CryptoTaxHelper.Application.Tests;

// Regression tests for issue #14: the PDF showed raw floating-point values such as
// "3,646999999999987E-05" and "-4,336808689942018E-19". Inputs are strings, so the
// exact pre-migration value (and the residue that double produced) is preserved.
public class PdfNumberFormatterTests
{
    private static decimal Dec(string s) =>
        decimal.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);

    [Theory]
    [InlineData("0.00003646999999999987", "0.00003647")]
    [InlineData("0.00017795000000000012", "0.00017795")]
    [InlineData("0.0031517199999999998", "0.00315172")]
    [InlineData("0.00158114", "0.00158114")]
    public void Crypto_RoundsToEightDecimals_WithoutScientificNotation(string value, string expected)
    {
        var text = PdfNumberFormatter.Crypto(Dec(value));

        text.Should().NotContainAny("E", "e");
        Parse(text).Should().Be(Dec(expected));
        DecimalsIn(text).Should().BeLessThanOrEqualTo(8);
    }

    [Theory]
    [InlineData("-0.0000000000000000004336808689942018")]
    [InlineData("0.0000000000000000008673617379884035")]
    [InlineData("-0.0000000000000000002168404344971009")]
    [InlineData("0")]
    public void Crypto_FloatingPointResidue_IsShownAsZeroNotNegativeZero(string residue)
    {
        var text = PdfNumberFormatter.Crypto(Dec(residue));

        Parse(text).Should().Be(0m);
        text.Should().NotStartWith("-");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0")]
    [InlineData("-0.001")]
    public void Money_ZeroLike_IsNeverNegativeZero(string value)
    {
        var text = PdfNumberFormatter.Money(Dec(value));

        Parse(text).Should().Be(0m);
        text.Should().NotStartWith("-");
    }

    [Theory]
    [InlineData("-87.505", "-87.51")]
    [InlineData("73.634", "73.63")]
    [InlineData("-21.03", "-21.03")]
    public void Money_RoundsToTwoDecimals_MidpointAwayFromZero(string value, string expected)
    {
        Parse(PdfNumberFormatter.Money(Dec(value))).Should().Be(Dec(expected));
    }

    private static decimal Parse(string s) =>
        decimal.Parse(s, NumberStyles.Float, CultureInfo.CurrentCulture);

    private static int DecimalsIn(string text)
    {
        var sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        var idx = text.IndexOf(sep, StringComparison.Ordinal);
        return idx < 0 ? 0 : text.Length - idx - sep.Length;
    }
}
