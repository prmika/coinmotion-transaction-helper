import csv
from datetime import datetime

def read_csv(file_path: str):
    transactions = []
    with open(file_path, mode='r', encoding='utf-8') as file:
        reader = csv.DictReader(file)
        for row in reader:
            try:
                transactions.append({
                    "fromCurrency": row["fromCurrency"],
                    "toCurrency": row["toCurrency"],
                    "type": row["type"],
                    "eurAmount": float(row["eurAmount"]) if row["eurAmount"] else 0.0,
                    "cryptoAmount": float(row["cryptoAmount"]) if row["cryptoAmount"] else 0.0,
                    "rate": float(row["rate"]) if row["rate"] else 0.0,
                    "fee": float(row["fee"]) if row["fee"] else 0.0,
                    "feeCurrency": row["feeCurrency"],
                    "time": row["time"],
                    "source": "Coinmotion Oy"
                })
            except (ValueError, KeyError) as e:
                raise ValueError(f"Error parsing row {reader.line_num}: {e}")
    if not transactions:
        print("No transactions found in the CSV file.")
        return []
    return create_objects_from_csv(transactions)

def create_objects_from_csv(transactions):
    result_1 = []  # sell transactions that has fromCurrency else that 'EUR' or 'eur'
    result_2 = []  # buy transactions that has toCurrency else that 'EUR' or 'eur'
    result_3 = []  # Everything else

    for transaction in transactions:
        from_currency = transaction["fromCurrency"].strip().lower()
        to_currency = transaction["toCurrency"].strip().lower()
        type_ = transaction["type"].strip().lower()
        
        if type_ in ['deposit', 'withdrawal']:
            # Skip deposits and withdrawals
            continue

        if from_currency != 'EUR':
            result_1.append(transaction)
        if to_currency == 'EUR':
            if type_ == 'deposit' or type_ == 'withdrawal':
                continue
            result_2.append(transaction)
        else:
            if transaction["type"].strip().lower() == 'account_transfer_in':
                result_3.append(handleAccount_transfer_in(transaction))

    objects = result_1 + result_2 + result_3

    return sort_by_date(objects)

def handleAccount_transfer_in(transaction):
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
            return sorted(rows, key=lambda row: datetime.strptime(row['time'], "%Y-%m-%dT%H:%M:%S%z"))
    except Exception as e:
        print(f"Error sorting rows by date: {e}")