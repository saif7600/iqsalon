# ADR 0021: AI and domain-service separation

## Status

Accepted.

## Decision

AI may interpret, summarize, recommend, draft, and propose registered tools.
Transactional truth remains in existing domain services. Models receive no raw
database connection and cannot directly post sales, change payroll, adjust
stock, approve purchases, alter permissions, or issue refunds.
