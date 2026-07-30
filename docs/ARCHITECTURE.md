# Architecture

AtiqSalon AI is a pnpm/Turborepo monorepo with two Next.js applications and an ASP.NET Core modular monolith. Public presentation and authenticated operations remain independently deployable while sharing accessible primitives and contracts. The API groups future modules by vertical business boundary without introducing network boundaries.

Foundational modules are Identity, Tenancy, Organizations, Branches and Audit. Future module boundaries are Staff, Customers, Services, Bookings, PointOfSale, Inventory, Memberships, Marketing, Finance, Notifications, ArtificialIntelligence, Reporting and Subscriptions. They are not implemented or exposed as working features.

Requests flow from browser to versioned API contracts, through authorization and tenant context, into EF Core data access. PostgreSQL is authoritative; Redis and object storage cannot establish identity or tenancy.
