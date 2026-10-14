---
trigger: always_on
---

# Rule: Business Invariants & Transaction Safety

This rule outlines the non-negotiable inventory constraints for the MihiraStores domain.

## 1. Stock Invariants
- Under no circumstances may a stock record have `Quantity < 0`.
- All items in a transfer request must be validated for stock availability before any stock is deducted. If any single item lacks sufficient stock, the entire dispatch operation must abort and throw an exception.
- Re-issuing a state transition request (dispatch, receive, cancel) must not apply duplicate increments or decrements. Check status prior to executing adjustments.

## 2. Transfer State Lifecycle
- Valid transitions:
  - `Requested` ➔ `Dispatched`
  - `Dispatched` ➔ `Received`
  - `Requested` ➔ `Cancelled`
- Invalid transitions:
  - `Dispatched` ➔ `Cancelled` (Forbidden)
  - `Received` ➔ `Dispatched` (Forbidden)
  - `Cancelled` ➔ `Dispatched` (Forbidden)


