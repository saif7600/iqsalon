#!/usr/bin/env bash
set -euo pipefail

target=${1:?Usage: atiqsalon-rollback <release-directory>}
root=/var/www/iqsalon.atiqsoft.com
target=$(readlink -f "$target")

case "$target" in
  "$root"/releases/*) ;;
  *) echo "Target must be an AtiqSalon release." >&2; exit 2 ;;
esac

test -f "$target/api/AtiqSalon.Api.dll"
test -f "$target/portal/apps/portal/server.js"
previous=$(readlink -f "$root/current")
ln -sfn "$target" "$root/current"
systemctl restart atiqsalon-api.service atiqsalon-portal.service

for _ in $(seq 1 30); do
  if curl --fail --silent http://127.0.0.1:5099/api/v1/health |
      grep --quiet '"status":"Healthy"' &&
     curl --fail --silent http://127.0.0.1:3042/login >/dev/null; then
    echo "Rolled back to $target"
    exit 0
  fi
  sleep 2
done

ln -sfn "$previous" "$root/current"
systemctl restart atiqsalon-api.service atiqsalon-portal.service
echo "Rollback target failed health checks; restored $previous" >&2
exit 1

