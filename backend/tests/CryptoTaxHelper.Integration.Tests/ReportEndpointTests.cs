using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CryptoTaxHelper.Integration.Tests;

public class ReportEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ReportEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private const string SampleCsv = """
        time,type,fromCurrency,toCurrency,eurAmount,cryptoAmount,rate,fee,feeCurrency
        2024-01-01T10:00:00+02:00,buy,EUR,BTC,10000,1.0,10000,0,EUR
        2024-02-01T10:00:00+02:00,sell,BTC,EUR,15000,1.0,15000,0,EUR
        """;

    [Fact]
    public async Task Generate_ValidCsv_ReturnsReportIdAndMetrics()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(SampleCsv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "test.csv");

        var response = await _client.PostAsync("/report/generate", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("report_id").GetString().Should().NotBeNullOrEmpty();
        doc.RootElement.GetProperty("pricing_metrics").GetProperty("total_sales_transactions").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Generate_NonCsvFile_Returns400()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("not a csv"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "test.txt");

        var response = await _client.PostAsync("/report/generate", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Download_ValidReportId_ReturnsZip()
    {
        // First generate
        var genContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(SampleCsv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        genContent.Add(fileContent, "file", "test.csv");

        var genResponse = await _client.PostAsync("/report/generate", genContent);
        var genJson = await genResponse.Content.ReadAsStringAsync();
        var reportId = JsonDocument.Parse(genJson).RootElement.GetProperty("report_id").GetString();

        // Then download
        var dlResponse = await _client.GetAsync($"/report/download/{reportId}");

        dlResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        dlResponse.Content.Headers.ContentType!.MediaType.Should().Be("application/zip");
    }

    [Fact]
    public async Task Download_InvalidReportId_Returns404()
    {
        var response = await _client.GetAsync("/report/download/nonexistent-id");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Generate_WithYearFilter_FiltersCorrectly()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(SampleCsv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "test.csv");

        var response = await _client.PostAsync("/report/generate?year=2024", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Generate_WithNonMatchingYear_Returns400()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(SampleCsv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "test.csv");

        var response = await _client.PostAsync("/report/generate?year=2020", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
