# API reference

Base URL: `http://localhost:8000` (override with the deployed host).

## Endpoints

| Method | Path                              | Description                                             |
| ------ | --------------------------------- | ------------------------------------------------------- |
| POST   | `/report/generate`                | Upload CSV (multipart), optional `?year=2024`. Returns `report_id` + metrics. |
| GET    | `/report/download/{reportId}`     | Download `pdf_reports.zip`.                              |

### POST /report/generate

- Uploads a `.csv` (multipart).
- Optional `?year=YYYY` query param filters the report to a specific year.
- Returns `report_id` and pricing metrics.

### GET /report/download/{reportId}

- Returns the generated `pdf_reports.zip`.
- Reports are stored **in memory** and are removed after download.

## Validation

- The `.csv` extension and non-empty file are validated.
- CSV parse errors raise `FormatException` (with the offending line number).
- Optional `year` query param filters the report to a specific year.
