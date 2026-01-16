from collections import deque

class FIFO:
    def __init__(self):
        self.queue = deque()

    def add_purchase(self, quantity, price):
        """Adds a purchase to the FIFO queue."""
        self.queue.append((quantity, price))

    def calculate_cogs(self, quantity_sold):
        """Calculates the cost of goods sold using FIFO."""
        cogs = 0
        remaining_to_sell = quantity_sold

        while remaining_to_sell > 0:
            if not self.queue:
                raise ValueError("Not enough inventory to sell")

            (available_quantity, purchase_price) = self.queue.popleft()

            if available_quantity <= remaining_to_sell:
                cogs += available_quantity * purchase_price
                remaining_to_sell -= available_quantity
            else:
                cogs += remaining_to_sell * purchase_price
                self.queue.appendleft((available_quantity - remaining_to_sell, purchase_price))
                remaining_to_sell = 0

        return cogs