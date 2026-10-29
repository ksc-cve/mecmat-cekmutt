#!/usr/bin/env bash
# Copies the server's MECMAT backups to the Mac (outside Google Drive), as an off-site copy.
set -euo pipefail
SERVER="${SERVER:-root@187.52.127.97}"
DEST="$HOME/mecmat-data/server-backups"
mkdir -p "$DEST"
rsync -az "$SERVER:/var/lib/mecmat-cekmutt/backups/" "$DEST/"
chmod -R go-rwx "$DEST"
ls -lt "$DEST" | head -5
