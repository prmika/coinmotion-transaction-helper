import csv
import logging
from io import StringIO
from datetime import datetime

logger = logging.getLogger(__name__)

def read_csv(file_path: str):
    transactions = []
    with open(file_path, mode='r', encoding='utf-8') as file:
        transactions = _parse_csv_reader(csv.DictReader(file))
    if not transactions:
        logger.warning("No transactions found in the CSV file.")
        return []
    return create_objects_from_csv(transactions)


def read_csv_stream(content: str):
    transactions = _parse_csv_reader(csv.DictReader(StringIO(content)))
    if not transactions:
        return []
    return create_objects_from_csv(transactions)


def _parse_csv_reader(reader):
    transactions = []
    for row in reader:
        try:
            transactions.append({
                "fromCurrency": row["fromCurrency"].strip().upper(),
                "toCurrency": row["toCurrency"].strip().upper(),
                "type": row["type"],
                "eurAmount": float(row["eurAmount"]) if row["eurAmount"] else 0.0,
                "cryptoAmount": float(row["cryptoAmount"]) if row["cryptoAmount"] else 0.0,
                "rate": float(row["rate"]) if row["rate"] else 0.0,
                "fee": float(row["fee"]) if row["fee"] else 0.0,
                "feeCurrency": row["feeCurrency"].strip().upper(),
                "time": datetime.strptime(row["time"], "%Y-%m-%dT%H:%M:%S%z"),
                "source": "Coinmotion Oy",
            })
        except (ValueError, KeyError) as e:
            raise ValueError(f"Error parsing row {reader.line_num}: {e}")
    return transactions

def create_objects_from_csv(transactions):
    sells = []
    buys = []
    transfers = []

    for transaction in transactions:
        from_currency = transaction["fromCurrency"].strip().upper()
        to_currency = transaction["toCurrency"].strip().upper()
        type_ = transaction["type"].strip().lower()
        
        if type_ in ['deposit', 'withdrawal']:
            continue

        if type_ == 'account_transfer_in':
            transfers.append(handle_account_transfer_in(transaction))
            continue

        if from_currency == 'EUR' and to_currency != 'EUR':
            transaction["type"] = "buy"
            buys.append(transaction)
            continue

        if to_currency == 'EUR' and from_currency != 'EUR':
            transaction["type"] = "sell"
            sells.append(transaction)

    objects = sells + buys + transfers

    return sort_by_date(objects)

def handle_account_transfer_in(transaction):
    # Process account_transfer_in transactions
    # For now we assume that account transfer is buy
    return {
        "fromCurrency": "EUR",
        "toCurrency": transaction["toCurrency"],
        "type": "buy",
        "eurAmount": 0,
        "cryptoAmount": transaction["cryptoAmount"],
        "rate": transaction["rate"],
        "fee": transaction["fee"],
        "feeCurrency": transaction["feeCurrency"],
        "time": transaction["time"],
        "source": transaction["source"]
    }


def sort_by_date(rows):
    try:
        if len(rows) == 0:
            return rows
        else:
            return sorted(rows, key=lambda row: row['time'])
    except Exception as e:
        logger.error(f"Error sorting rows by date: {e}")