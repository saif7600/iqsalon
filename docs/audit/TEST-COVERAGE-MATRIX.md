# Test Coverage Matrix

| Capability | Unit | Integration | Auth/Tenant | Browser | Status |
|---|---|---|---|---|---|
| Domain rules | Yes | Limited | N/A | No | PARTIAL |
| Tenancy | EF tests | Limited | Some branch/role tests | No | PARTIAL |
| Authentication | Limited | Not fully evidenced | Limited | Manual login only | PARTIAL |
| CRUD forms | Sparse | Sparse | Sparse | Missing | BROKEN coverage |
| Booking lifecycle | Rule tests | Incomplete | Incomplete | Missing full flow | BROKEN coverage |
| Commercial/inventory/workforce | Rule-heavy | Limited | Limited | Minimal | PARTIAL |
| Portal | 2 tests | No | No | E2E files not run in root test | POOR |
| Customer/staff/web | 1 shallow test each | No | No | No | POOR |

pnpm test passing must not be interpreted as product readiness.# Audit basis

Audit date: 2026-07-30. Repository: `D:\Atiq Softwares june 2026\atiqsalon`. No product code was changed and nothing was deployed during this audit. Evidence combines source inspection, static gates, existing live browser/API checks from this session, and production observations. Clean-checkout proof is impossible because `main` has no commits and every repository file is untracked.
