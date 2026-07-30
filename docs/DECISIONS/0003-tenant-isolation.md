# ADR 0003: Tenant isolation

Accepted. Tenant identity is derived from validated identity claims and enforced through EF Core global query filters plus tests. Client-provided tenant identifiers never establish access.
