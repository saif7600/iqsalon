# Auth URL parity

Portal canonical sign-in path is `/login`. `/sign-in` redirects to `/login` (query string preserved).

## Portal

- Route: `apps/portal/src/app/(auth)/sign-in/page.tsx`
- Permanent redirects in `apps/portal/next.config.ts`

## Nginx (marketing + app)

Install:

```bash
scp deploy/nginx/install-auth-url-parity.sh root@HOST:/tmp/install-auth-url-parity.sh
ssh root@HOST 'bash /tmp/install-auth-url-parity.sh'
```

- Marketing hosts (`iq*.atiqsoft.com`): `/login` and `/sign-in` → `https://app.<brand>.atiqsoft.com/login`
- App hosts (`app.iq*.atiqsoft.com`): `/sign-in` → `/login`
