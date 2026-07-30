# Phase 07 Baseline

Date: 2026-07-30

## Verdict

Phase 07 is not implemented. The repository has a working modular-monolith API,
an authenticated internal portal, a public website, and shared TypeScript
packages. Customer and staff mobile applications, PWA support, home-service
operations, enterprise hierarchy, franchise controls, developer integrations,
white-label routing, and managed data movement are missing.

This document records the state before Phase 07 product changes.

## Existing applications

| Area | Classification | Evidence |
| --- | --- | --- |
| `apps/api` | REAL | .NET 10 API, PostgreSQL, EF Core, tenant filters, permissions, tests |
| `apps/portal` | REAL | Next.js 16 authenticated operating portal |
| `apps/web` | PARTIAL | Public site and public booking entry routes |
| `apps/customer` | MISSING | No independent customer application |
| `apps/staff` | MISSING | No independent staff application |

## Existing platform controls

- Cookie-delivered JWT access sessions and hashed refresh sessions exist.
- Tenant query filters and branch-scoped authorization exist.
- Permission policies are default-deny at protected endpoints.
- PostgreSQL is the transactional store.
- Redis, MinIO, Mailpit, and PostgreSQL are declared for local development.
- Serilog request logging, problem details, health checks, and audit records
  exist.
- Booking, commercial, inventory, workforce, growth, and governed AI modules
  have domain services and tests.
- Twenty-one EF migrations are present through
  `20260730083212_AiGovernanceFoundation`.

## Phase 07 capability truth

| Capability | Classification |
| --- | --- |
| Customer mobile experience | MISSING |
| Staff mobile experience | MISSING |
| PWA manifests and service workers | MISSING |
| Registered devices and mobile session controls | MISSING |
| Push provider abstraction | MISSING |
| Home-service settings, zones, dispatch, travel, safety | MISSING |
| Enterprise hierarchy and regional access | MISSING |
| Franchise profiles, royalty foundation, compliance | MISSING |
| Central catalogue and purchasing policy | MISSING |
| Public API clients, scopes, usage metering | MISSING |
| Webhooks and transactional integration outbox | MISSING |
| Managed integration connections | MISSING |
| White-label branding and custom domains | MISSING |
| Feature entitlements | MISSING |
| Temporary approved support access | MISSING |
| Validated imports, expiring exports, bulk operations | MISSING |

## Baseline quality gates

| Command | Result |
| --- | --- |
| `pnpm install --frozen-lockfile` | PASS |
| `pnpm format:check` | FAIL |
| `pnpm lint` | PASS |
| `pnpm typecheck` | PASS |
| `pnpm test` | PASS: 3 frontend and 60 backend tests |
| `pnpm build` | PASS |
| `dotnet restore apps/api/AtiqSalon.slnx` | PASS |
| `dotnet format apps/api/AtiqSalon.slnx --verify-no-changes --no-restore` | PASS |
| `dotnet build apps/api/AtiqSalon.slnx --no-restore` | PASS |
| `dotnet test apps/api/AtiqSalon.slnx --no-restore` | PASS: 60 tests |
| `dotnet ef migrations list --project apps/api/src/AtiqSalon.Api --no-build` | PARTIAL |
| `docker compose config` | PASS |

## Pre-existing failures and limitations

1. Prettier reports 11 source files and generated deployment artifacts as
   unformatted.
2. The local EF migration command cannot authenticate to the PostgreSQL
   connection configured for `localhost:5432`.
3. Docker Compose publishes PostgreSQL on host port `5433`; migration applied
   state was therefore not confirmed by the baseline command.
4. Shared TypeScript packages contain no executable tests.
5. Existing frontend tests are minimal smoke tests, not workflow evidence.
6. No CI evidence currently exercises browser, migration, tenant-isolation, or
   security workflows end to end.

## Implementation constraints

- New mobile channels must call the existing domain services and must not
  duplicate booking, financial, inventory, or workforce rules.
- Customer identity must be distinct from staff and operator identity.
- Staff data access must be assignment and branch scoped.
- Address data must not enter analytics or broad customer projections.
- External publication must use an outbox and must not participate in internal
  authorization.
- Enterprise hierarchy must supplement, not bypass, tenant and organization
  boundaries.
- Phase 07 migrations must be generated only after domain constraints and
  indexes are defined.

