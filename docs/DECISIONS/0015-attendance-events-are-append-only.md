# ADR 0015: Attendance events are append-only

## Status

Accepted.

## Decision

Clock and break facts are immutable events. Corrections append an event that references the superseded event. No update or delete API is exposed.

## Consequences

The history remains auditable. Attendance summaries are rebuildable projections.
