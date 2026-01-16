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
        for tx in data["transactions"]:
            tx_year = tx["time"].split("-")[0]
            _ensure_year_entry(data, tx_year)

            if tx["fromCurrency"] == "EUR":
                _handle_buy_transaction(fifo, tx)
            elif tx["toCurrency"] == "EUR":
                _handle_sell_transaction(fifo, data, tx, tx_year)

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
        return

    sold_crypto = {
        "amount": tx["cryptoAmount"],
        "total_revenue": tx["eurAmount"],
    }

    sold_time = _parse_time(tx["time"])
    cost_basis, assumed_cost = fifo.calculate_cogs(
        sold_crypto["amount"],
        sold_time,
        sold_crypto["total_revenue"],
    )
    cost_basis_used = max(cost_basis, assumed_cost)
    cost_basis_method = "assumption" if assumed_cost > cost_basis else "fifo"
    profit_loss = sold_crypto["total_revenue"] - cost_basis_used

    tx["costBasis"] = cost_basis
    tx["assumedCost"] = assumed_cost
    tx["costBasisUsed"] = cost_basis_used
    tx["costBasisMethod"] = cost_basis_method

    tx["remainingQuantity"] = fifo.remaining_quantity()

    if profit_loss > 0:
        data["years"][tx_year]["wins"] += profit_loss
    else:
        data["years"][tx_year]["losses"] += abs(profit_loss)

    data["years"][tx_year]["total"] += round(profit_loss, 2)


def _parse_time(value):
    return datetime.strptime(value, "%Y-%m-%dT%H:%M:%S%z")
