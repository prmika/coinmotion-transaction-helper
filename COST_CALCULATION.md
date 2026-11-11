# Cost calculation contract

The report service uses FIFO lots for each cryptocurrency.

- EUR purchase fees are added to the fiat acquisition cost of the purchased lot.
- EUR selling fees are allocated across consumed lots in proportion to their gross EUR proceeds and reduce each lot's net profit or loss.
- Fees in currencies other than EUR are retained in the transaction data but are not converted or included in the calculation.
- The selected cost basis for a lot is the larger of its FIFO acquisition cost and the applicable deemed cost (20% of proceeds for holdings under 3,650 days; 40% at or above 3,650 days).
- Monetary lot outputs and year aggregates are rounded to two decimals using midpoint-away-from-zero rounding.
- Crypto amounts, fiat amounts, rates, and fees must be finite and non-negative. Invalid values are rejected before processing.

This is an implementation contract, not tax advice. The deemed-cost policy and treatment of non-EUR fees should be reviewed against applicable tax guidance before production use.
