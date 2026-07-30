# ADR 0019: IQAI is the AtiqSalon AI provider

## Status

Accepted.

## Decision

AtiqSalon calls IQAI through its tenant-bound SDK API from the server. SDK tokens are never sent to browsers. Initial access is advisory and cannot write salon records. Deterministic services remain authoritative for permissions, calculations, approvals, ledgers, and persistence.

## Configuration

- `IQAI_BASE_URL`: IQAI service origin.
- `IQAI_SDK_TOKEN`: tenant-bound SDK token issued by IQAI.

## Consequences

The portal reports IQAI as unavailable when configuration or the upstream service is unavailable. Future AI actions require explicit typed tools, permission checks, confirmation, audit records, and idempotency.
