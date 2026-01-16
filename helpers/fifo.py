from collections import deque
from dataclasses import dataclass
from datetime import datetime, timedelta

from config import EPSILON


@dataclass
class Lot:
    quantity: float
    price: float
    time: datetime


class FIFO:
    def __init__(self):
        self.queue = deque()

    def add_purchase(self, quantity, price, time):
        """Adds a purchase to the FIFO queue."""
        if quantity <= 0:
            raise ValueError("Purchase quantity must be positive")
        if price < 0:
            raise ValueError("Purchase price cannot be negative")
        if not isinstance(time, datetime):
            raise ValueError("Purchase time must be a datetime")
        self.queue.append(Lot(quantity, price, time))

    def calculate_cogs(self, quantity_sold, sold_time, total_revenue):
        """Calculates cost of goods sold and acquisition cost assumption using FIFO."""
        if quantity_sold <= 0:
            raise ValueError("Sell quantity must be positive")
        if not isinstance(sold_time, datetime):
            raise ValueError("Sell time must be a datetime")
        if total_revenue < 0:
            raise ValueError("Total revenue cannot be negative")

        cogs = 0.0
        assumed_cost = 0.0
        price_per_unit = total_revenue / quantity_sold if quantity_sold else 0.0
        remaining_to_sell = quantity_sold

        while remaining_to_sell > EPSILON:
            if not self.queue:
                if remaining_to_sell <= EPSILON:
                    break
                raise ValueError("Not enough inventory to sell")

            lot = self.queue.popleft()
            sell_qty = min(lot.quantity, remaining_to_sell)
            lot_held_long = (sold_time - lot.time) >= timedelta(days=3650)
            assumed_rate = 0.4 if lot_held_long else 0.2
            proceeds_portion = sell_qty * price_per_unit

            if lot.quantity <= remaining_to_sell + EPSILON:
                cogs += lot.quantity * lot.price
                assumed_cost += proceeds_portion * assumed_rate
                remaining_to_sell -= lot.quantity
            else:
                cogs += remaining_to_sell * lot.price
                assumed_cost += proceeds_portion * assumed_rate
                self.queue.appendleft(Lot(lot.quantity - remaining_to_sell, lot.price, lot.time))
                remaining_to_sell = 0.0

        return cogs, assumed_cost

    def remaining_quantity(self):
        return sum(lot.quantity for lot in self.queue)