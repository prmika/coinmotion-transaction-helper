from helpers.fifo import FIFO
def create_tax_report(objects):
    """
    Create a tax report from the given objects.
    This function processes the transactions and returns a structured report.
    """
    if not objects:
        print("No transactions found.")
        return []

    results = {}
    # Example structure of results:
    # results = {
    #     "BTC": {
    #         "years": {
    #             This will be filled with each year's tax report that has for example: 
    #             "2024": {
    #                 "fromTime": "1.1.2024-31.12.2024" -from first to last date of the year,
    #                 "wins": 1000 - all profits,  
    #                 "losses": 500 - all losses, 
    #                 "total": 500 - net profit/loss,
    #               },
    #               "2023":{...},
    #             }
    #         "transactions": [
    #             This will be filled with all transactions, each transaction will have the following structure:
    #             {
    #                 "time": "2024-01-01T00:00:00Z",
    #                 "type": "Buy/Osto",
    #                 "cryptoAmount": 0.5,
    #                 "rate": 30000,
    #                 "eurAmount": 15000,
    #                 "source": "Coinmotion",
    #                 "fromCurrency": "EUR",
    #                 "toCurrency": "BTC",
    #                 "fee": 0.001,
    #                 "feeCurrency": "EUR"
    #             },
    #             {
    #                 "time": "2024-01-02T00:00:00Z",
    #                 "type": "sell/Myynti",
    #                 "cryptoAmount": 0.25,
    #                 "rate": 35000,
    #             ... (and so on for each transaction)
    #             }
    #         ],
    #     },
    # }

    # First we need separate the transactions by currency
    # objects = [obj for obj in objects if obj["toCurrency"] == "MATIC" or obj["fromCurrency"] == "MATIC"]
    for obj in objects:
        currency = obj["toCurrency"]
        if currency != "EUR" and currency not in results:
            results[currency] = {
                "years": {},
                "transactions": [obj]
            }
        elif currency in results:
            results[currency]["transactions"].append(obj)

        # ALSO add sold transactions to the same currency
        if obj["fromCurrency"] != "EUR":
            currency = obj["fromCurrency"]
            if currency not in results:
                # results[currency] should already be initialized create error if not
                raise ValueError(f"Currency {currency} not found in results, but it should be initialized.")
            results[currency]["transactions"].append(obj)
    fifo = FIFO()
    # For debugging lets use only MATIC transactions from objects
    to_be_removed = []
    # Now we need to process each currency's transactions
    for currency, data in results.items():
        # Now we need to calculate the profit/loss for each transaction
        for tx in data["transactions"]:
            tx_year = tx["time"].split("-")[0]
            if tx_year not in data["years"]:
                data["years"][tx_year] = {
                    "fromTime": f"1.1.{tx_year}-31.12.{tx_year}",
                    "wins": 0,
                    "losses": 0,
                    "total": 0,
                }
            if tx["fromCurrency"] == 'EUR':
                # This is a buy transaction
                fifo.add_purchase(tx["cryptoAmount"], (tx["eurAmount"] / tx["cryptoAmount"]))
            elif tx["toCurrency"] == 'EUR':
                # This is a sell transaction
                sold_crypto = {
                    "amount": tx["cryptoAmount"],
                    "total_revenue": tx["eurAmount"]
                }
                try:
                    cost_basis = fifo.calculate_cogs(sold_crypto["amount"])
                    profit_loss = sold_crypto["total_revenue"] - cost_basis
                    print(profit_loss)
                    print("fifo", fifo)
                except ValueError as e:
                    print(f"Error calculating COGS for {currency} in year {tx_year}: {e}")
                    continue
                # print("Sold", tx["cryptoAmount"], "for", profit_loss)
                if profit_loss > 0:
                    data["years"][tx_year]["wins"] += profit_loss
                else:
                    data["years"][tx_year]["losses"] += abs(profit_loss)
                data["years"][tx_year]["total"] += round(profit_loss, 2)
                # Reset owned crypto after selling
                # owned_crypto = {"amount": owned_crypto["amount"] - tx["cryptoAmount"], "total_cost": owned_crypto["total_cost"]}
                # if owned_crypto["amount"] < 0:
                #     print(f"Warning: Owned crypto amount is negative for {currency} in year {tx_year}. Removing crypto from results.")
                #     # If owned crypto amount is negative, we should remove it from results
                #     to_be_removed.append(currency)
        


    # Remove currencies with negative owned crypto amount
    for currency in to_be_removed:
        if currency in results:
            del results[currency]

    print(results)
    return results
