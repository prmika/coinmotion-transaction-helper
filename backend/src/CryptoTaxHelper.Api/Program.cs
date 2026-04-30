using CryptoTaxHelper.Application.Services;
using CryptoTaxHelper.Infrastructure;
using CryptoTaxHelper.Api.Endpoints;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// QuestPDF Community License
QuestPDF.Settings.License = LicenseType.Community;

// Register services
builder.Services.AddInfrastructure();
builder.Services.AddScoped<ReportService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors();

// Map endpoints
app.MapReportEndpoints();

app.Run();

// Required for integration tests
public partial class Program { }
