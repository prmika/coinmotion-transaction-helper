# Development

## Branching

- `main` — production. CI enforces checks on PRs to `main` and pushes to `main`.
- Feature work goes on feature branches (e.g. `feat/*`, `wt/*` for worktrees).
- Keep branches small and reviewable; do not force-push shared branches.

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

Run `git diff --check` before finishing to catch whitespace errors.

## Tests

- `Domain.Tests` — FIFO unit tests.
- `Application.Tests` — service logic.
- `Integration.Tests` — HTTP pipeline.
- `frontend` — lint + build (Playwright e2e smoke tests are wired into CI).

## Debugging

F5 in VS Code using the `.NET Core Launch (web)` config. Requires the C# Dev Kit
extension.

## Conventions

- **All UI text goes through `frontend/src/i18n.ts`** (both `en` and `fi`). Never hardcode strings.
- Treat uploaded broker CSVs, reports, tokens, and config values as sensitive.
- Never present Binance as implemented.

## VS Code tasks

`frontend/src/tasks.json` and `frontend/src/launch.json` drive lint/build/debug for
the frontend.
