---
name: stock-invariant-auditor
description: Use this skill when auditing, testing, or verifying retail stock invariants, atomic transactions, idempotency guards, and non-negative stock safety for MihiraStores.
---

# Stock Invariant Auditor Skill

Checklist:
1. Attempt dispatch with insufficient stock.
2. Confirm stock and status remain unchanged after failure.
3. Dispatch a valid transfer and check source stock.
4. Dispatch the same transfer again and verify no second deduction.
5. Receive a dispatched transfer and verify destination stock.
6. Receive the same transfer again and verify no second addition.
7. Try receiving a Requested transfer.
8. Try cancelling a Dispatched transfer.
9. Try a transfer with the same source and destination.
10. Try an empty transfer and zero/negative quantities.
11. For a multi-line transfer, make one line invalid and verify all stock rows remain unchanged.
12. Review the concurrency strategy and test it where practical.
13. Run dotnet build and dotnet test.
14. Report failures and unresolved risks honestly.