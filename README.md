# Crypto Tax Helper

Crypto tax-reporting tool: upload a supported broker CSV, calculate FIFO cost basis, and download a ZIP containing PDF reports per cryptocurrency. The report is informational and is not tax advice; verify results against the source data and current Finnish Tax Administration guidance.

## Current status

- **Backend:** ASP.NET Core Minimal API targeting .NET 10.
- **Supported broker:** Coinmotion CSV (`coinmotion`). A Binance entry exists in the frontend configuration but is not implemented and must not be selected as a working broker.
- **Frontend:** React, TypeScript, and Vite, with Finnish and English UI translations.
- **Storage:** Generated ZIPs are held in process memory, expire after 60 minutes if not downloaded, and are removed after the first successful download. Staging uses a synthetic-data-only Compose stack; it is not production-ready.

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

The same backend test and frontend lint/build checks run in `.github/workflows/ci.yml` for pushes to `main`/`test` and pull requests targeting those branches. CI also builds the staging containers and runs a synthetic-data smoke test. Only successful pushes to `test` publish GHCR images; server deployment remains manual. See [`docs/STAGING.md`](docs/STAGING.md) for the SSH-tunnel-only runbook.

`npm ci` may report vulnerabilities in development dependencies. Review `npm audit` output before upgrading dependencies; do not apply automatic fixes without checking the resulting lockfile and build.

## Local development

Start the API and frontend in separate terminals. The Vite dev server proxies `/report` and `/healthz` to `http://localhost:8000`, so no `VITE_API_URL` is needed for the default setup:

```bash
# Terminal 1
cd backend
ASPNETCORE_URLS=http://localhost:8000 dotnet run --project src/CryptoTaxHelper.Api

# Terminal 2
cd frontend
npm run dev
```

Open the Vite URL shown in the terminal, normally <http://localhost:5173>. To use a different API address, create a local frontend `.env` (not committed) with `VITE_API_URL=http://localhost:<port>`.

Windows PowerShell equivalent for the API URL:

```powershell
$env:ASPNETCORE_URLS = "http://localhost:8000"
dotnet run --project src/CryptoTaxHelper.Api
```

## API

| Method | Path | Description |
| --- | --- | --- |
| `POST` | `/report/generate?year=2024` | Multipart upload with a `file` field. The file name must end in `.csv`. Returns `report_id` and pricing metrics. `year` is optional. |
| `GET` | `/report/download/{reportId}` | Returns `pdf_reports.zip` once. The in-memory report is removed after retrieval and expires after 60 minutes if never downloaded. |
| `GET` | `/healthz` | Minimal health check returning `{"status":"ok"}`. |

CSV uploads are limited to 10 MiB per file. The API has no authentication and no persistent report storage. Do not expose it publicly; the staging Compose stack binds only to host loopback and is intended to be reached through SSH forwarding with synthetic data.

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

deploy/staging/                          # Loopback-only Compose and Nginx config
tests/fixtures/                           # Synthetic CSVs only
tests/smoke/                              # Container staging smoke checks
```

## Adding a broker

1. Implement `IBrokerFileParser` under `backend/src/CryptoTaxHelper.Infrastructure/Brokers/<Broker>/`.
2. Register the parser in `DependencyInjection.cs`.
3. Add the broker only when the backend endpoint and parser are ready; update `frontend/src/config/brokerConfigs.ts`.
4. Add Finnish and English translations.
5. Add parser, calculation, and endpoint tests using synthetic CSV fixtures.

## Data and privacy

Uploaded CSVs contain financial information. Do not commit real exports, include them in logs or bug reports, or send them to external services. Staging is unauthenticated and must use synthetic data only. Generated reports remain in process memory until downloaded or expired after 60 minutes; production work still requires an approved authentication, privacy, isolation, and deployment design.

See [`AGENTIC_CODING_PLAN.md`](AGENTIC_CODING_PLAN.md) for the story-to-verified-code workflow and [`docs/STAGING.md`](docs/STAGING.md) for the manual staging runbook. Repository-specific agent guidance is in [`.github/copilot-instructions.md`](.github/copilot-instructions.md).
