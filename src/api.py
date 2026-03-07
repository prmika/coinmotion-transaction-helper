from typing import Optional
import uuid
import logging

from fastapi import FastAPI, File, UploadFile, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import StreamingResponse

from processor import create_tax_report
from readers.CsvReader import read_csv_stream
from writers.PdfWriter import build_pdf_zip_bytes

logger = logging.getLogger(__name__)
app = FastAPI(title="coinmotion-transaction-helper")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:5173"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# In-memory storage for reports holding state between generation and download.
# For production, this should be moved to Redis or a Database.
report_storage = {}


@app.post("/report/generate")
async def generate_report_metadata(file: UploadFile = File(...), year: Optional[int] = None):
    if file.filename is None or not file.filename.lower().endswith(".csv"):
        raise HTTPException(status_code=400, detail="Upload a .csv file")

    content = await file.read()
    try:
        csv_text = content.decode("utf-8")
    except UnicodeDecodeError:
        raise HTTPException(status_code=400, detail="CSV must be UTF-8 encoded")

    try:
        transactions = read_csv_stream(csv_text)
        if transactions is None:
            raise HTTPException(status_code=400, detail="Failed to parse CSV file")
        report = create_tax_report(transactions)
        if year is not None:
            report = _filter_report_by_year(report, str(year))
            if not report:
                raise HTTPException(
                    status_code=400,
                    detail=f"No report data found for year {year}.",
                )
        
        total_sales_volume = 0.0
        total_transactions = 0
        total_profit = 0.0

        for currency, data in report.items():
            total_transactions += len(data.get("transactions", []))
            for year_data in data.get("years", {}).values():
                total_sales_volume += year_data.get("wins", 0.0)
                total_profit += year_data.get("total", 0.0)

        report_id = str(uuid.uuid4())
        report_storage[report_id] = report

        return {
            "report_id": report_id,
            "pricing_metrics": {
                "total_sales_transactions": total_transactions,
                "total_sales_volume_eur": round(total_sales_volume, 2),
                "total_profit_loss_eur": round(total_profit, 2)
            }
        }
    except HTTPException:
        raise
    except Exception as exc:
        raise HTTPException(status_code=400, detail=str(exc))

@app.get("/report/download/{report_id}")
async def download_report_zip(report_id: str):
    if report_id not in report_storage:
        raise HTTPException(status_code=404, detail="Report not found or has expired")
    
    try:
        report_data = report_storage[report_id]
        zip_bytes = build_pdf_zip_bytes(report_data)
        
        # Optionally cleanup immediately after download to save RAM 
        del report_storage[report_id]

        return StreamingResponse(
            iter([zip_bytes]),
            media_type="application/zip",
            headers={"Content-Disposition": "attachment; filename=pdf_reports.zip"},
        )
    except Exception as exc:
        logger.error(f"Failed to generate ZIP: {exc}")
        raise HTTPException(status_code=500, detail="Failed to generate the ZIP file")


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
