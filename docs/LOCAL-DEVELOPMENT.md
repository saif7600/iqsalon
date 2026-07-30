# Local development

Copy `.env.example` to `.env`, replace local secrets, run `pnpm install`, `pnpm infra:up`, `pnpm api:restore`, migrations, then `pnpm dev` and `pnpm api:dev`.

PostgreSQL uses port 5432, Redis 6379, MinIO 9000/9001, Mailpit SMTP 1025 and UI 8025. If a port is occupied, stop the conflicting local service or change the published port and matching environment configuration. `docker compose down` preserves named volumes; `docker compose down -v` destroys local data and must only be used intentionally.
