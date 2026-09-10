#!/usr/bin/env bash
# Install shared security headers on the four AtiqSoft brand HTTPS vhosts.
# Safe to re-run: skips files that already include the snippet.
set -euo pipefail

SNIPPET_SRC="${1:-/tmp/atiq-security-headers.conf}"
SNIPPET_DST=/etc/nginx/snippets/atiq-security-headers.conf
INCLUDE_LINE='include /etc/nginx/snippets/atiq-security-headers.conf;'

SITES=(
  /etc/nginx/sites-available/iqsalon.atiqsoft.com
  /etc/nginx/sites-available/app.iqsalon.atiqsoft.com
  /etc/nginx/sites-available/iqkitchen.atiqsoft.com.conf
  /etc/nginx/sites-available/app.iqkitchen.atiqsoft.com.conf
  /etc/nginx/sites-available/iqretail.atiqsoft.com.conf
  /etc/nginx/sites-available/app.iqretail.atiqsoft.com.conf
  /etc/nginx/sites-available/iqgarage.atiqsoft.com
  /etc/nginx/sites-available/app.iqgarage.atiqsoft.com
)

install -d /etc/nginx/snippets
cp "$SNIPPET_SRC" "$SNIPPET_DST"
chmod 644 "$SNIPPET_DST"

python3 - <<'PY'
from pathlib import Path

include_line = "    include /etc/nginx/snippets/atiq-security-headers.conf;\n"
sites = [
  "/etc/nginx/sites-available/iqsalon.atiqsoft.com",
  "/etc/nginx/sites-available/app.iqsalon.atiqsoft.com",
  "/etc/nginx/sites-available/iqkitchen.atiqsoft.com.conf",
  "/etc/nginx/sites-available/app.iqkitchen.atiqsoft.com.conf",
  "/etc/nginx/sites-available/iqretail.atiqsoft.com.conf",
  "/etc/nginx/sites-available/app.iqretail.atiqsoft.com.conf",
  "/etc/nginx/sites-available/iqgarage.atiqsoft.com",
  "/etc/nginx/sites-available/app.iqgarage.atiqsoft.com",
]

for path in sites:
    p = Path(path)
    if not p.exists():
        print(f"skip missing {path}")
        continue
    text = p.read_text()
    if "atiq-security-headers.conf" in text:
        print(f"ok already {path}")
        continue

    out = []
    i = 0
    lines = text.splitlines(keepends=True)
    while i < len(lines):
        line = lines[i]
        out.append(line)
        # Insert once per SSL server block (listen 443), after client_max_body_size when present,
        # otherwise after the opening server_name line of that block.
        if "listen" in line and "443" in line:
            # Walk back: if this block already got the include, skip.
            block_start = len(out) - 1
            while block_start > 0 and not out[block_start].lstrip().startswith("server {"):
                block_start -= 1
            block_so_far = "".join(out[block_start:])
            if "atiq-security-headers.conf" not in block_so_far:
                # Insert just before the listen 443 line we just appended
                out.insert(-1, include_line)
                print(f"patched {path}")
        i += 1
    p.write_text("".join(out))

print("done")
PY

nginx -t
systemctl reload nginx
echo "nginx reloaded with security headers"
