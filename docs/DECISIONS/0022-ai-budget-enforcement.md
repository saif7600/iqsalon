# ADR 0022: AI budget enforcement

## Status

Accepted.

## Decision

Admission checks tenant status, daily request limits, per-user limits, monthly
tokens, and estimated monthly cost before provider invocation. Usage is an
append-only idempotent ledger. Budget rejection leaves deterministic workflows
available.
