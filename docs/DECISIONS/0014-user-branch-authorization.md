# ADR 0014: Persisted User Branch Authorization

## Status

Accepted.

## Context

Tenant isolation prevents access across subscribed accounts, but it does not restrict an operational user to assigned branches inside one tenant. Receptionists, branch managers, and service providers must not gain organization-wide access merely because they hold a valid tenant token.

## Decision

`UserBranchAssignment` is the persisted source of branch access. Each active assignment belongs to one tenant, user, organization, and branch. The API projects active branch identifiers into signed `branch_id` JWT claims at login.

`OrganizationOwner` and `OrganizationAdmin` are the only organization-wide operational roles. Other roles fail closed when a requested branch is absent from their signed branch claims. Tenant query filters remain mandatory and operate independently from branch authorization.

Appointment creation, listing, detail, history, lifecycle transitions, availability, and rescheduling enforce branch access server-side. Portal filtering is a usability aid and is not an authorization boundary.

Branch-assignment changes are permission-protected and audited. A new login is required before changed assignments appear in a token.

## Consequences

- Branch authorization remains deterministic and does not require a database query on every request.
- Assignment changes do not affect an already-issued 15-minute access token.
- Organization-wide roles require deliberate assignment because they bypass branch restrictions.
- Future refresh-token rotation must re-project current branch assignments.
- Customer visibility needs a separate policy because customers are organization-level records rather than branch-owned records.
