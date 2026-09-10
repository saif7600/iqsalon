# Nginx edge configs for AtiqSoft multi-brand portals

## Security headers

Shared snippet: [`snippets/atiq-security-headers.conf`](./snippets/atiq-security-headers.conf)

Sets on HTTPS responses:

- `Strict-Transport-Security`
- `Content-Security-Policy`
- `X-Frame-Options`
- `X-Content-Type-Options`
- `Referrer-Policy`
- `Permissions-Policy`

Install on the production host:

```bash
scp deploy/nginx/snippets/atiq-security-headers.conf root@HOST:/tmp/atiq-security-headers.conf
scp deploy/nginx/install-security-headers.sh root@HOST:/tmp/install-security-headers.sh
ssh root@HOST 'bash /tmp/install-security-headers.sh /tmp/atiq-security-headers.conf'
```

Next.js apps also emit the same headers via `next.config.ts` (`portal`, `web`, `customer`) so local/dev and app hosts stay covered if nginx is skipped.
