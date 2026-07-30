#!/usr/bin/env bash
set -euo pipefail

curl --fail --silent --show-error --max-time 10 \
  https://iqsalon.atiqsoft.com/api/v1/health |
  grep --quiet '"status":"Healthy"'
curl --fail --silent --show-error --max-time 10 \
  https://iqsalon.atiqsoft.com/login >/dev/null

