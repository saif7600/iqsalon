# Authorization

Business logic authorizes permissions, not role names. Roles are curated permission bundles: PlatformSuperAdmin, OrganizationOwner, OrganizationAdmin, BranchManager, Receptionist, ServiceProvider, Cashier, InventoryManager, Accountant, MarketingManager and Viewer.

PlatformSuperAdmin has platform permissions only and does not implicitly enter tenant context. API endpoints use explicit policies such as `branch.create`. Changes to role mappings must be audited and regression-tested.
