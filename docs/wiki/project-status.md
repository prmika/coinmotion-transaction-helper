# Project status

> Keep this page current. It is intentionally concise — a snapshot of where the
> project stands and what is in flight.

## Implemented

- **Coinmotion CSV** broker — full upload → PDF report flow.
- Backend: `.NET 10` Minimal API, FIFO cost basis, PDF ZIP report.
- Frontend: React + TypeScript + Vite, fi/en i18n.
- CI: backend test + frontend lint/build on PRs to `main`.

## Not yet implemented

- **Binance** — configured as a placeholder only. Do not present as implemented.
- **OIDC authentication & report authorization** — open PR #1 (`feat/authentication`).
- Additional brokers beyond Coinmotion.

## In flight / branches

Feature and exploration work lives on separate branches/worktrees (e.g.
`chore/ci-node24`, `feat/playwright-e2e`, `feat/staging-test-env`, `wt/*`).
Do not treat unmerged branch work as shipped.

## How to read the branches

- `wt/*` — worktree scratch branches; not long-lived.
- `feat/*` — feature work.
- `chore/*` — tooling / CI / repo hygiene.
- `fix/*` — bugfixes.
- `pr/*` — remote PR branch tracking.

## Review against this page

When reviewing a PR, check whether it advances something listed here and whether
the status page was updated to match.
