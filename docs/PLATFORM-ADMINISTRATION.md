# Platform Administration

`/platform` is the isolated AtiqSalon SaaS control plane. It is not part of a salon tenant's operating workspace.

## Implemented foundation

- Cross-tenant overview and tenant directory
- Persisted SaaS plans and versioned plan prices
- Persisted tenant subscriptions and billing accounts
- Platform-specific permissions and operational roles
- Audited plan creation and subscription activation

## Authorization

Platform context is issued only to roles whose names begin with `Platform`. Organization roles do not receive platform permissions.

Supported roles are `PlatformOwner`, `PlatformAdministrator`, `PlatformBillingManager`, `PlatformSupportAgent`, and `PlatformReadOnlyAuditor`.

## Not implemented

Payment-provider integration, invoices, usage metering, feature entitlements, automated dunning, tenant impersonation, and provisioning workflows are not operational.
