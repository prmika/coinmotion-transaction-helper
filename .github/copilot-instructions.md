# Crypto Tax Helper

## Architecture

C# .NET 10 backend with React TypeScript frontend. Cryptocurrency tax reporting — upload broker CSV, get PDF report with FIFO cost basis.

### Data Flow

```
CSV Upload → IBrokerFileParser → ReportService.ProcessTransactions() → FIFO → PdfReportGenerator → ZIP
```

### Solution Structure (Clean Architecture)

- **CryptoTaxHelper.Domain** — FIFO queue, cost basis calculation, constants. Zero external dependencies.
- **CryptoTaxHelper.Application** — `ReportService`, interfaces (`IBrokerFileParser`, `IReportGenerator`, `IReportStore`), models.
- **CryptoTaxHelper.Infrastructure** — Broker parsers (Coinmotion), QuestPDF report generator, in-memory report store.
- **CryptoTaxHelper.Api** — ASP.NET Core Minimal API endpoints, CORS, DI wiring.

### Key Interfaces

- `IBrokerFileParser` — each broker implements this. `ParseAsync(Stream) → NormalizedTransaction[]`. Resolved by `BrokerId`.
- `IReportGenerator` — generates PDF zip from `TaxReport`.
- `IReportStore` — stores/retrieves generated report bytes.

## Development Workflows

### Running

```powershell
cd backend
dotnet run --project src/CryptoTaxHelper.Api   # API on http://localhost:8000
```

```powershell
cd frontend
npm run dev   # UI on http://localhost:5173
```

### Testing

```powershell
cd backend
dotnet test
```

Tests: `Domain.Tests` (FIFO unit), `Application.Tests` (service logic), `Integration.Tests` (HTTP pipeline).

### Debugging

F5 in VS Code with the `.NET Core Launch (web)` config. Requires C# Dev Kit extension.

## Project-Specific Conventions

### Transaction Structure

`NormalizedTransaction` always has `FromCurrency` and `ToCurrency`. EUR is the base currency:

- **Buy**: `FromCurrency == "EUR"` → adds to FIFO queue
- **Sell**: `ToCurrency == "EUR"` → consumes from FIFO queue
- **Transfer**: `account_transfer_in` → synthetic buy with 0 EUR amount

### FIFO Behavior

- `Constants.Epsilon` (1e-13) for floating-point comparisons
- Holding period ≥ 3650 days → 40% assumed cost rate, otherwise 20%
- Throws `InsufficientInventoryException` if inventory insufficient
- Returns `ConsumedLot` list showing which purchase lots were used
- Cost basis method: higher of actual FIFO cost vs assumed cost is used

### Adding a New Broker

1. Create `Infrastructure/Brokers/NewBroker/NewBrokerCsvParser.cs` implementing `IBrokerFileParser`
2. Register in `Infrastructure/DependencyInjection.cs`
3. Add broker to `frontend/src/config/brokerConfigs.ts`
4. Add translations to `frontend/src/i18n.ts` (both `en` and `fi`)

### UI and Translations Pattern

- **Mandatory Translations**: All UI text MUST go through `frontend/src/i18n.ts` (both `en` and `fi`). Never hardcode strings.

### Error Handling

- API validates `.csv` extension and non-empty file
- `FormatException` for CSV parse errors (with line number)
- Optional `year` query param filters report to specific year
- Reports auto-delete from memory after download

## Key Files

- [backend/src/CryptoTaxHelper.Api/Endpoints/ReportEndpoints.cs](backend/src/CryptoTaxHelper.Api/Endpoints/ReportEndpoints.cs): API endpoints
- [backend/src/CryptoTaxHelper.Application/Services/ReportService.cs](backend/src/CryptoTaxHelper.Application/Services/ReportService.cs): Core report logic
- [backend/src/CryptoTaxHelper.Domain/Fifo/FifoQueue.cs](backend/src/CryptoTaxHelper.Domain/Fifo/FifoQueue.cs): FIFO queue
- [backend/src/CryptoTaxHelper.Infrastructure/Brokers/Coinmotion/CoinmotionCsvParser.cs](backend/src/CryptoTaxHelper.Infrastructure/Brokers/Coinmotion/CoinmotionCsvParser.cs): Coinmotion CSV parser
- [backend/src/CryptoTaxHelper.Infrastructure/Reports/PdfReportGenerator.cs](backend/src/CryptoTaxHelper.Infrastructure/Reports/PdfReportGenerator.cs): PDF generation
- [frontend/src/config/brokerConfigs.ts](frontend/src/config/brokerConfigs.ts): Broker registry
- [frontend/src/i18n.ts](frontend/src/i18n.ts): Translations
