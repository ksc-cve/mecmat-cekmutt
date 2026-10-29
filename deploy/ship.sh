#!/usr/bin/env bash
# Sends the current commit to the server. Usage: deploy/ship.sh [--dry-run]
# Own script, own paths, own service — nothing here reads or writes Reaction ERP (CLAUDE.md).
set -euo pipefail
cd "$(dirname "$0")/.."
SERVER="${SERVER:-root@187.52.127.97}"
APP=/opt/mecmat-cekmutt
DATA=/var/lib/mecmat-cekmutt
BUILD_OUT="$HOME/dev/mecmat-cekmutt-build/publish"
DRY=0; [ "${1:-}" = "--dry-run" ] && DRY=1
say() { printf '\n▶ %s\n' "$*"; }
die() { printf '\n✗ %s\n  %s\n' "$1" "${2:-}" >&2; exit 1; }
run() { if [ $DRY = 1 ]; then printf '  (dry-run) %s\n' "$*"; else "$@"; fi; }

say "1/8 ตรวจ git"
[ -z "$(git status --porcelain)" ] || die "มีไฟล์ยังไม่ commit" "commit ก่อนส่งขึ้นเซิร์ฟเวอร์ เพื่อให้รู้ว่าเซิร์ฟเวอร์รันโค้ดอะไร"
git fetch -q origin main && [ "$(git rev-parse HEAD)" = "$(git rev-parse origin/main)" ] || die "HEAD ไม่ตรงกับ origin/main" "push ก่อน (Drive เคยทำ .git หาย)"

say "2/8 migration ที่เสี่ยงตั้งแต่เวอร์ชันบนเซิร์ฟเวอร์"
DEPLOYED="$(ssh -o ConnectTimeout=20 "$SERVER" "cat $APP/DEPLOYED_COMMIT 2>/dev/null" 2>/dev/null | tr -d '[:space:]' || true)"
RISKY=0
if [ -n "$DEPLOYED" ] && git cat-file -e "$DEPLOYED" 2>/dev/null; then
  if git diff "$DEPLOYED" HEAD -- src/Mecmat.Server/Data/Migrations | grep -E '^\+.*(DropTable|DropColumn|RenameColumn|RenameTable|AlterColumn|DropForeignKey|AddForeignKey|\.Sql\()' >/dev/null; then RISKY=1; fi
  echo "  เซิร์ฟเวอร์รัน $DEPLOYED · migration เสี่ยง: $RISKY"
else
  echo "  ยังไม่เคย deploy หรือไม่รู้ commit เดิม → ถือว่าเสี่ยง"; RISKY=1
fi

say "3/8 สำรองข้อมูลก่อน (ถ้ามีฐานข้อมูลแล้ว)"
if ssh -o ConnectTimeout=20 "$SERVER" "test -f $DATA/mecmat.db"; then
  [ $RISKY = 1 ] && run ssh "$SERVER" "$APP/backup.sh" || echo "  ไม่มี migration เสี่ยง — ใช้ backup ตามรอบ"
else echo "  ยังไม่มีฐานข้อมูล (ติดตั้งครั้งแรก)"; fi

say "4/8 build"
run deploy/publish.sh

say "5/8 เตรียมผู้ใช้/โฟลเดอร์ของ MECMAT (ครั้งแรก) และเก็บเวอร์ชันเดิมไว้ย้อนกลับ"
run ssh "$SERVER" "id mecmat >/dev/null 2>&1 || useradd --system --no-create-home --home-dir /nonexistent --shell /usr/sbin/nologin mecmat; \
  install -d -o root -g mecmat -m 750 $APP; install -d -o mecmat -g mecmat -m 750 $DATA $DATA/backups $DATA/uploads; \
  command -v sqlite3 >/dev/null || apt-get install -y sqlite3; \
  if [ -f $APP/Mecmat.Server ]; then rm -rf $APP.prev && cp -a $APP $APP.prev; fi"

say "6/8 rsync → $SERVER:$APP"
run rsync -az --delete --exclude 'appsettings.Production.json' --exclude 'DEPLOYED_COMMIT' "$BUILD_OUT/" "$SERVER:$APP/"

say "7/8 after-deploy + restart"
run ssh "$SERVER" "printf '%s\n' '$(git rev-parse HEAD)' > $APP/DEPLOYED_COMMIT; $APP/after-deploy.sh && systemctl restart mecmat-cekmutt"

say "8/8 ตรวจสุขภาพ"
if [ $DRY = 0 ]; then
  WANT="$(git rev-parse --short HEAD)"
  for i in $(seq 1 30); do
    GOT="$(ssh "$SERVER" "curl -fsS http://127.0.0.1:5180/api/health" 2>/dev/null || true)"
    case "$GOT" in *"$WANT"*) echo "  ✓ $GOT"; break ;; esac
    sleep 2
  done
  case "$GOT" in *"$WANT"*) ;; *) die "เวอร์ชันใหม่ไม่ตอบ" "ssh $SERVER 'journalctl -u mecmat-cekmutt -n 60 --no-pager' · ย้อนกลับ: ssh $SERVER 'rm -rf $APP && mv $APP.prev $APP && systemctl restart mecmat-cekmutt'" ;; esac
  ssh "$SERVER" "curl -fsS -o /dev/null -w 'reaction-erp.com %{http_code}\n' https://reaction-erp.com/ || true"
fi
echo; echo "เสร็จ"
