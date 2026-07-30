# ADR 0020: Provider-neutral AI layer

## Status

Accepted.

## Decision

Domain use cases depend on AtiqSalon AI completion, routing, usage, safety, and
tool contracts. IQAI is the first external adapter. Deterministic simulation is
development-only and must identify itself as simulated.

Provider credentials remain server-side. Model names are selected from the
registry and cannot be supplied arbitrarily by portal users.
