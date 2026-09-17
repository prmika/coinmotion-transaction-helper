using CryptoTaxHelper.Application.Services;
using CryptoTaxHelper.Infrastructure;
using CryptoTaxHelper.Api.Endpoints;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// QuestPDF Community License
QuestPDF.Settings.License = LicenseType.Community;

// Register services
builder.Services.AddInfrastructure();
builder.Services.AddScoped<ReportService>();

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
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapReportEndpoints();

app.Run();

// Required for integration tests
public partial class Program { }
