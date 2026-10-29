#!/usr/bin/env bash
# Builds a self-contained linux-x64 release (no .NET runtime needed on the server) into
# ~/dev/mecmat-cekmutt-build/publish, stamped with the git commit so /api/health tells what runs.
set -euo pipefail
cd "$(dirname "$0")/.."
OUT="$HOME/dev/mecmat-cekmutt-build/publish"
SHA="$(git rev-parse --short HEAD)$(git diff --quiet || echo '-dirty')"
rm -rf "$OUT"
tools/dn publish src/Mecmat.Server -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=false -p:InformationalVersion="$SHA" -o "$OUT" --nologo -v quiet
cp deploy/backup.sh deploy/after-deploy.sh deploy/mecmat-cekmutt.service deploy/mecmat-cekmutt-backup.cron "$OUT/"
chmod +x "$OUT/backup.sh" "$OUT/after-deploy.sh"
echo "publish → $OUT ($SHA)"
