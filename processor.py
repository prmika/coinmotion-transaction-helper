from datetime import datetime

from helpers.fifo import FIFO


def create_tax_report(objects):
    """
    Create a tax report from the given objects.
    This function processes the transactions and returns a structured report.
    """
    if not objects:
        return []

    results = _group_transactions_by_currency(objects)
    fifo_by_currency = {}

    for currency, data in results.items():
        fifo = fifo_by_currency.setdefault(currency, FIFO())
        processed_transactions = []
        for tx in data["transactions"]:
            tx_year = tx["time"].split("-")[0]
            _ensure_year_entry(data, tx_year)

            if tx["fromCurrency"] == "EUR":
                _handle_buy_transaction(fifo, tx)
                processed_transactions.append(tx)
            elif tx["toCurrency"] == "EUR":
                processed_transactions.extend(_handle_sell_transaction(fifo, data, tx, tx_year))
            else:
                processed_transactions.append(tx)

        data["transactions"] = processed_transactions

    return results


def _group_transactions_by_currency(objects):
    results = {}

    for obj in objects:
        to_currency = obj["toCurrency"]
        if to_currency != "EUR":
            results.setdefault(to_currency, {"years": {}, "transactions": []})
            results[to_currency]["transactions"].append(obj)

        from_currency = obj["fromCurrency"]
        if from_currency != "EUR":
            if from_currency not in results:
                raise ValueError(
                    f"Currency {from_currency} not found in results, but it should be initialized."
                )
            results[from_currency]["transactions"].append(obj)

    return results


def _ensure_year_entry(data, tx_year):
    if tx_year not in data["years"]:
        data["years"][tx_year] = {
            "fromTime": f"1.1.{tx_year}-31.12.{tx_year}",
            "wins": 0,
            "losses": 0,
            "total": 0,
        }


def _handle_buy_transaction(fifo, tx):
    if tx["cryptoAmount"] <= 0:
        return
    fifo.add_purchase(
        tx["cryptoAmount"],
        (tx["eurAmount"] / tx["cryptoAmount"]),
        _parse_time(tx["time"]),
    )


def _handle_sell_transaction(fifo, data, tx, tx_year):
    if tx["cryptoAmount"] <= 0:
        return []

    sold_crypto = {
        "amount": tx["cryptoAmount"],
        "total_revenue": tx["eurAmount"],
    }

    sold_time = _parse_time(tx["time"])
    cost_basis, assumed_cost, consumed_lots = fifo.calculate_cogs(
        sold_crypto["amount"],
        sold_time,
        sold_crypto["total_revenue"],
    )

    price_per_unit = sold_crypto["total_revenue"] / sold_crypto["amount"]
    remaining_before = fifo.remaining_quantity() + sold_crypto["amount"]
    cumulative_sold = 0.0
    split_transactions = []

    for lot in consumed_lots:
        lot_quantity = lot["quantity"]
        lot_revenue = lot_quantity * price_per_unit
        lot_cost_basis = lot_quantity * lot["price"]
        lot_held_long = (sold_time - lot["time"]).days >= 3650
        lot_assumed_cost = lot_revenue * (0.4 if lot_held_long else 0.2)
        lot_cost_basis_used = max(lot_cost_basis, lot_assumed_cost)
        lot_method = "assumption" if lot_assumed_cost > lot_cost_basis else "fifo"
        lot_profit_loss = lot_revenue - lot_cost_basis_used

        cumulative_sold += lot_quantity
        remaining_after = remaining_before - cumulative_sold

        split_tx = dict(tx)
        split_tx["cryptoAmount"] = lot_quantity
        split_tx["eurAmount"] = lot_revenue
        split_tx["costBasis"] = lot_cost_basis
        split_tx["assumedCost"] = lot_assumed_cost
        split_tx["costBasisUsed"] = lot_cost_basis_used
        split_tx["costBasisMethod"] = lot_method
        split_tx["remainingQuantity"] = remaining_after

        if lot_profit_loss > 0:
            data["years"][tx_year]["wins"] += lot_profit_loss
        else:
            data["years"][tx_year]["losses"] += abs(lot_profit_loss)

        data["years"][tx_year]["total"] += round(lot_profit_loss, 2)

        split_transactions.append(split_tx)

    return split_transactions


def _parse_time(value):
    return datetime.strptime(value, "%Y-%m-%dT%H:%M:%S%z")
