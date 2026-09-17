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

### Authentication configuration

The API uses OIDC JWT bearer authentication. Set these deployment environment variables (ASP.NET's `__` separator maps to nested settings):

```text
Authentication__Authority=https://your-issuer.example.com
Authentication__Audience=your-api-audience
Authentication__RequiredScope=tax-helper.reports
Cors__AllowedOrigins__0=http://localhost:5173
```

The SPA uses Authorization Code with PKCE and keeps access tokens in memory. Its public OIDC settings are configured with `VITE_OIDC_AUTHORITY`, `VITE_OIDC_CLIENT_ID`, and optional `VITE_OIDC_SCOPE`; never put a client secret in `VITE_*` variables. The provider must allow the exact redirect URI `http://localhost:5173/` (or the deployed SPA origin). Both report endpoints require the configured scope; reports are owner-scoped and expire after 30 minutes.

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
