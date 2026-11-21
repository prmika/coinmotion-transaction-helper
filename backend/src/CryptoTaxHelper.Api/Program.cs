using CryptoTaxHelper.Api;
using CryptoTaxHelper.Application.Services;
using CryptoTaxHelper.Infrastructure;
using CryptoTaxHelper.Api.Endpoints;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// QuestPDF Community License
QuestPDF.Settings.License = LicenseType.Community;

// Register services
builder.Services.AddInfrastructure();
builder.Services.AddScoped<ReportService>();

// HTTP boundary limits (upload size, rate limits). See HttpLimitsOptions.
// HTTP boundary limits (upload size, rate limits). See HttpLimitsOptions.
builder.Services.Configure<HttpLimitsOptions>(builder.Configuration.GetSection("HttpLimits"));
var startupLimits = builder.Configuration.GetSection("HttpLimits").Get<HttpLimitsOptions>() ?? new HttpLimitsOptions();
// Body cap for the whole request: file + multipart overhead allowance (64 KiB). Kestrel enforces it
// while reading, so oversize bodies are rejected without buffering them. The multipart limit is set
// slightly higher so Kestrel's 413 fires first and is handled uniformly by the endpoint.
const long multipartOverheadBytes = 64 * 1024;
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = startupLimits.Upload.MaxBytes + multipartOverheadBytes);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
    options.MultipartBodyLengthLimit = startupLimits.Upload.MaxBytes + multipartOverheadBytes + 1024 * 1024);

// Per-client-IP fixed-window rate limits (built-in ASP.NET Core rate limiter).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            code = "rate_limited",
            detail = "Too many requests. Try again later."
        }, token);
    };
    options.AddPolicy(ReportEndpoints.GenerateRateLimitPolicy, context =>
    {
        var rule = context.RequestServices.GetRequiredService<IOptions<HttpLimitsOptions>>().Value.RateLimits.GenerateReport;
        return RateLimitPartition.GetFixedWindowLimiter(ClientKey(context), _ => ToWindowOptions(rule));
    });
    options.AddPolicy(ReportEndpoints.DownloadRateLimitPolicy, context =>
    {
        var rule = context.RequestServices.GetRequiredService<IOptions<HttpLimitsOptions>>().Value.RateLimits.DownloadReport;
        return RateLimitPartition.GetFixedWindowLimiter(ClientKey(context), _ => ToWindowOptions(rule));
    });
});

var requiredScope = builder.Configuration["Authentication:RequiredScope"] ?? "tax-helper.reports";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.Audience = builder.Configuration["Authentication:Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization(options => options.AddPolicy("Reports", policy =>
    policy.RequireAuthenticatedUser().RequireAssertion(context =>
    {
        var scopes = context.User.FindFirst("scope")?.Value?.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            ?? context.User.FindFirst("scp")?.Value?.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            ?? [];
        return scopes.Contains(requiredScope, StringComparer.Ordinal);
    })));

// CORS
builder.Services.AddCors(options =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:5173"];
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(origins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapReportEndpoints();

app.Run();

static string ClientKey(HttpContext context) =>
    context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

static FixedWindowRateLimiterOptions ToWindowOptions(RateLimitRule rule) => new()
{
    PermitLimit = rule.PermitLimit,
    Window = TimeSpan.FromSeconds(rule.WindowSeconds),
    QueueLimit = 0,
    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
    AutoReplenishment = true
};

// Required for integration tests
public partial class Program { }
