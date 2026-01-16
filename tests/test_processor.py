from processor import create_tax_report


def test_create_tax_report_fifo_per_currency():
    objects = [
        {
            "time": "2024-01-01T10:00:00+02:00",
            "type": "buy",
            "cryptoAmount": 1.0,
            "rate": 10000.0,
            "eurAmount": 10000.0,
            "source": "Coinmotion",
            "fromCurrency": "EUR",
            "toCurrency": "BTC",
            "fee": 0.0,
            "feeCurrency": "EUR",
        },
        {
            "time": "2024-02-01T10:00:00+02:00",
            "type": "sell",
            "cryptoAmount": 0.4,
            "rate": 15000.0,
            "eurAmount": 6000.0,
            "source": "Coinmotion",
            "fromCurrency": "BTC",
            "toCurrency": "EUR",
            "fee": 0.0,
            "feeCurrency": "EUR",
        },
        {
            "time": "2024-01-10T10:00:00+02:00",
            "type": "buy",
            "cryptoAmount": 2.0,
            "rate": 1000.0,
            "eurAmount": 2000.0,
            "source": "Coinmotion",
            "fromCurrency": "EUR",
            "toCurrency": "ETH",
            "fee": 0.0,
            "feeCurrency": "EUR",
        },
        {
            "time": "2024-03-01T10:00:00+02:00",
            "type": "sell",
            "cryptoAmount": 1.0,
            "rate": 1500.0,
            "eurAmount": 1500.0,
            "source": "Coinmotion",
            "fromCurrency": "ETH",
            "toCurrency": "EUR",
            "fee": 0.0,
            "feeCurrency": "EUR",
        },
    ]

    results = create_tax_report(objects)

    btc_2024 = results["BTC"]["years"]["2024"]
    eth_2024 = results["ETH"]["years"]["2024"]

    assert btc_2024["wins"] == 2000.0
    assert btc_2024["losses"] == 0
    assert btc_2024["total"] == 2000.0

    assert eth_2024["wins"] == 500.0
    assert eth_2024["losses"] == 0
    assert eth_2024["total"] == 500.0

    sell_btc = results["BTC"]["transactions"][1]
    sell_eth = results["ETH"]["transactions"][1]

    assert sell_btc["remainingQuantity"] == 0.6
    assert sell_eth["remainingQuantity"] == 1.0
    assert sell_btc["costBasisMethod"] == "fifo"
    assert sell_eth["costBasisMethod"] == "fifo"


def test_create_tax_report_allows_small_rounding_in_fifo():
    objects = [
        {
            "time": "2024-01-01T10:00:00+02:00",
            "type": "buy",
            "cryptoAmount": 0.27622000,
            "rate": 1.0,
            "eurAmount": 0.27622000,
            "source": "Coinmotion",
            "fromCurrency": "EUR",
            "toCurrency": "XRP",
            "fee": 0.0,
            "feeCurrency": "EUR",
        },
        {
            "time": "2024-01-02T10:00:00+02:00",
            "type": "sell",
            "cryptoAmount": 0.27622000000001,
            "rate": 1.0,
            "eurAmount": 0.27622000000001,
            "source": "Coinmotion",
            "fromCurrency": "XRP",
            "toCurrency": "EUR",
            "fee": 0.0,
            "feeCurrency": "EUR",
        },
    ]

    results = create_tax_report(objects)

    assert "XRP" in results


def test_create_tax_report_handles_eur_to_crypto_buy():
    objects = [
        {
            "time": "2024-01-01T10:00:00+02:00",
            "type": "buy",
            "cryptoAmount": 1.0,
            "rate": 10000.0,
            "eurAmount": 10000.0,
            "source": "Coinmotion",
            "fromCurrency": "EUR",
            "toCurrency": "XRP",
            "fee": 0.0,
            "feeCurrency": "EUR",
        }
    ]

    results = create_tax_report(objects)

    assert "XRP" in results
    assert results["XRP"]["transactions"][0]["cryptoAmount"] == 1.0
