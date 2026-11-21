using System.Text;

namespace CryptoTaxHelper.Api.Uploads;

/// <summary>
/// Content checks on uploaded report bytes, run before the broker parser sees them.
/// The file name and the client-declared content type are not trusted; only the bytes are checked.
/// </summary>
internal static class CsvUploadValidator
{
    public const string InvalidContentCode = "invalid_file_content";
    public const string InvalidHeaderCode = "invalid_csv_header";
    public const string InvalidContentMessage = "The uploaded file must be a UTF-8 text CSV export.";
    public const string InvalidHeaderMessage = "The CSV header row does not match the Coinmotion export format.";

    // Columns the Coinmotion parser reads by name. A file missing any of them cannot be parsed.
    private static readonly string[] RequiredColumns =
    [
        "time", "type", "fromCurrency", "toCurrency", "eurAmount",
        "cryptoAmount", "rate", "fee", "feeCurrency"
    ];

    public enum Result
    {
        Valid,
        InvalidContent,
        InvalidHeader
    }

    public static Result Validate(byte[] bytes)
    {
        var body = StripUtf8Bom(bytes);

        // NUL bytes never appear in a text CSV export. Binary files that were renamed to .csv usually contain them.
        if (Array.IndexOf(body, (byte)0) >= 0)
            return Result.InvalidContent;

        string text;
        try
        {
            text = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(body);
        }
        catch (DecoderFallbackException)
        {
            return Result.InvalidContent;
        }

        // Same header rule as the parser: first non-empty line, trimmed, quotes removed, compared case-insensitively.
        var headerLine = text.Split('\n').FirstOrDefault(line => line.Trim().Length > 0) ?? string.Empty;
        var columns = headerLine
            .Split(',')
            .Select(column => column.Trim().Trim('"').Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return RequiredColumns.All(columns.Contains) ? Result.Valid : Result.InvalidHeader;
    }

    private static byte[] StripUtf8Bom(byte[] bytes) =>
        bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF
            ? bytes[3..]
            : bytes;
}
