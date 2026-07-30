# Security

The foundation uses Argon-compatible ASP.NET password hashing, short-lived signed access tokens in HTTP-only cookies, hashed refresh tokens, revocable sessions, permission policies, tenant query filters, Problem Details, structured request logging and immutable audit-event records.

Production requires managed secrets, TLS, key rotation, email token delivery, refresh rotation endpoints, CSRF defenses for state-changing cookie requests, rate limits, lockout policy, dependency scanning, database backups, restore drills, CSP and reviewed privacy/retention controls. Never log passwords, tokens, private keys, full payment details or sensitive before/after values.
