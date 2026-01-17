import os
from datetime import datetime

from reportlab.lib import colors
from reportlab.lib.pagesizes import A4, landscape
from reportlab.lib.styles import getSampleStyleSheet
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle

from config import REPORT_VERSION


OUTPUT_HEADERS = [
    "Time",
    "Type",
    "Crypto Amount",
    "Rate",
    "Amount €",
    "Source",
    "From Currency",
    "To Currency",
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


def write_pdf(objects):
    if not objects:
        print("No objects to write")
        return

    output_folder = "./output/"
    if not os.path.exists(output_folder):
        os.makedirs(output_folder)

    for currency, data in objects.items():
        filename = os.path.join(output_folder, f"{_sanitize_filename(currency)}.pdf")
        doc = SimpleDocTemplate(
            filename,
            pagesize=landscape(A4),
            leftMargin=20,
            rightMargin=20,
            topMargin=20,
            bottomMargin=20,
        )
        styles = getSampleStyleSheet()

        elements = [Paragraph(f"Tax Report - {currency}", styles["Title"]), Spacer(1, 6)]
        elements.append(Paragraph(f"Version {REPORT_VERSION}", styles["Normal"]))
        elements.append(Paragraph(f"Generated on {datetime.now().strftime('%d.%m.%Y %H:%M:%S')}", styles["Normal"]))
        elements.append(Paragraph("Legend", styles["Heading2"]))
        elements.append(Paragraph("This report provides a detailed summary of your cryptocurrency transactions, including yearly summaries and individual transaction details. Double-check the data for accuracy and consult a tax professional if needed.", styles["Normal"]))
        elements.append(Paragraph("Tämä raportti tarjoaa yksityiskohtaisen yhteenvedon kryptovaluuttatapahtumistasi, mukaan lukien vuosittaiset yhteenvedot ja yksittäisten tapahtumien tiedot. Tarkista tietojen oikeellisuus ja ota tarvittaessa yhteyttä veroasiantuntijaan.", styles["Normal"]))


        elements.append(Paragraph("Dictionary", styles["Heading2"]))
        dictionary_rows = [
            ["Key", "Description"],
            ["Year", "Vuosi"],
            ["From Time", "Laskentajakso"],
            ["Wins €", "Voitot yhteensä euroina"],
            ["Losses €", "Tappiot yhteensä euroina"],
            ["Total €", "Nettovoitto/-tappio euroina"],
            ["Time", "Tapahtuma-aika"],
            ["type", "Tapahtumatyyppi"],
            ["buy", "Ostotapahtuma"],
            ["sell", "Myyntitapahtuma"],
            ["Crypto Amount", "Kryptovaluutan määrä"],
            ["Amount €", "Euro määrä"],
            ["Rate", "Kryptovaluutan kurssi euroissa"],
            ["From Currency", "Mistä valuutasta"],
            ["To Currency", "Mihin valuuttaan"],
            ["Remaining Quantity", "Jäljellä oleva määrä"],
            ["Cost Basis €", "Hankintameno"],
            ["Assumed Cost €", "Hankintameno-olettama 20% tai 40% omistusajan mukaan"],
            ["Cost Basis Used", "Käytetty hankintameno"],
            ["Cost Basis Method", "Hankintamenomenetelmä"],
            ["fifo", "First In First Out -menetelmä (hankintameno)"],
            ["assumption", "Hankintameno-olettama"]
        ]
        elements.append(_make_table(dictionary_rows, col_widths=[100, 400]))
        elements.append(Spacer(1, 36))

        elements.append(Paragraph("Yearly Summary", styles["Heading2"]))
        year_rows = [YEAR_HEADERS]
        for year in sorted(data.get("years", {}).keys()):
            summary = data["years"][year]
            year_rows.append(
                [
                    year,
                    summary.get("fromTime", ""),
                    _format_eur(summary.get("wins", 0)),
                    _format_eur(summary.get("losses", 0)),
                    _format_eur(summary.get("total", 0)),
                ]
            )
        elements.append(_make_table(year_rows, col_widths=_year_col_widths(doc.width)))
        elements.append(Spacer(1, 16))

        elements.append(Paragraph("Transactions", styles["Heading2"]))
        elements.append(Spacer(1, 16))

        tx_rows = [[_header_cell(text, styles) for text in OUTPUT_HEADERS]]
        for item in data.get("transactions", []):
            tx_rows.append(
                [
                    _format_time(item["time"]),
                    item["type"],
                        _format_crypto(item["cryptoAmount"]),
                    item["rate"],
                    _format_eur(item["eurAmount"]),
                    item["source"],
                    item["fromCurrency"],
                    item["toCurrency"],
                    _format_remaining_quantity(item.get("remainingQuantity", "")),
                    _format_eur(item.get("costBasis", "")),
                    _format_eur(item.get("assumedCost", "")),
                    _format_eur(item.get("costBasisUsed", "")),
                    item.get("costBasisMethod", ""),
                ]
            )
        elements.append(
            _make_table(
                tx_rows,
                repeat_header=True,
                col_widths=_transaction_col_widths(doc.width),
            )
        )

        doc.build(elements)


def _make_table(rows, repeat_header=False, col_widths=None):
    table = Table(rows, repeatRows=1 if repeat_header else 0, colWidths=col_widths)
    table.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, 0), colors.lightgrey),
                ("TEXTCOLOR", (0, 0), (-1, 0), colors.black),
                ("GRID", (0, 0), (-1, -1), 0.5, colors.grey),
                ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
                ("FONTSIZE", (0, 0), (-1, -1), 8),
                ("FONTSIZE", (0, 0), (-1, 0), 8),
                ("VALIGN", (0, 0), (-1, -1), "TOP"),
            ]
        )
    )
    return table


def _sanitize_filename(name):
    cleaned = "".join(char if char.isalnum() or char in "._-" else "_" for char in name.strip())
    return cleaned or "UNKNOWN"


def _header_cell(text, styles):
    return Paragraph(text.replace("\n", "<br/>"), styles["BodyText"])


def _format_time(value):
    try:
        parsed = datetime.strptime(value, "%Y-%m-%dT%H:%M:%S%z")
        return parsed.strftime("%d.%m.%Y %H:%M:%S")
    except (TypeError, ValueError):
        return value


def _transaction_col_widths(total_width):
    fractions = [
        0.12,  # Time
        0.06,  # Type
        0.09,  # Crypto Amount
        0.06,  # Rate
        0.07,  # Amount €
        0.08,  # Source
        0.07,  # From Currency
        0.07,  # To Currency
        0.08,  # Remaining Quantity
        0.06,  # Cost Basis €
        0.07,  # Assumed Cost €
        0.06,  # Cost Basis Used
        0.07,  # Cost Basis Method
    ]
    return [total_width * f for f in fractions]


def _year_col_widths(total_width):
    fractions = [0.06, 0.24, 0.08, 0.08, 0.08]
    return [total_width * f for f in fractions]

def _format_crypto(value):
    try:
        return f"{float(value):.8f}"
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
        return round(float(value), 8)
    except (TypeError, ValueError):
        return value
