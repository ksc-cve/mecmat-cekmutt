#!/usr/bin/env bash
# Runs on the server after each rsync (as root): ownership, and the unit/cron files if they changed.
set -euo pipefail
APP=/opt/mecmat-cekmutt
chown -R root:mecmat "$APP"
chmod -R u=rwX,g=rX,o= "$APP"
chmod 750 "$APP/Mecmat.Server" "$APP/backup.sh" "$APP/after-deploy.sh"
if ! cmp -s "$APP/mecmat-cekmutt.service" /etc/systemd/system/mecmat-cekmutt.service; then
  install -m 644 "$APP/mecmat-cekmutt.service" /etc/systemd/system/mecmat-cekmutt.service
  systemctl daemon-reload
fi
if ! cmp -s "$APP/mecmat-cekmutt-backup.cron" /etc/cron.d/mecmat-cekmutt-backup; then
  install -m 644 "$APP/mecmat-cekmutt-backup.cron" /etc/cron.d/mecmat-cekmutt-backup
fi
