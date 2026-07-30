# AtiqSalon AI

Operating System for Beauty & Wellness Businesses.

AtiqSalon AI is a greenfield multi-tenant SaaS foundation for salons, barbershops, spas, nail studios, wellness centres, home-service teams, groups and franchises.

## Architecture

- `apps/web`: public Next.js website on port 3000
- `apps/portal`: authenticated Next.js portal on port 3001
- `apps/api`: ASP.NET Core 10 modular monolith on port 5080
- `packages/*`: shared UI, types, validation and API SDK
- PostgreSQL: transactional source of truth
- Redis: cache and future distributed coordination (host port 6380)
- MinIO: local S3-compatible object storage
- Mailpit: local email capture

## Prerequisites

Node.js 22+, pnpm 9.15, .NET SDK 10, Docker Desktop and Git.

## Local setup

```powershell
Copy-Item .env.example .env
pnpm install
pnpm api:restore
pnpm infra:up
$env:DATABASE_URL='Host=localhost;Port=5433;Database=atiqsalon;Username=atiqsalon;Password=change-this-local-password'
$env:JWT_SIGNING_KEY='replace-with-at-least-32-random-characters'
pnpm db:migrate
pnpm dev
```

Run the API separately with `pnpm api:dev`. Open the website at `http://localhost:3000`, portal at `http://localhost:3001`, API health at `http://localhost:5080/api/v1/health`, OpenAPI at `http://localhost:5080/api/v1/openapi/v1.json`, Mailpit at `http://localhost:8025`, and MinIO at `http://localhost:9001`.

The explicit development seed creates local-only users `owner@fictional-pearl.example.test` and `reception@fictional-pearl.example.test` with password `LocalDevelopment!2026`. The receptionist is restricted to the Fictional Marina branch. These accounts are never created outside the Development environment.

## Quality gates

```powershell
pnpm format:check
pnpm lint
pnpm typecheck
pnpm test
pnpm build
dotnet restore apps/api/AtiqSalon.slnx
dotnet build apps/api/AtiqSalon.slnx
dotnet test apps/api/AtiqSalon.slnx
docker compose config
```

No production credentials or operational sample data are seeded. Registration creates explicit development data through the real API. See `docs/LOCAL-DEVELOPMENT.md` for troubleshooting.
