using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CryptoTaxHelper.Integration.Tests;

/// <summary>
/// Content checks on POST /report/generate that run before the CSV parser: NUL bytes, UTF-8 validity
/// (after an optional BOM), and the Coinmotion header row. The .csv extension is not sufficient on its own.
/// </summary>
public class UploadContentValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Header = "time,type,fromCurrency,toCurrency,eurAmount,cryptoAmount,rate,fee,feeCurrency";
    private const string Rows =
        "2024-01-01T10:00:00+02:00,buy,EUR,BTC,10000,1.0,10000,0,EUR\n" +
        "2024-02-01T10:00:00+02:00,sell,BTC,EUR,15000,1.0,15000,0,EUR\n";
    private static readonly string SampleCsv = Header + "\n" + Rows;

    private readonly HttpClient _client;

    public UploadContentValidationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RenamedBinaryFile_Returns400WithStableCodeAndNoInternals()
    {
        // PNG signature and IHDR chunk, renamed to .csv.
        var bytes = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0xFF, 0xFE, 0x80, 0xC3, 0x28
        };

        var (status, doc, raw) = await Post(bytes);

        status.Should().Be(HttpStatusCode.BadRequest);
        doc.GetProperty("code").GetString().Should().Be("invalid_file_content");
        AssertNoParserDetails(raw);
    }

    [Fact]
    public async Task NulByteFile_Returns400WithStableCodeAndNoInternals()
    {
        var bytes = Encoding.UTF8.GetBytes(SampleCsv);
        var withNul = bytes.Take(60).Concat(new byte[] { 0x00 }).Concat(bytes.Skip(60)).ToArray();

        var (status, doc, raw) = await Post(withNul);

        status.Should().Be(HttpStatusCode.BadRequest);
        doc.GetProperty("code").GetString().Should().Be("invalid_file_content");
        AssertNoParserDetails(raw);
    }

    [Fact]
    public async Task NonUtf8File_Returns400WithStableCodeAndNoInternals()
    {
        // 'ä' encoded as Latin-1 (0xE4) is not valid UTF-8 on its own.
        var bytes = Encoding.Latin1.GetBytes(SampleCsv.Replace("1.0,10000,0,EUR", "1.0,10000,0,EUR Mätä"));
        bytes.Should().Contain((byte)0xE4);

        var (status, doc, raw) = await Post(bytes);

        status.Should().Be(HttpStatusCode.BadRequest);
        doc.GetProperty("code").GetString().Should().Be("invalid_file_content");
        AssertNoParserDetails(raw);
    }

    [Fact]
    public async Task ValidUtf8WithBom_IsAccepted()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes(SampleCsv)).ToArray();

        var (status, doc, _) = await Post(bytes);

        status.Should().Be(HttpStatusCode.OK);
        doc.GetProperty("report_id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task WrongHeaderRow_Returns400WithStableCodeAndNoInternals()
    {
        var csv = "date,amount,coin\n" + Rows;

        var (status, doc, raw) = await Post(Encoding.UTF8.GetBytes(csv));

        status.Should().Be(HttpStatusCode.BadRequest);
        doc.GetProperty("code").GetString().Should().Be("invalid_csv_header");
        AssertNoParserDetails(raw);
    }

    private async Task<(HttpStatusCode Status, JsonElement Body, string Raw)> Post(byte[] bytes, string fileName = "test.csv")
    {
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(file, "file", fileName);

        using var response = await _client.PostAsync("/report/generate", content);
        var raw = await response.Content.ReadAsStringAsync();
        var body = JsonDocument.Parse(raw).RootElement.Clone();
        return (response.StatusCode, body, raw);
    }

    private static void AssertNoParserDetails(string raw)
    {
        raw.Should().NotContain("Exception");
        raw.Should().NotContain("Missing required column");
        raw.Should().NotContain("Invalid CSV data");
        raw.Should().NotContain("   at ");
        raw.Should().NotContain("CryptoTaxHelper");
    }
}
