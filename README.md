# Crypto Tax Helper

Crypto tax-reporting tool: upload a supported broker CSV, calculate FIFO cost basis, and download a ZIP containing PDF reports per cryptocurrency. The report is informational and is not tax advice; verify results against the source data and current Finnish Tax Administration guidance.

## Current status

- **Backend:** ASP.NET Core Minimal API targeting .NET 10.
- **Supported broker:** Coinmotion CSV (`coinmotion`). A Binance entry exists in the frontend configuration but is not implemented and must not be selected as a working broker.
- **Frontend:** React, TypeScript, and Vite, with Finnish and English UI translations.
- **Storage:** Generated ZIPs are held in process memory and removed after download. This is suitable for local development, not yet a production deployment design.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Node.js 20+ and npm

The backend targets `net10.0`; install a matching SDK before running backend commands. The frontend is independently buildable with Node/npm.

## Setup and validation

From the repository root:

```bash
cd backend
dotnet restore
dotnet build
dotnet test

cd ../frontend
npm ci
npm run lint
npm run build
```

`npm ci` may report vulnerabilities in development dependencies. Review `npm audit` output before upgrading dependencies; do not apply automatic fixes without checking the resulting lockfile and build.

## Local development

```powershell
# Terminal 1 - API, reachable from other computers
cd backend
$env:ASPNETCORE_URLS = "http://0.0.0.0:8000"
dotnet run --project src/CryptoTaxHelper.Api

# Terminal 2 - Frontend, reachable from other computers
Start the API and frontend in separate terminals. Set the API URL explicitly because the frontend defaults to port 8000:

```bash
# Terminal 1
cd backend
ASPNETCORE_URLS=http://localhost:8000 dotnet run --project src/CryptoTaxHelper.Api

# Terminal 2
cd frontend
npm run dev -- --host 0.0.0.0
```

Open `http://localhost:5173` on the server or `http://192.168.0.120:5173` from another computer.

For LAN testing, set `frontend/.env.local` (never commit it):

```text
VITE_API_URL=http://192.168.0.120:8000
VITE_OIDC_AUTHORITY=https://your-issuer.example.com
VITE_OIDC_CLIENT_ID=your-public-spa-client-id
VITE_OIDC_AUDIENCE=your-api-audience
VITE_OIDC_SCOPE=openid profile tax-helper.reports
```

The frontend keeps the PKCE verifier in memory/session storage. Because plain LAN HTTP is not a secure browser context, the development build includes a SHA-256 fallback for PKCE; use HTTPS for anything beyond isolated LAN testing.

### Authentication configuration

The API uses OIDC JWT bearer authentication. Set these deployment environment variables (ASP.NET's `__` separator maps to nested settings):

```text
Authentication__Authority=https://your-issuer.example.com
Authentication__Audience=your-api-audience
Authentication__RequiredScope=tax-helper.reports
Cors__AllowedOrigins__0=http://localhost:5173
Cors__AllowedOrigins__1=http://192.168.0.120:5173
```

The SPA uses Authorization Code with PKCE and keeps access tokens in memory. Its public OIDC settings are configured with `VITE_OIDC_AUTHORITY`, `VITE_OIDC_CLIENT_ID`, and optional `VITE_OIDC_SCOPE`; never put a client secret in `VITE_*` variables. The provider must allow the exact redirect URI `http://localhost:5173/` (or the deployed SPA origin). Both report endpoints require the configured scope; reports are owner-scoped and expire after 30 minutes.
Open the Vite URL shown in the terminal, normally <http://localhost:5173>. For another API address, create a local frontend `.env` (not committed) with `VITE_API_URL=http://localhost:<port>`.

Windows PowerShell equivalent for the API URL:

```powershell
$env:ASPNETCORE_URLS = "http://localhost:8000"
dotnet run --project src/CryptoTaxHelper.Api
```

## API

| Method | Path | Description |
| --- | --- | --- |
| `POST` | `/report/generate?year=2024` | Multipart upload with a `file` field. The file name must end in `.csv`. Returns `report_id` and pricing metrics. `year` is optional. |
| `GET` | `/report/download/{reportId}` | Returns `pdf_reports.zip` once. The in-memory report is removed after retrieval. |

The API currently permits the local frontend origin `http://localhost:5173` through CORS. It has no authentication, persistent storage, configured upload limit, or production deployment manifest; do not expose it publicly without addressing those concerns.

## Project structure

```text
backend/
├── CryptoTaxHelper.sln
├── src/
│   ├── CryptoTaxHelper.Api/             # Minimal API endpoints and host
│   ├── CryptoTaxHelper.Application/     # Report service, interfaces, models
│   ├── CryptoTaxHelper.Domain/          # FIFO and calculation rules
│   └── CryptoTaxHelper.Infrastructure/  # CSV parser, PDF generator, storage
└── tests/                               # Domain, application, integration tests

frontend/
└── src/
    ├── components/                      # React UI components
    ├── config/                          # Broker registry/configuration
    └── i18n.ts                          # Finnish and English translations
```

## Adding a broker

1. Implement `IBrokerFileParser` under `backend/src/CryptoTaxHelper.Infrastructure/Brokers/<Broker>/`.
2. Register the parser in `DependencyInjection.cs`.
3. Add the broker only when the backend endpoint and parser are ready; update `frontend/src/config/brokerConfigs.ts`.
4. Add Finnish and English translations.
5. Add parser, calculation, and endpoint tests using synthetic CSV fixtures.

## Data and privacy

Uploaded CSVs contain financial information. Do not commit real exports, include them in logs or bug reports, or send them to external services. Generated reports currently exist in server memory until downloaded; production work must define authentication, retention, isolation, HTTPS, rate/size limits, and secret handling before deployment.

See [`DEVELOPMENT_PLAN.md`](DEVELOPMENT_PLAN.md) for the inspection findings and prioritized roadmap. Agent-specific repository rules are in [`AGENTS.md`](AGENTS.md).
