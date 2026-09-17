# Crypto Tax Helper

Cryptocurrency tax reporting tool. Upload a broker CSV → get a PDF report with FIFO cost basis calculations.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/)

## Setup

### Backend

```powershell
cd backend
dotnet restore
dotnet build
```

### Frontend

```powershell
cd frontend
npm install
```

## Running

Start both in separate terminals:

```powershell
# Terminal 1 - API (http://localhost:8000)
cd backend
dotnet run --project src/CryptoTaxHelper.Api

# Terminal 2 - Frontend (http://localhost:5173)
cd frontend
npm run dev
```

Open http://localhost:5173.

## Tests

```powershell
cd backend
dotnet test
```

## API Endpoints

| Method | Path                          | Description                                                                  |
| ------ | ----------------------------- | ---------------------------------------------------------------------------- |
| POST   | `/report/generate`            | Upload CSV (multipart), optional `?year=2024`. Returns `report_id` + metrics |
| GET    | `/report/download/{reportId}` | Download `pdf_reports.zip`                                                   |

### Coinmotion quantity reconciliation

The Coinmotion import preserves the CSV quantity text and source row number. Before FIFO processing, each asset is reconciled using a technical numeric tolerance of `1e-13` crypto units only. A positive deficit above that tolerance returns HTTP 422 with exact available, requested, difference, source row, and timestamp details; no balancing or zero-cost transaction is synthesized, and no report is stored. This tolerance is an implementation policy, not a tax or Vero threshold.

Before uploading, provide the complete export, including purchases, sales, transfers, and fees, and relevant activity from other wallets or exchanges. A reconciliation failure must be investigated before filing; the application does not determine whether a difference is tax-relevant.

## Project Structure

```
backend/
├── src/
│   ├── CryptoTaxHelper.Api/             # Minimal API endpoints
│   ├── CryptoTaxHelper.Application/     # Business logic, interfaces
│   ├── CryptoTaxHelper.Domain/          # FIFO queue, cost basis (no deps)
│   └── CryptoTaxHelper.Infrastructure/  # Broker parsers, PDF gen, storage
└── tests/

frontend/
├── src/
│   ├── components/     # React components
│   ├── config/         # Broker configurations
│   └── i18n.ts         # Translations (fi/en)
```

## Adding a New Broker

1. Create `Infrastructure/Brokers/YourBroker/YourBrokerCsvParser.cs` implementing `IBrokerFileParser`
2. Register in `Infrastructure/DependencyInjection.cs`
3. Add broker entry in `frontend/src/config/brokerConfigs.ts`
4. Add translations in `frontend/src/i18n.ts`
