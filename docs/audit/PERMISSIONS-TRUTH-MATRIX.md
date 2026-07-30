# Permissions Truth Matrix

| Feature | Permission | UI | API | Result |
|---|---|---|---|---|
| Calendar | ppointments.read | Hidden by permission | Policy required | PARTIAL evidence |
| Booking create | ppointments.create | Navigation/form | Policy required | PARTIAL |
| POS | pos.access | Hidden | Policy required | PARTIAL |
| Sensitive notes | customers.notes.sensitive.read | Role-derived | Catalog test | PARTIAL |
| Platform admin | platform.dashboard.read | Hidden | Policy required | Live owner verified |
| Platform mutation | platform manage permissions | No complete UI | Policy required | PARTIAL |

Role expansion exists for platform and organization roles. Missing: browser/API hostile tests for every role, branch, foreign identifier and mutation. UI hiding is not counted as enforcement.# Audit basis

Audit date: 2026-07-30. Repository: `D:\Atiq Softwares june 2026\atiqsalon`. No product code was changed and nothing was deployed during this audit. Evidence combines source inspection, static gates, existing live browser/API checks from this session, and production observations. Clean-checkout proof is impossible because `main` has no commits and every repository file is untracked.
