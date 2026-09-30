using CryptoTaxHelper.Api;
using CryptoTaxHelper.Application.Interfaces;
using CryptoTaxHelper.Application.Services;
using CryptoTaxHelper.Infrastructure;
using CryptoTaxHelper.Infrastructure.Reports;
using CryptoTaxHelper.Api.Endpoints;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = UploadLimits.MaxRequestBodyBytes);
builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit = UploadLimits.MaxRequestBodyBytes);

// QuestPDF Community License
QuestPDF.Settings.License = LicenseType.Community;

// Register services
builder.Services.AddInfrastructure();
builder.Services.AddScoped<ReportService>();

var reportStoreLifetimeMinutes = int.TryParse(builder.Configuration["ReportStore:LifetimeMinutes"], out var configuredLifetime)
    && configuredLifetime > 0
    ? configuredLifetime
    : 60;
builder.Services.RemoveAll<IReportStore>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton(sp => new InMemoryReportStore(
    TimeSpan.FromMinutes(reportStoreLifetimeMinutes),
    sp.GetRequiredService<TimeProvider>()));
builder.Services.AddSingleton<IReportStore>(sp => sp.GetRequiredService<InMemoryReportStore>());
builder.Services.AddSingleton<IExpiringReportStore>(sp => sp.GetRequiredService<InMemoryReportStore>());
builder.Services.AddHostedService<ReportStoreCleanupService>();

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

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

// Map endpoints
app.MapReportEndpoints();

app.Run();

// Required for integration tests
public partial class Program { }
