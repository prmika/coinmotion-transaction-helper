# Coinmotion Tax Helper

## Architecture

This is a Python-based cryptocurrency tax reporting tool with two execution modes:

- **CLI**: Batch processing from `./data/input/` CSV files → Excel/PDF reports in `./data/output/`
- **API**: FastAPI server accepting CSV uploads → returns `pdf_reports.zip`

### Data Flow Pipeline

```
CSV Upload → CsvReader → create_tax_report() → FIFO processor → PdfWriter/XlsWriter → Output
```

Key architectural points:

- **Per-currency FIFO tracking**: Each cryptocurrency maintains its own FIFO queue in `src/helpers/fifo.py`
- **Transaction splitting**: Sell transactions may generate multiple output rows if they span multiple purchase lots
- **Year-based grouping**: Reports aggregate data by year with summary sheets

### Core Components

- `src/readers/CsvReader.py`: Parses Coinmotion CSV exports (both file and stream)
- `src/processor.py`: Groups transactions by currency, applies FIFO using `src/helpers/fifo.py`
- `src/helpers/fifo.py`: FIFO queue with cost basis calculation, handles 10-year holding period (40% vs 20% assumed cost)
- `src/writers/XlsWriter.py`: Generates Excel files with yearly summary + transaction detail sheets
- `src/writers/PdfWriter.py`: Creates PDF reports bundled into a zip archive
- `src/api.py`: FastAPI endpoints with CORS enabled for localhost:5173

## Development Workflows

### Running Locally

**CLI Mode** (processes single CSV from `./data/input/`):

```powershell
python src\main.py
```

**API Mode** (starts FastAPI server):

```powershell
uvicorn src.api:app --reload
```

**Virtual Environment** (typically activated in terminal):

```powershell
& .venv\Scripts\Activate.ps1
```

### Testing

Run pytest test suite:

```powershell
python -m pytest
```

Tests are in `tests/` and use `conftest.py` for fixtures. Key test file: `test_processor.py` validates FIFO logic across multiple currencies.

## Project-Specific Conventions

### Transaction Structure

Transactions always have `fromCurrency` and `toCurrency`. EUR is the base currency:

- **Buy**: `fromCurrency: "EUR"` → adds to FIFO queue
- **Sell**: `toCurrency: "EUR"` → consumes from FIFO queue
- **Crypto-to-crypto**: Handled as-is without FIFO processing

### FIFO Behavior

- Uses `EPSILON` (from `src/config.py`) for floating-point comparisons
- Lot holding period ≥ 3650 days → 40% assumed cost rate, otherwise 20%
- Raises `ValueError` if inventory insufficient for sell transaction
- Returns `consumed_lots` list showing which purchase lots were used

### Error Handling Patterns

- `src/main.py` enforces exactly one CSV file in `./data/input/` folder
- API validates UTF-8 encoding and `.csv` extension
- Optional `year` parameter in API filters report to specific year

## Key Files

- [src/main.py](src/main.py): CLI entry point
- [src/processor.py](src/processor.py): Core report generation logic
- [src/helpers/fifo.py](src/helpers/fifo.py): FIFO queue with tax calculation
- [src/api.py](src/api.py): FastAPI server with `/report/pdf-zip` endpoint
- [tests/test_processor.py](tests/test_processor.py): FIFO validation tests
