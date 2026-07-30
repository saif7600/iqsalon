# Route and API Matrix

Portal exposes 39 page routes and source scan found approximately 175 mapped API operations. Route volume is not completion evidence.

| Route family | API | Result |
|---|---|---|
| /login, /register | /api/v1/auth/* | Login REAL; registration PARTIAL; recovery PLACEHOLDER |
| /settings/* | organizations/branches | PARTIAL |
| /services, /staff, /customers, /resources | corresponding CRUD | PARTIAL; persistence matrix incomplete |
| /calendar, /appointments/* | appointments | PARTIAL |
| /book/* | public booking | NOT TESTED end to end |
| /pos, /commercial/* | sales/payment APIs | PARTIAL |
| /inventory, /workforce, /growth | module APIs | PARTIAL |
| /iqai | /iqai/status, /iqai/chat | PARTIAL, slow |
| /platform | /platform/* | PARTIAL foundation |
| protected catch-all | no guaranteed workflow | PLACEHOLDER risk |# Audit basis

Audit date: 2026-07-30. Repository: `D:\Atiq Softwares june 2026\atiqsalon`. No product code was changed and nothing was deployed during this audit. Evidence combines source inspection, static gates, existing live browser/API checks from this session, and production observations. Clean-checkout proof is impossible because `main` has no commits and every repository file is untracked.
