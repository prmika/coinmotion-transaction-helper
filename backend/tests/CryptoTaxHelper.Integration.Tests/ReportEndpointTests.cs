using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Xunit;

namespace CryptoTaxHelper.Integration.Tests;

public class ReportEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ReportEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        }));
        _client = _factory.CreateClient();
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
    public async Task Generate_MalformedCsv_DoesNotExposeParserExceptionDetails()
    {
        const string malformedCsv = """
            time,type,fromCurrency,toCurrency,eurAmount,cryptoAmount,rate,fee,feeCurrency
            2024-01-01T10:00:00+02:00,buy,EUR,BTC,not-a-number,1.0,10000,0,EUR
            """;
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(malformedCsv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "malformed.csv");

        var response = await _client.PostAsync("/report/generate", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var detail = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
            .RootElement.GetProperty("detail").GetString();
        detail.Should().Be("Unable to process the uploaded file.");
        detail.Should().NotContain("not-a-number");
        detail.Should().NotContain("Input string");
        detail.Should().NotContain("Invalid CSV data");
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

    [Fact]
    public async Task Generate_WithoutAuthentication_Returns401()
    {
        using var unauthenticated = new WebApplicationFactory<Program>().CreateClient();
        var response = await unauthenticated.PostAsync("/report/generate", new MultipartFormDataContent());
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Generate_WithInvalidBearerToken_Returns401()
    {
        using var unauthenticated = new WebApplicationFactory<Program>().CreateClient();
        unauthenticated.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "not-a-valid-token");

        var response = await unauthenticated.PostAsync("/report/generate", new MultipartFormDataContent());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Download_ReportCreatedByAnotherUser_Returns404()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(SampleCsv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "test.csv");

        var generateResponse = await _client.PostAsync("/report/generate", content);
        var reportId = JsonDocument.Parse(await generateResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("report_id").GetString();

        using var otherUser = _factory.CreateClient();
        otherUser.DefaultRequestHeaders.Add("X-Test-User", "another-user");
        var downloadResponse = await otherUser.GetAsync($"/report/download/{reportId}");

        downloadResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Download_SameReportConcurrently_AllowsOnlyOneRequest()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(SampleCsv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "test.csv");

        var generateResponse = await _client.PostAsync("/report/generate", content);
        var reportId = JsonDocument.Parse(await generateResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("report_id").GetString();

        var responses = await Task.WhenAll(
            _client.GetAsync($"/report/download/{reportId}"),
            _client.GetAsync($"/report/download/{reportId}"));

        responses.Count(response => response.StatusCode == HttpStatusCode.OK).Should().Be(1);
        responses.Count(response => response.StatusCode == HttpStatusCode.NotFound).Should().Be(1);
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
            UrlEncoder encoder) : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity("Test");
            identity.AddClaim(new Claim("sub", Request.Headers["X-Test-User"].FirstOrDefault() ?? "test-user"));
            identity.AddClaim(new Claim("scope", "tax-helper.reports"));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
