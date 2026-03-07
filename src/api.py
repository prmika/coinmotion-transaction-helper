from typing import Optional

from fastapi import FastAPI, File, UploadFile, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import StreamingResponse

from processor import create_tax_report
from readers.CsvReader import read_csv_stream
from writers.PdfWriter import build_pdf_zip_bytes

app = FastAPI(title="coinmotion-transaction-helper")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:5173"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.post("/report/pdf-zip")
async def report_pdf_zip(file: UploadFile = File(...), year: Optional[int] = None):
    if file.filename is None or not file.filename.lower().endswith(".csv"):
        raise HTTPException(status_code=400, detail="Upload a .csv file")

    content = await file.read()
    try:
        csv_text = content.decode("utf-8")
    except UnicodeDecodeError:
        raise HTTPException(status_code=400, detail="CSV must be UTF-8 encoded")

    try:
        transactions = read_csv_stream(csv_text)
        report = create_tax_report(transactions)
        if year is not None:
            report = _filter_report_by_year(report, str(year))
            if not report:
                raise HTTPException(
                    status_code=400,
                    detail=f"No report data found for year {year}.",
                )
        zip_bytes = build_pdf_zip_bytes(report)
    except HTTPException:
        raise
    except Exception as exc:
        raise HTTPException(status_code=400, detail=str(exc))

    return StreamingResponse(
        iter([zip_bytes]),
        media_type="application/zip",
        headers={"Content-Disposition": "attachment; filename=pdf_reports.zip"},
    )


def _filter_report_by_year(report, year):
    filtered = {}
    for currency, data in report.items():
        years = data.get("years", {})
        if year not in years:
            continue

        filtered[currency] = {
            **data,
            "years": {year: years[year]},
            "transactions": data.get("transactions", []),
        }

    return filtered
