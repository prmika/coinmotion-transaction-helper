using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CryptoTaxHelper.Integration.Tests;

/// <summary>
/// Upload size limit and per-IP rate limits on POST /report/generate and GET /report/download/{id}.
/// Each factory is a separate host, so rate-limiter state is isolated per test class.
/// </summary>
public class UploadAndRateLimitTests
{
    private const string SampleCsv = """
        time,type,fromCurrency,toCurrency,eurAmount,cryptoAmount,rate,fee,feeCurrency
        2024-01-01T10:00:00+02:00,buy,EUR,BTC,10000,1.0,10000,0,EUR
        2024-02-01T10:00:00+02:00,sell,BTC,EUR,15000,1.0,15000,0,EUR
        """;

    private static WebApplicationFactory<Program> CreateFactory(Action<IDictionary<string, string?>> configure)
    {
        var settings = new Dictionary<string, string?>();
        configure(settings);
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // App configuration (not host settings) so the values override appsettings.json.
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(settings));
        });
    }

    private static MultipartFormDataContent CsvContent(string csv, string fileName = "test.csv")
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(file, "file", fileName);
        return content;
    }

    [Fact]
    public async Task Upload_DeclaredOversizeBody_Returns413WithStableCodeAndNoInternals()
    {
        using var factory = CreateFactory(s => s["HttpLimits:Upload:MaxBytes"] = "4096");
        using var client = factory.CreateClient();
        var padded = SampleCsv + new string('x', 200_000); // well above 4 KiB + overhead

        using var response = await client.PostAsync("/report/generate", CsvContent(padded));

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("code").GetString().Should().Be("upload_too_large");
        doc.GetProperty("detail").GetString().Should().Contain("limit");
        body.Should().NotContain("Exception");
        body.Should().NotContain("   at ");
        body.Should().NotContain("CryptoTaxHelper");
    }

    [Fact]
    public async Task Upload_FileJustOverLimit_Returns413()
    {
        using var factory = CreateFactory(s => s["HttpLimits:Upload:MaxBytes"] = "4096");
        using var client = factory.CreateClient();
        // Slightly over 4 KiB file: inside the multipart overhead allowance, so only the file-size check rejects it.
        var csv = SampleCsv + new string('\n', 4096 - SampleCsv.Length + 500);

        using var response = await client.PostAsync("/report/generate", CsvContent(csv));

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        JsonDocument.Parse(await response.Content.ReadAsStringAsync())
            .RootElement.GetProperty("code").GetString().Should().Be("upload_too_large");
    }

    [Fact]
    public async Task Upload_ChunkedOversizeBody_IsRejectedWith413()
    {
        using var factory = CreateFactory(s => s["HttpLimits:Upload:MaxBytes"] = "4096");
        using var client = factory.CreateClient();
        // Content-Length is not sent (chunked), so the limit must be enforced while reading.
        using var content = CsvContent(SampleCsv + new string('x', 200_000));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/report/generate")
        {
            Content = new ChunkedContent(await content.ReadAsByteArrayAsync(), content.Headers.ContentType!.ToString())
        };

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        JsonDocument.Parse(await response.Content.ReadAsStringAsync())
            .RootElement.GetProperty("code").GetString().Should().Be("upload_too_large");
    }

    [Fact]
    public async Task Upload_WithinLimit_StillSucceeds()
    {
        using var factory = CreateFactory(s => s["HttpLimits:Upload:MaxBytes"] = "4096");
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/report/generate", CsvContent(SampleCsv));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument.Parse(await response.Content.ReadAsStringAsync())
            .RootElement.GetProperty("report_id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Generate_ExceedingRateLimit_Returns429WithStableCode()
    {
        using var factory = CreateFactory(s =>
        {
            s["HttpLimits:RateLimits:GenerateReport:PermitLimit"] = "2";
            s["HttpLimits:RateLimits:GenerateReport:WindowSeconds"] = "60";
        });
        using var client = factory.CreateClient();

        for (var i = 0; i < 2; i++)
        {
            using var ok = await client.PostAsync("/report/generate", CsvContent(SampleCsv));
            ok.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var limited = await client.PostAsync("/report/generate", CsvContent(SampleCsv));

        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limited.Headers.Contains("Retry-After").Should().BeTrue();
        var doc = JsonDocument.Parse(await limited.Content.ReadAsStringAsync()).RootElement;
        doc.GetProperty("code").GetString().Should().Be("rate_limited");
    }

    [Fact]
    public async Task Download_ExceedingRateLimit_Returns429WithStableCode()
    {
        using var factory = CreateFactory(s =>
        {
            s["HttpLimits:RateLimits:DownloadReport:PermitLimit"] = "2";
            s["HttpLimits:RateLimits:DownloadReport:WindowSeconds"] = "60";
        });
        using var client = factory.CreateClient();
        using var gen = await client.PostAsync("/report/generate", CsvContent(SampleCsv));
        var reportId = JsonDocument.Parse(await gen.Content.ReadAsStringAsync())
            .RootElement.GetProperty("report_id").GetString();

        for (var i = 0; i < 2; i++)
        {
            using var attempt = await client.GetAsync($"/report/download/{reportId}");
            attempt.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        using var limited = await client.GetAsync($"/report/download/{reportId}");

        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        JsonDocument.Parse(await limited.Content.ReadAsStringAsync())
            .RootElement.GetProperty("code").GetString().Should().Be("rate_limited");
    }

    /// <summary>Sends the body with no Content-Length (Transfer-Encoding: chunked).</summary>
    private sealed class ChunkedContent : HttpContent
    {
        private readonly byte[] _bytes;

        public ChunkedContent(byte[] bytes, string contentType)
        {
            _bytes = bytes;
            Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            // Write in pieces so the server sees a stream without a known length.
            const int chunk = 8192;
            for (var offset = 0; offset < _bytes.Length; offset += chunk)
                await stream.WriteAsync(_bytes.AsMemory(offset, Math.Min(chunk, _bytes.Length - offset)));
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
