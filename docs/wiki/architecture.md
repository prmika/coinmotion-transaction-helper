# Architecture

Clean Architecture with four backend layers and a React frontend.

## Layers

| Layer | Project | Responsibility |
|---|---|---|
| Domain | `CryptoTaxHelper.Domain` | FIFO queue, cost-basis calculation, constants. **Zero external dependencies.** |
| Application | `CryptoTaxHelper.Application` | `ReportService`, interfaces (`IBrokerFileParser`, `IReportGenerator`, `IReportStore`), models. |
| Infrastructure | `CryptoTaxHelper.Infrastructure` | Broker parsers (Coinmotion), PDF generation, storage. |
| Api | `CryptoTaxHelper.Api` | ASP.NET Core Minimal API endpoints, CORS, DI wiring. |

```
Api  ──►  Application  ──►  Infrastructure  ──►  Domain
```

## Key interfaces

- **`IBrokerFileParser`** — each broker implements this. `ParseAsync(Stream) → NormalizedTransaction[]`, resolved by `BrokerId`.
- **`IReportGenerator`** — generates the PDF ZIP from a `TaxReport`.
- **`IReportStore`** — stores/retrieves generated report bytes.

## Data flow

```
CSV upload → IBrokerFileParser → ReportService.ProcessTransactions() → FIFO → PdfReportGenerator → ZIP
```

## CI

` .github/workflows/ci.yml ` runs two jobs on PRs to `main` and pushes to `main`:

- **backend** — `.NET 10` restore → build (Release) → test.
- **frontend** — Node 20 `npm ci` → lint → build.

## Key files

- [`backend/.../Endpoints/ReportEndpoints.cs`](backend/src/CryptoTaxHelper.Api/Endpoints/ReportEndpoints.cs) — API endpoints
- [`backend/.../Services/ReportService.cs`](backend/src/CryptoTaxHelper.Application/Services/ReportService.cs) — core report logic
- [`backend/.../Fifo/FifoQueue.cs`](backend/src/CryptoTaxHelper.Domain/Fifo/FifoQueue.cs) — FIFO queue
- [`backend/.../Brokers/Coinmotion/CoinmotionCsvParser.cs`](backend/src/CryptoTaxHelper.Infrastructure/Brokers/Coinmotion/CoinmotionCsvParser.cs) — Coinmotion parser
- [`backend/.../Reports/PdfReportGenerator.cs`](backend/src/CryptoTaxHelper.Infrastructure/Reports/PdfReportGenerator.cs) — PDF generation
- [`frontend/src/config/brokerConfigs.ts`](frontend/src/config/brokerConfigs.ts) — broker registry
- [`frontend/src/i18n.ts`](frontend/src/i18n.ts) — translations (fi/en)
