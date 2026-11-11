# Agent instructions for Crypto Tax Helper

## Before making changes

1. Read `README.md` for the supported workflow and current architecture.
2. Read `DEVELOPMENT_PLAN.md` before changing security, calculation, parser, deployment, or storage behavior.
3. Inspect the relevant backend and frontend code and tests before proposing or implementing a change.
4. Treat uploaded broker CSVs, reports, tokens, and configuration values as sensitive financial data. Never commit, print, upload, or paste real exports or secrets.

## Repository scope

This is a Finnish/English crypto tax-reporting application:

- Backend: ASP.NET Core Minimal API targeting `.NET 10`.
- Frontend: React, TypeScript, and Vite.
- Currently implemented broker: Coinmotion CSV.
- Binance configuration is a placeholder and must not be presented as implemented.
- Generated reports are currently stored in memory and removed after download.

## Validation commands

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

The initial inspection environment did not have the .NET SDK. A user-local .NET 10 SDK is now installed at `/home/mikau/.dotnet`; export `DOTNET_ROOT=/home/mikau/.dotnet` and prepend it to `PATH` when running backend commands in this environment.

Use the narrowest relevant validation after a change, then run the broader applicable checks before declaring completion. Run `git diff --check` before finishing.

## Implementation rules

- Preserve the separation between Domain, Application, Infrastructure, and API layers.
- Use decimal-safe financial arithmetic where appropriate; do not change tax or rounding semantics without tests and an explicit explanation.
- Add or update synthetic fixtures and tests for parser, calculation, and endpoint behavior when changing those areas.
- Do not silently skip malformed CSV rows or invalid numeric/date values in new code; make the behavior explicit and tested.
- Do not expose raw exception messages, CSV contents, tokens, or internal paths in API responses or logs.
- Keep upload size, rate limiting, authentication, CORS, retention, and HTTPS concerns visible when touching the HTTP boundary. Do not claim the application is production-safe without evidence.
- Keep unsupported brokers unavailable until both backend support, frontend wiring, translations, and tests exist.
- Do not add real financial exports or generated reports to the repository.

## Documentation and changes

- Update `README.md` when setup, commands, API behavior, supported brokers, or architecture changes.
- Update `DEVELOPMENT_PLAN.md` when inspection findings, risks, priorities, or acceptance criteria change.
- Keep documentation factual: distinguish observed behavior, verified results, blocked checks, and recommendations.
- Do not commit or push changes unless the user explicitly asks for it.
- Before finishing, report changed files, commands actually run, passed checks, blocked checks, and remaining risks.
