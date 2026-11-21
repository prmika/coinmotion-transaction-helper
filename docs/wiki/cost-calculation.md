# Cost calculation contract

The report service uses **FIFO lots** per cryptocurrency. See
[`COST_CALCULATION.md`](../../COST_CALCULATION.md) for the authoritative source.

- **Fees (EUR).** Purchase fees are added to the fiat acquisition cost of the purchased lot. Selling fees are allocated across consumed lots in proportion to their gross EUR proceeds and reduce each lot's net profit or loss.
- **Non-EUR fees.** Fees in currencies other than EUR are retained in the transaction data but are **not** converted or included in the calculation.
- **Deemed cost.** The selected cost basis for a lot is the larger of its FIFO acquisition cost and the applicable deemed cost:
  - **20%** of proceeds for holdings under 3,650 days;
  - **40%** at or above 3,650 days.
- **Rounding.** Monetary lot outputs and year aggregates are rounded to two decimals using midpoint-away-from-zero rounding.
- **Validation.** Crypto amounts, fiat amounts, rates, and fees must be finite and non-negative. Invalid values are rejected before processing.
- **Inventory.** If inventory is insufficient, the service fails closed (`InsufficientInventoryException`) rather than producing a partial report.

> This is an implementation contract, not tax advice. The deemed-cost policy and
> treatment of non-EUR fees should be reviewed against applicable tax guidance before
> production use.

## Transaction model

`NormalizedTransaction` always has `FromCurrency` and `ToCurrency`. **EUR is the base
currency:**

- **Buy** — `FromCurrency == "EUR"` → adds to the FIFO queue.
- **Sell** — `ToCurrency == "EUR"` → consumes from the FIFO queue.
- **Transfer** — `account_transfer_in` → synthetic buy with 0 EUR amount.

## FIFO behavior

- Floating-point comparisons use `Constants.Epsilon` (1e-13).
- Holding period **≥ 3650 days** → 40% assumed cost rate, otherwise 20%.
- Throws `InsufficientInventoryException` when inventory is insufficient.
- Returns a `ConsumedLot` list showing which purchase lots were used.
- Cost basis = **higher of** actual FIFO cost vs. assumed cost.
