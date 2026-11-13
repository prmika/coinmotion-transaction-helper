using System.Globalization;
using CryptoTaxHelper.Infrastructure.Reports;
using FluentAssertions;
using Xunit;

namespace CryptoTaxHelper.Application.Tests;

// Regression tests for issue #14: PDF showed raw floating-point values such as
// "3,646999999999987E-05" and "-4,336808689942018E-19".
public class PdfNumberFormatterTests
{
    private static double Parse(string s) =>
        double.Parse(s, NumberStyles.Float, CultureInfo.CurrentCulture);

    [Theory]
    [InlineData(3.646999999999987E-05, 0.00003647)]
    [InlineData(0.00017795000000000012, 0.00017795)]
    [InlineData(0.0031517199999999998, 0.00315172)]
    [InlineData(0.00158114, 0.00158114)]
    public void Crypto_RoundsToEightDecimals_WithoutScientificNotation(double value, double expected)
    {
        var text = PdfNumberFormatter.Crypto(value);

        text.Should().NotContainAny("E", "e");
        Parse(text).Should().BeApproximately(expected, 1e-12);
        DecimalsIn(text).Should().BeLessThanOrEqualTo(8);
    }

    [Theory]
    [InlineData(-4.336808689942018E-19)]
    [InlineData(8.673617379884035E-19)]
    [InlineData(-2.168404344971009E-19)]
    [InlineData(-0.0)]
    public void Crypto_FloatingPointResidue_IsShownAsZeroNotNegativeZero(double residue)
    {
        var text = PdfNumberFormatter.Crypto(residue);

        Parse(text).Should().Be(0);
        text.Should().NotStartWith("-");
    }

    [Theory]
    [InlineData(-0.0)]
    [InlineData(0.0)]
    [InlineData(-0.001)]
    public void Money_ZeroLike_IsNeverNegativeZero(double value)
    {
        var text = PdfNumberFormatter.Money(value);

        Parse(text).Should().Be(0);
        text.Should().NotStartWith("-");
    }

    [Theory]
    [InlineData(-87.505, -87.51)]
    [InlineData(73.634, 73.63)]
    [InlineData(-21.03, -21.03)]
    public void Money_RoundsToTwoDecimals_MidpointAwayFromZero(double value, double expected)
    {
        Parse(PdfNumberFormatter.Money(value)).Should().BeApproximately(expected, 1e-9);
    }

    private static int DecimalsIn(string text)
    {
        var sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        var idx = text.IndexOf(sep, StringComparison.Ordinal);
        return idx < 0 ? 0 : text.Length - idx - sep.Length;
    }
}
