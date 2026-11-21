# Adding a new broker

To implement a new broker (e.g. Kraken), follow this checklist:

1. Create `Infrastructure/Brokers/<Broker>/<Broker>CsvParser.cs` implementing `IBrokerFileParser`. `ParseAsync(Stream)` must return `NormalizedTransaction[]`.
2. Register the parser in `Infrastructure/DependencyInjection.cs`.
3. Add the broker entry in `frontend/src/config/brokerConfigs.ts`.
4. Add translations in `frontend/src/i18n.ts` (both `en` and `fi`).

## Parser contract

- Implement `IBrokerFileParser`.
- `ParseAsync(Stream) → NormalizedTransaction[]`.
- The parser is resolved by `BrokerId`.

## Configuration

`frontend/src/config/brokerConfigs.ts` declares each broker with:

- `id`, `name`
- `apiEndpoint` (path used by the frontend)
- `fileType`
- `hasYearSelection`
- `hasTutorialVideo` / `videoAssetPath` (optional)
- `metricsEnabled`

Currently supported: `coinmotion` (implemented), `binance` (placeholder).
