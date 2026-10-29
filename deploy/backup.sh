#!/usr/bin/env bash
# Backs up MECMAT only — never anything under /var/lib/reaction-erp (CLAUDE.md).
# The database is copied with SQLite's online backup API (never `cp`, which can catch a
# half-written WAL). Keys go in too: without field.key the encrypted columns are unreadable.
set -euo pipefail
DATA="${MECMAT_DATA:-/var/lib/mecmat-cekmutt}"
DEST="$DATA/backups"
KEEP_DAYS=30
STAMP="$(date +%Y%m%d-%H%M%S)"
mkdir -p "$DEST"
umask 077
sqlite3 "$DATA/mecmat.db" ".backup '$DEST/mecmat-$STAMP.db'"
# The copy inherits WAL mode; switch it to a single self-contained file before checking it,
# otherwise the check leaves -wal/-shm files beside the archive.
sqlite3 "$DEST/mecmat-$STAMP.db" "PRAGMA journal_mode=DELETE;" >/dev/null
sqlite3 "$DEST/mecmat-$STAMP.db" "PRAGMA integrity_check;" | grep -qx ok || { echo "integrity_check failed on backup" >&2; exit 1; }
rm -f "$DEST/mecmat-$STAMP.db-wal" "$DEST/mecmat-$STAMP.db-shm"
gzip -9 "$DEST/mecmat-$STAMP.db"
tar -C "$DATA" -czf "$DEST/keys-$STAMP.tgz" field.key ip.salt dp-keys 2>/dev/null || true
# Uploads change rarely: only archive when something is newer than the last archive.
LAST="$(ls -t "$DEST"/uploads-*.tgz 2>/dev/null | head -1 || true)"
if [ -d "$DATA/uploads" ] && { [ -z "$LAST" ] || [ -n "$(find "$DATA/uploads" -newer "$LAST" -type f | head -1)" ]; }; then
  tar -C "$DATA" -czf "$DEST/uploads-$STAMP.tgz" uploads
fi
find "$DEST" -maxdepth 1 -type f -mtime +"$KEEP_DAYS" \( -name 'mecmat-*.db.gz' -o -name 'keys-*.tgz' \) -delete
# Keep the newest uploads archive whatever its age; it is the only copy of unchanged files.
ls -t "$DEST"/uploads-*.tgz 2>/dev/null | tail -n +4 | xargs -r rm -f || true
chown -R mecmat:mecmat "$DEST" 2>/dev/null || true
echo "backup ok: $DEST/mecmat-$STAMP.db.gz"
