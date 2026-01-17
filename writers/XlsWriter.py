import openpyxl
import os
import re
from datetime import datetime

OUTPUT_HEADERS = [
    "Time",
    "Type",
    "Crypto Amount",
    "Rate",
    "Amount €",
    "Source",
    "From Currency",
    "To Currency",
    "Fee",
    "Fee Currency",
    "Remaining Quantity",
    "Cost Basis €",
    "Assumed Cost €",
    "Cost Basis Used",
    "Cost Basis Method",
]

YEAR_HEADERS = [
    "Year",
    "From Time",
    "Wins €",
    "Losses €",
    "Total €",
]


def write_xls(objects):
    if not objects:
        print("No objects to write")
        return

    output_folder = "./output/"
    if not os.path.exists(output_folder):
        os.makedirs(output_folder)

    try:
        for currency, data in objects.items():
            wb = openpyxl.Workbook()
            ws = wb.active
            ws.title = currency

            _write_year_summary(ws, data.get("years", {}))

            ws.append(OUTPUT_HEADERS)

            for item in data.get("transactions", []):
                ws.append(
                    [
                        _format_time(item["time"]),
                        item["type"],
                        item["cryptoAmount"],
                        item["rate"],
                        _format_eur(item["eurAmount"]),
                        item["source"],
                        item["fromCurrency"],
                        item["toCurrency"],
                        _format_eur(item["fee"]),
                        item["feeCurrency"],
                        _format_remaining_quantity(item.get("remainingQuantity", "")),
                        _format_eur(item.get("costBasis", "")),
                        _format_eur(item.get("assumedCost", "")),
                        _format_eur(item.get("costBasisUsed", "")),
                        item.get("costBasisMethod", ""),
                    ]
                )

            filename = f"{_sanitize_filename(currency)}.xlsx"
            wb.save(os.path.join(output_folder, filename))

    except Exception as e:
        print(f"Error writing XLSX file: {e}")
        return


def _sanitize_filename(name):
    cleaned = re.sub(r"[^A-Za-z0-9._-]+", "_", name.strip())
    return cleaned or "UNKNOWN"


def _format_time(value):
    try:
        parsed = datetime.strptime(value, "%Y-%m-%dT%H:%M:%S%z")
        return parsed.strftime("%d.%m.%Y %H:%M:%S")
    except (TypeError, ValueError):
        return value


def _format_eur(value):
    try:
        return round(float(value), 2)
    except (TypeError, ValueError):
        return value


def _format_remaining_quantity(value):
    try:
        if abs(float(value)) <= 1e-12:
            return 0.0
        return float(value)
    except (TypeError, ValueError):
        return value


def _write_year_summary(ws, years):
    ws.append(["Yearly Summary"])
    ws.append(YEAR_HEADERS)

    for year in sorted(years.keys()):
        summary = years[year]
        ws.append(
            [
                year,
                summary.get("fromTime", ""),
                _format_eur(summary.get("wins", 0)),
                _format_eur(summary.get("losses", 0)),
                _format_eur(summary.get("total", 0)),
            ]
        )

    ws.append([])