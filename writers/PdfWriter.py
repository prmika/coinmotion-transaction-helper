import os
from datetime import datetime
from io import BytesIO
import zipfile

from reportlab.lib import colors
from reportlab.lib.pagesizes import A4, landscape
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle

from config import REPORT_VERSION


OUTPUT_HEADERS = [
    "Time",
    "Type",
    "Crypto\nAmount",
    "Rate",
    "Amount €",
    "Source",
    "From\nCurrency",
    "To\nCurrency",
    "Remaining\nQuantity",
    "Cost Basis €",
    "Assumed Cost €",
    "Cost Basis Used",
    "Cost Basis Method",
    "Selling fee €",
    "Profit/Loss €",
]

YEAR_HEADERS = [
    "Year",
    "From Time",
    "Wins €",
    "Losses €",
    "Total €",
]


def write_pdf_zip(objects, output_folder="./output/", zip_name="pdf_reports.zip"):
    if not objects:
        print("No objects to write")
        return

    if not os.path.exists(output_folder):
        os.makedirs(output_folder)

    zip_bytes = build_pdf_zip_bytes(objects)
    zip_path = os.path.join(output_folder, zip_name)
    with open(zip_path, "wb") as handle:
        handle.write(zip_bytes)


def build_pdf_zip_bytes(objects):
    buffer = BytesIO()
    with zipfile.ZipFile(buffer, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        for currency, data in objects.items():
            pdf_bytes = _build_pdf_bytes(currency, data)
            filename = f"{_sanitize_filename(currency)}.pdf"
            archive.writestr(filename, pdf_bytes)
    return buffer.getvalue()


def _build_pdf_bytes(currency, data):
    buffer = BytesIO()
    doc = SimpleDocTemplate(
        buffer,
        pagesize=landscape(A4),
        leftMargin=20,
        rightMargin=20,
        topMargin=20,
        bottomMargin=20,
    )
    styles = getSampleStyleSheet()
    disclaimer_style = ParagraphStyle(
        "Disclaimer",
        parent=styles["Normal"],
        fontSize=8,
        leading=10,
        textColor=colors.black,
        spaceBefore=4,
        spaceAfter=8,
    )

    elements = [Paragraph(f"Tax Report - {currency}", styles["Title"]), Spacer(1, 6)]
    elements.append(Paragraph(f"Version {REPORT_VERSION}", disclaimer_style))
    elements.append(Paragraph(f"Generated on {datetime.now().strftime('%d.%m.%Y %H:%M:%S')}", disclaimer_style))
    elements.append(Paragraph("Tämä raportti on automaattisesti muodostettu Coinmotionin toimittamien transaktiotietojen sekä käyttäjän antamien lähtötietojen perusteella. / This report has been automatically generated based on transaction data provided by Coinmotion and information supplied by the user.", disclaimer_style))

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
        ["Profit/Loss €", "Myyntivoitto/-tappio"],
        ["fifo", "First In First Out -menetelmä (hankintameno)"],
        ["assumption", "Hankintameno-olettama"],
    ]
    elements.append(_make_table(dictionary_rows, col_widths=[100, 400]))
    elements.append(Spacer(1, 36))

    elements.append(Paragraph("Yearly Summary *", styles["Heading2"]))
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
    elements.append(Paragraph("* Luvuista on vähennetty mahdolliset osto- ja myyntikulut. / The figures have been reduced by possible purchase and sale fees.", disclaimer_style))
    elements.append(Spacer(1, 16))

    elements.append(Paragraph("Transactions **", styles["Heading2"]))
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
                _format_eur(item.get("fee", "")) if item["type"] == "sell" and item.get("costBasisMethod", "") == "fifo" else "",
                _format_eur(item.get("profitLoss", "")),
            ]
        )
    elements.append(
        _make_table(
            tx_rows,
            repeat_header=True,
            col_widths=_transaction_col_widths(doc.width),
        )
    )
    elements.append(Paragraph("** Voitto/tappio on laskettu Amount € - Cost Basis Used - Selling fee €. / Profit/loss is calculated as Amount € - Cost Basis Used - Selling fee €.", disclaimer_style)) 

    elements.append(Spacer(1, 12))
    elements.append(Paragraph("Disclaimer", styles["Heading2"]))
    elements.append(
        Paragraph(
            """Tämä raportti on automaattisesti muodostettu Coinmotionin toimittamien transaktiotietojen sekä käyttäjän antamien lähtötietojen perusteella.<br/><br/>
            Raportti on suuntaa-antava eikä ole veroneuvontaa. Palvelu ei takaa raportin tietojen täydellisyyttä, oikeellisuutta tai soveltuvuutta käyttäjän yksittäiseen verotustilanteeseen. Käyttäjä vastaa itse tietojen oikeellisuudesta ja veroilmoitukselle ilmoitettavista luvuista.<br/><br/>
            Raportti ei välttämättä huomioi oikein tai kattavasti kaikkia seuraavia tapahtumia:<br/>
            • lompakkojen välisiä siirtoja<br/>
            • ulkopuolisista pörsseistä tai palveluista tehtyjä transaktioita<br/>
            • staking-, lending- tai muita tuottotapahtumia<br/>
            • DeFi-tapahtumia<br/>
            • airdroppeja ja hard fork -tapahtumia<br/>
            • NFT-kauppaa<br/><br/>
            Raportissa esitetyt laskelmat (esim. todellinen hankintahinta tai hankintameno-olettama) ovat laskennallisia. Hankintameno-olettaman käyttö ja lopullinen verotuksellinen valinta on aina käyttäjän vastuulla.<br/><br/>
            Palvelun tarjoaja ei vastaa mahdollisista veroseuraamuksista, veronkorotuksista tai muista vahingoista, jotka aiheutuvat raportin käytöstä.<br/><br/>
            Ajantasaiset ja sitovat ohjeet löytyvät Verohallinnon verkkosivuilta. Epäselvissä tilanteissa suositellaan ottamaan yhteyttä veroasiantuntijaan.""",
            disclaimer_style,
        )
    )
    elements.append(Spacer(1, 8))
    elements.append(
        Paragraph(
            """This report has been automatically generated based on transaction data provided by Coinmotion and information supplied by the user.<br/><br/>
            This report is for informational purposes only and does not constitute tax advice. The service does not guarantee the completeness, accuracy, or suitability of the report for the user’s individual tax situation. The user is solely responsible for verifying the correctness of the information and the figures reported to the tax authorities.<br/><br/>
            The report may not fully or correctly account for the following events:<br/>
            • transfers between wallets<br/>
            • transactions from external exchanges or services<br/>
            • staking, lending, or yield-related income<br/>
            • DeFi transactions<br/>
            • airdrops and hard forks<br/>
            • NFT transactions<br/><br/>
            Any calculations presented in the report (e.g. actual acquisition cost or deemed acquisition cost) are estimates. The choice and applicability of the deemed acquisition cost method is always the responsibility of the user.<br/><br/>
            The service provider shall not be held liable for any tax consequences, penalties, or damages arising from the use of this report.<br/><br/>
            For official and binding guidance, please refer to the Finnish Tax Administration or consult a qualified tax professional.""",
            disclaimer_style,
        )
    )

    
    doc.build(elements)
    return buffer.getvalue()


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
        0.05,  # Cost Basis Used
        0.06,  # Cost Basis Method
        0.06,  # Selling fee €
        0.05,  # Profit/Loss €
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
        amount = float(value)
        if abs(amount) <= 1e-8:
            return "0"
        return f"{amount:.8f}"
    except (TypeError, ValueError):
        return value
