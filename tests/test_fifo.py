from datetime import datetime

from helpers.fifo import FIFO
import pytest
from config import EPSILON


def test_fifo_acquisition_cost_assumption():
    fifo = FIFO()
    fifo.add_purchase(1.0, 1, _ts("2022-01-01T10:00:00+02:00"))
    fifo.add_purchase(1.0, 4, _ts("2023-01-01T10:00:00+02:00"))

    cogs, assumed_cost, consumed = fifo.calculate_cogs(
        2.0,
        _ts("2024-02-01T10:00:00+02:00"),
        20,
    )

    assert pytest.approx(cogs, rel=EPSILON) == 5.0
    assert pytest.approx(assumed_cost, rel=EPSILON) == 4.0 # 0.4 * 20 * 2.0
    assert len(consumed) == 2
    assert len(fifo.queue) == 0

    fifo.add_purchase(1.0, 1, _ts("2010-06-01T10:00:00+02:00"))
    fifo.add_purchase(1.0, 4, _ts("2024-01-01T10:00:00+02:00"))

    cogs, assumed_cost, consumed = fifo.calculate_cogs(
        2.0,
        _ts("2024-12-01T10:00:00+02:00"),
        20,
    )

    assert pytest.approx(cogs, rel=EPSILON) == 5.0
    assert pytest.approx(assumed_cost, rel=EPSILON) == 6.0 # 0.4 * 10 + 0.2 * 10
    assert len(consumed) == 2
    assert len(fifo.queue) == 0


def test_fifo_partial_sell():
    fifo = FIFO()
    fifo.add_purchase(1.0, 10000.0, _ts("2024-01-01T10:00:00+02:00"))
    fifo.add_purchase(0.5, 12000.0, _ts("2024-01-10T10:00:00+02:00"))

    cogs, assumed_cost, consumed = fifo.calculate_cogs(
        1.2,
        _ts("2024-02-01T10:00:00+02:00"),
        1.2 * 15000.0,
    )

    assert pytest.approx(cogs, rel=EPSILON) == 10000.0 + (0.2 * 12000.0)
    assert pytest.approx(assumed_cost, rel=EPSILON) == (1.2 * 15000.0) * 0.2
    assert len(consumed) == 2
    assert pytest.approx(fifo.queue[0].quantity, rel=EPSILON) == 0.3
    assert fifo.queue[0].price == 12000.0


def test_fifo_insufficient_inventory():
    fifo = FIFO()
    fifo.add_purchase(0.1, 10000.0, _ts("2024-01-01T10:00:00+02:00"))

    with pytest.raises(ValueError):
        fifo.calculate_cogs(0.2, _ts("2024-02-01T10:00:00+02:00"), 0.2 * 15000.0)


def test_fifo_rejects_invalid_inputs():
    fifo = FIFO()

    with pytest.raises(ValueError):
        fifo.add_purchase(0, 10000.0, _ts("2024-01-01T10:00:00+02:00"))

    with pytest.raises(ValueError):
        fifo.add_purchase(-1, 10000.0, _ts("2024-01-01T10:00:00+02:00"))

    with pytest.raises(ValueError):
        fifo.add_purchase(1, -10.0, _ts("2024-01-01T10:00:00+02:00"))

    with pytest.raises(ValueError):
        fifo.calculate_cogs(0, _ts("2024-02-01T10:00:00+02:00"), 0.0)

    with pytest.raises(ValueError):
        fifo.calculate_cogs(-0.5, _ts("2024-02-01T10:00:00+02:00"), 0.0)


def _ts(value):
    return datetime.strptime(value, "%Y-%m-%dT%H:%M:%S%z")
