# Rectification Plan

| Priority | Finding | Required change | Acceptance |
|---|---|---|---|
| P0 | No reproducible source | Commit intentional source; exclude artifacts/secrets; CI clean checkout | Fresh machine passes documented setup |
| P0 | Tenant isolation not fully proven | Two-tenant hostile API/browser suite | All foreign IDs return non-disclosing denial |
| P0 | Dependency vulnerabilities | Upgrade/override and rerun audit | No high/critical findings |
| P1 | Forms below standard | Shared schema-driven form system; migrate onboarding/core CRUD/booking | Field/server errors, persistence, mobile, RTL, accessibility pass |
| P1 | Core onboarding/booking incomplete | Wizard: organization, branch, hours, service, staff, customer, availability, booking | Create/reload/restart/conflict/reschedule/cancel/audit pass |
| P1 | Auth recovery | Real email verification/reset/revocation | Complete browser and negative tests |
| P2 | Quality gates | Fix formatting and generated-artifact scope | All gates green from clean checkout |
| P2 | Browser coverage | Playwright matrix at required viewports/themes/languages | Critical workflows pass with no console/network errors |
| P2 | Error handling | Preserve structured ProblemDetails/field errors | No generic false credential/provider messages |
| P3 | Advanced modules | Resume only after P0/P1 | Explicit approval after evidence |

Recommended next Codex task: **P0 repository normalization and clean-checkout reproducibility audit**, followed by **P1 shared professional form system specification**, not implementation of more modules.# Audit basis

Audit date: 2026-07-30. Repository: `D:\Atiq Softwares june 2026\atiqsalon`. No product code was changed and nothing was deployed during this audit. Evidence combines source inspection, static gates, existing live browser/API checks from this session, and production observations. Clean-checkout proof is impossible because `main` has no commits and every repository file is untracked.
