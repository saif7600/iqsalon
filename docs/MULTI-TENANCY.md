# Multi-tenancy

Tenant identity comes from a validated server-issued access token, never request JSON, query strings or arbitrary headers. `TenantContext` reads authenticated claims and EF Core global filters scope all tenant-owned entities. Platform context is a separate claim and permission set.

Every new tenant-owned entity must implement the tenant entity contract, receive `TenantId` from server context, define a global filter, and have a cross-tenant isolation test. Background jobs, object keys and Redis keys must carry verified tenant scope when introduced.
