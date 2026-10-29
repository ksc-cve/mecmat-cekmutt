#!/usr/bin/env bash
# One-time server set-up for MECMAT. Run ON the server as root, after the first deploy/ship.sh
# (which creates the mecmat user and folders and copies the app). Idempotent. Touches nothing of
# Reaction ERP.
set -euo pipefail
APP=/opt/mecmat-cekmutt
DATA=/var/lib/mecmat-cekmutt
id mecmat >/dev/null 2>&1 || useradd --system --no-create-home --home-dir /nonexistent --shell /usr/sbin/nologin mecmat
install -d -o root -g mecmat -m 750 "$APP"
install -d -o mecmat -g mecmat -m 750 "$DATA" "$DATA/backups" "$DATA/uploads"
command -v sqlite3 >/dev/null || apt-get install -y sqlite3
"$APP/after-deploy.sh"
systemctl enable mecmat-cekmutt
systemctl restart mecmat-cekmutt
sleep 4
curl -fsS http://127.0.0.1:5180/api/health && echo
echo
echo "First admin password (shown here once, nowhere else):"
sudo -u mecmat env ASPNETCORE_ENVIRONMENT=Production Mecmat__DataRoot="$DATA" "$APP/Mecmat.Server" reset-admin kasan.cha
echo
echo "Next: add deploy/Caddy-block.txt to /etc/caddy/Caddyfile, then"
echo "  caddy validate --config /etc/caddy/Caddyfile && systemctl reload caddy"
