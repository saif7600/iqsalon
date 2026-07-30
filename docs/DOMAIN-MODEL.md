# Domain model

`Tenant` is the SaaS isolation boundary. `Organization` represents the subscribed business and `Branch` an operating location. `User`, `RefreshSession`, `Organization`, `Branch`, and `AuditEvent` carry `TenantId`.

Organizations own branches. Users receive roles whose permissions are emitted as authenticated claims and enforced by API policies. Audit events are append-oriented security and business evidence; secrets, credentials and tokens are excluded.
