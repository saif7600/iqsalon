# Testing

Frontend packages use Vitest for route/content units; the next increment adds Testing Library, axe and Playwright browser coverage once API lifecycle fixtures are available. The API test project includes permission and EF Core tenant-filter tests.

Run `pnpm test` and `dotnet test apps/api/AtiqSalon.slnx`. Browser acceptance must cover registration, login, protected-route denial, onboarding, Arabic RTL and cross-tenant API denial before production readiness.
