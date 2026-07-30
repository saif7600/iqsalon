# ADR 0023: Phase 07 Multichannel and Enterprise Platform Boundaries

Status: Accepted

Date: 2026-07-30

## Context

AtiqSalon must add customer and staff mobile experiences, home-service
operations, enterprise and franchise controls, developer integrations,
white-label behavior, and managed data movement without replacing its modular
monolith or weakening tenant, branch, financial, inventory, booking, consent,
AI, and audit controls.

This umbrella ADR records the Phase 07 decisions. Each numbered decision is an
independent architectural commitment and may be superseded by a later focused
ADR.

## Decisions

### 1. Customer and staff application separation

Create `apps/customer` and `apps/staff` as separate Next.js applications.
They may share packages and API contracts but have independent authentication
entry points, manifests, service workers, navigation, branding contexts, and
deployment artifacts. Neither application is a responsive copy of the operator
portal.

### 2. PWA caching and offline strategy

Cache versioned application shells and explicitly safe reads only. Never cache
tokens, payment details, full invoices, sensitive profiles, or unrestricted
customer data. Offline writes use an encrypted-at-rest browser draft where
available, a generated idempotency key, visible pending state, server
revalidation, and conflict-preserving failure.

### 3. Mobile session and device model

Authentication sessions and registered devices are separate records. Device
trust never bypasses authentication. Access sessions remain short-lived;
refresh sessions and devices are independently revocable. Push tokens are
encrypted and are never returned after registration.

### 4. Home-service address protection

Store normalized operational addresses with tenant and organization ownership.
Expose precise addresses only to assigned operational staff during the required
service window. Use minimized projections, audit sensitive access, and exclude
coordinates and full address lines from analytics and general reporting.

### 5. Travel-time provider abstraction

Use a deterministic local provider based on configured zone and preparation
buffers. Availability includes outbound, service, and return occupancy. Future
map providers implement the same interface; no live-traffic claim is allowed
without configured credentials and provider evidence.

### 6. Enterprise hierarchy storage

Use tenant-owned adjacency-list units with a materialized path for bounded
descendant queries. Validate moves transactionally, reject cycles, and retain
explicit branch assignments. Hierarchy access supplements existing
organization and branch checks and cannot broaden tenant scope.

### 7. Central catalogue inheritance

Central catalogue items publish immutable versions. Branches inherit the
current approved version unless an explicitly allowed field override is
approved. Effective configuration is computed from central version plus active
override and records both provenance values.

### 8. Franchise data isolation

Franchise profiles attach to explicit hierarchy units and legal organizations.
Franchisor users receive only contracted aggregate and compliance projections.
Franchisees cannot query sibling franchisees. Tenant filters remain mandatory
even when a franchisor owns multiple entities.

### 9. Public API authentication

Machine clients use one-time-displayed secrets that are stored only as hashes.
OAuth client credentials issue short-lived tokens containing tenant,
organization, environment, scopes, and client identity. Sandbox and production
credentials are isolated. User cookies are not public API credentials.

### 10. API versioning

Public endpoints use `/api/public/v1`. Additive changes are permitted within a
version; breaking request, response, authorization, or semantic changes require
a new major route and a documented deprecation window.

### 11. Webhook signatures and retries

Each delivery signs timestamp plus raw body with HMAC-SHA256. Receivers must be
able to enforce timestamp tolerance and event-id replay protection. Delivery
uses bounded exponential retry, a terminal dead-letter state, and audited
manual redelivery. Secrets are encrypted and displayed only at creation or
rotation.

### 12. Transactional outbox

Domain transactions append integration events in the same PostgreSQL
transaction. A background publisher claims rows with concurrency-safe leases,
publishes idempotently, records attempts, and moves exhausted events to a dead
letter state. External failure never rolls back an already committed internal
transaction.

### 13. Integration credential storage

Persist provider, status, scopes, expiry metadata, and a secret reference.
Encrypt local development credentials through the platform protector; use an
external secret manager reference in production. Never log or return raw
credentials.

### 14. White-label token restrictions

Branding is a constrained token set, not arbitrary CSS or script injection.
Validate color contrast, dimensions, file types, and safe URLs. Security,
consent, payment, and legal messages retain platform-controlled semantics and
accessible presentation.

### 15. Custom-domain tenant resolution

Resolve normalized host names through a globally unique verified-domain table.
Unknown, disabled, and unverified hosts fail closed before authentication.
Verification tokens are random and hashed. Certificate automation remains a
deployment concern and is not claimed by application-level verification.

### 16. Import validation architecture

Imports upload to quarantined object storage, pass a malware-scanning
interface, parse into staging rows, validate headers and every row, and produce
a preview plus error report before execution. Execution is idempotent and
transactional by bounded batch; source files expire by retention policy.

### 17. Support-access controls

Support access requires a tenant request or explicit tenant approval, a reason,
bounded permissions, start and expiry times, and a second authorized approver.
Activation produces a separate auditable context. Access expires
automatically, cannot grant financial-secret access, and never changes the
support user's permanent roles.

### 18. Entitlement enforcement

Entitlements are evaluated server-side using tenant, organization, feature,
limits, and effective dates. Frontends may hide unavailable features but are
not enforcement points. Entitlements never replace permissions, branch scope,
subscription status, or domain invariants.

## Consequences

- Two additional deployable frontend applications will be introduced.
- The modular monolith gains bounded domain modules rather than a generalized
  enterprise service.
- PostgreSQL remains the source of transactional truth.
- Background work is required for outbox, push, webhook, import, and export
  processing.
- Object storage is required for consented evidence and import/export files.
- Production push, maps, accounting, messaging, identity, and certificate
  automation remain unavailable until real providers and credentials are
  configured and verified.

