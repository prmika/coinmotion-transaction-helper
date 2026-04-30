# Backend - CryptoTaxHelper

## Build & Run

```powershell
cd backend
dotnet restore
dotnet build
dotnet run --project src/CryptoTaxHelper.Api
```

API starts on http://localhost:8000

## Run Tests

```powershell
dotnet test
```

## Project Structure

```
src/
├── CryptoTaxHelper.Api/              → Minimal API endpoints
├── CryptoTaxHelper.Application/      → Business logic orchestration, interfaces
├── CryptoTaxHelper.Domain/           → Core FIFO/tax calculation (no dependencies)
└── CryptoTaxHelper.Infrastructure/   → Broker parsers, PDF, storage implementations

tests/
├── CryptoTaxHelper.Domain.Tests/     → FIFO unit tests
├── CryptoTaxHelper.Application.Tests/ → Service tests
└── CryptoTaxHelper.Integration.Tests/ → Full HTTP pipeline tests
```

## Adding a New Broker

1. Create `Infrastructure/Brokers/YourBroker/YourBrokerCsvParser.cs` implementing `IBrokerFileParser`
2. Register in `Infrastructure/DependencyInjection.cs`
3. Add broker to frontend `brokerConfigs.ts` + `i18n.ts`
