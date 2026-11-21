# Crypto Tax Helper — Overview

**Crypto Tax Helper** is a cryptocurrency tax-reporting tool. Upload a broker
CSV export and get a PDF report with FIFO cost-basis calculations.

## Stack

- **Backend** — ASP.NET Core Minimal API, `.NET 10`, Clean Architecture (Domain / Application / Infrastructure / Api).
- **Frontend** — React + TypeScript + Vite.
- **Language** — Finnish and English (`frontend/src/i18n.ts`).
- **Report** — QuestPDF, packaged as a ZIP download.

## Data flow

```
CSV upload → IBrokerFileParser → ReportService.ProcessTransactions()
  → FIFO cost basis → PdfReportGenerator → ZIP download
```

## Scope

- **Implemented broker:** Coinmotion CSV.
- **Binance** is configured as a placeholder only and must not be presented as implemented.
- Generated reports are stored **in memory** and removed after download.

## Links

- [Getting started](getting-started.md) — prerequisites and run/test commands
- [Architecture](architecture.md) — layers, interfaces, key files
- [API reference](api-reference.md) — endpoints and request/response shape
- [Cost calculation](cost-calculation.md) — the FIFO / deemed-cost contract
- [Development](development.md) — branching, tests, validation
- [Adding a broker](adding-a-broker.md) — the new-broker checklist
- [Project status](project-status.md) — current state and roadmap
