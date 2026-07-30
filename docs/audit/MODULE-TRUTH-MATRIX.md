# Module Truth Matrix

| Module | Frontend | API/DB | Evidence | Classification |
|---|---|---|---|---|
| Auth/login | Real | Real | Live login verified | PARTIAL |
| Password recovery/email verification | Shell | Incomplete | UI reports unavailable workflows | PLACEHOLDER |
| Tenancy/authorization | Navigation + policies | Query filters/policies | Unit-level evidence only | PARTIAL |
| Organization/branches | Forms/routes | Persistent entities/endpoints | Full reload/deactivation flow not audited | PARTIAL |
| Services/staff/customers/resources | CRUD shells | Persistent endpoints | Live tenant empty; no E2E matrix | PARTIAL |
| Availability/business hours | Weak UI exposure | Domain structures | Full booking prerequisites not proven | PARTIAL |
| Appointments/calendar | Operational shell | Persistent API | No complete conflict/restart workflow | PARTIAL |
| Public booking | Route exists | Public API exists | End-to-end persistence not proven | PARTIAL |
| POS/commercial | Dense operational forms | Broad persistent API | Browser persistence incomplete | PARTIAL |
| Inventory/workforce/growth | Workspaces | Broad entities/endpoints | Mostly rule tests, limited browser proof | PARTIAL |
| IQAI | Chat UI | Grounded tenant counts | Functional but ~56s response | PARTIAL |
| Customer/staff apps | Minimal PWA shells | Limited mobile endpoints | One shallow test each | PLACEHOLDER |
| SaaS admin | Tenant/plan/subscription view | Foundation entities/API | Billing/provider absent | PARTIAL |
| Billing/entitlements/provisioning | Missing UI | Not operational | Explicitly excluded | MISSING |# Audit basis

Audit date: 2026-07-30. Repository: `D:\Atiq Softwares june 2026\atiqsalon`. No product code was changed and nothing was deployed during this audit. Evidence combines source inspection, static gates, existing live browser/API checks from this session, and production observations. Clean-checkout proof is impossible because `main` has no commits and every repository file is untracked.
