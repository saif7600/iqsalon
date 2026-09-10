#!/usr/bin/env bash
# Add /login and /sign-in parity across brand marketing + app portals.
set -euo pipefail

python3 - <<'PY'
from pathlib import Path

# Marketing apex → app portal login
MARKETING = {
  "/etc/nginx/sites-available/iqsalon.atiqsoft.com": "https://app.iqsalon.atiqsoft.com/login",
  "/etc/nginx/sites-available/iqkitchen.atiqsoft.com.conf": "https://app.iqkitchen.atiqsoft.com/login",
  "/etc/nginx/sites-available/iqretail.atiqsoft.com.conf": "https://app.iqretail.atiqsoft.com/login",
  "/etc/nginx/sites-available/iqgarage.atiqsoft.com": "https://app.iqgarage.atiqsoft.com/login",
}

# App portals: /sign-in → /login (same host)
APPS = [
  "/etc/nginx/sites-available/app.iqsalon.atiqsoft.com",
  "/etc/nginx/sites-available/app.iqkitchen.atiqsoft.com.conf",
  "/etc/nginx/sites-available/app.iqretail.atiqsoft.com.conf",
  "/etc/nginx/sites-available/app.iqgarage.atiqsoft.com",
]

def marketing_block(target: str) -> str:
    return f"""
    location = /login {{
        return 301 {target};
    }}
    location = /login/ {{
        return 301 {target};
    }}
    location = /sign-in {{
        return 301 {target};
    }}
    location = /sign-in/ {{
        return 301 {target};
    }}
"""

APP_BLOCK = """
    location = /sign-in {
        return 301 /login$is_args$args;
    }
    location = /sign-in/ {
        return 301 /login$is_args$args;
    }
"""

def ensure_in_ssl_server(path: Path, block: str, marker: str) -> None:
    if not path.exists():
        print(f"skip missing {path}")
        return
    text = path.read_text()
    if marker in text and "/sign-in" in text and (marker != "login" or "location = /login" in text):
        # For marketing, require both login and sign-in
        if "location = /sign-in" in text and ("location = /login" in text or path.name.startswith("app.")):
            print(f"ok already {path}")
            return

    lines = text.splitlines(keepends=True)
    out = []
    i = 0
    inserted = False
    while i < len(lines):
        line = lines[i]
        # Insert after client_max_body_size inside first SSL-capable server block
        if (not inserted) and "client_max_body_size" in line:
            out.append(line)
            # peek ahead: only insert if this server later listens 443 or is the main block
            out.append(block if not block.startswith("\n") else block)
            # avoid double blank
            inserted = True
            i += 1
            continue
        out.append(line)
        i += 1

    if not inserted:
        print(f"WARN could not patch {path}")
        return

    # Deduplicate if we re-added login blocks for salon
    new_text = "".join(out)
    # If duplicate location = /login appears, keep first occurrence cluster only — simple cleanup:
    if new_text.count("location = /login {") > 1:
        # remove old salon-only login block later duplicates by rewriting cleanly from original once
        pass
    path.write_text(new_text)
    print(f"patched {path}")

for conf, target in MARKETING.items():
    p = Path(conf)
    if not p.exists():
        print(f"skip missing {conf}")
        continue
    text = p.read_text()
    if "location = /sign-in" in text and "location = /login" in text:
        print(f"ok already {conf}")
        continue
    # If salon already has /login only, append /sign-in near it
    if "location = /login" in text and "location = /sign-in" not in text:
        needle = "location = /login/ {\n        return 301 "
        idx = text.find("location = /login/")
        if idx == -1:
            idx = text.find("location = /login {")
        # Find end of login/ block
        end = text.find("}", text.find("location = /login/"))
        if end != -1:
            insert_at = end + 1
            addition = f"""

    location = /sign-in {{
        return 301 {target};
    }}
    location = /sign-in/ {{
        return 301 {target};
    }}
"""
            p.write_text(text[:insert_at] + addition + text[insert_at:])
            print(f"patched sign-in onto existing login {conf}")
            continue
    # Fresh insert after client_max_body_size
    lines = text.splitlines(keepends=True)
    out = []
    done = False
    for line in lines:
        out.append(line)
        if (not done) and "client_max_body_size" in line:
            out.append(marketing_block(target))
            done = True
    if done:
        p.write_text("".join(out))
        print(f"patched {conf}")
    else:
        print(f"WARN {conf}")

for conf in APPS:
    p = Path(conf)
    if not p.exists():
        print(f"skip missing {conf}")
        continue
    text = p.read_text()
    if "location = /sign-in" in text:
        print(f"ok already {conf}")
        continue
    lines = text.splitlines(keepends=True)
    out = []
    done = False
    for line in lines:
        out.append(line)
        if (not done) and "client_max_body_size" in line:
            out.append(APP_BLOCK)
            done = True
    if done:
        p.write_text("".join(out))
        print(f"patched {conf}")
    else:
        print(f"WARN {conf}")

print("done")
PY

nginx -t
systemctl reload nginx
echo "auth url parity applied"
