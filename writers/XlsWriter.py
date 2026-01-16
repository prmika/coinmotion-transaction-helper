import openpyxl
import os
import re

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
                        item["time"],
                        item["type"],
                        item["cryptoAmount"],
                        item["rate"],
                        item["eurAmount"],
                        item["source"],
                        item["fromCurrency"],
                        item["toCurrency"],
                        item["fee"],
                        item["feeCurrency"],
                        item.get("remainingQuantity", ""),
                        item.get("costBasis", ""),
                        item.get("assumedCost", ""),
                        item.get("costBasisUsed", ""),
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


def _write_year_summary(ws, years):
    ws.append(["Yearly Summary"])
    ws.append(YEAR_HEADERS)

    for year in sorted(years.keys()):
        summary = years[year]
        ws.append(
            [
                year,
                summary.get("fromTime", ""),
                summary.get("wins", 0),
                summary.get("losses", 0),
                summary.get("total", 0),
            ]
        )

    ws.append([])