#!/usr/bin/env bash
set -euo pipefail

backup_root=/var/backups/atiqsalon
timestamp=$(date -u +%Y%m%dT%H%M%SZ)
install -d -m 700 -o postgres -g postgres "$backup_root"
umask 077

sudo -u postgres pg_dump --port=5433 --format=custom --file="$backup_root/atiqsalon-$timestamp.dump" atiqsalon_prod
find "$backup_root" -type f -name 'atiqsalon-*.dump' -mtime +14 -delete
