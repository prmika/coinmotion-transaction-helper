# Getting started

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/)

## Clone

```bash
git clone https://github.com/prmika/coinmotion-transaction-helper.git
cd coinmotion-transaction-helper
```

## Build

### Backend

```bash
cd backend
dotnet restore
dotnet build
```

### Frontend

```bash
cd frontend
npm install
```

## Run

Start both in separate terminals:

```bash
# Terminal 1 — API  (http://localhost:8000)
cd backend
dotnet run --project src/CryptoTaxHelper.Api

# Terminal 2 — Frontend (http://localhost:5173)
cd frontend
npm run dev
```

Open <http://localhost:5173>.

## Checks

CI runs backend tests and frontend lint/build on PRs targeting `main` and pushes
to `main` (see [architecture](architecture.md#ci)). Run the same locally:

```bash
# Backend
cd backend
dotnet test

# Frontend
cd ../frontend
npm ci
npm run lint
npm run build
```

## Sensitivity

Broker CSVs, reports, tokens, and configuration values are treated as **sensitive
financial data**. Never commit, print, upload, or paste real exports or secrets.
