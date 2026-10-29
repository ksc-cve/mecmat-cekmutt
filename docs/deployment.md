# การติดตั้งและส่งงานขึ้นเซิร์ฟเวอร์

เซิร์ฟเวอร์: `root@187.52.127.97` (ใช้เครื่องร่วมกับ Reaction ERP แต่ไม่ใช้ไฟล์ ผู้ใช้ หรือข้อมูลร่วมกัน)

| สิ่ง | ที่อยู่ |
|---|---|
| โปรแกรม | `/opt/mecmat-cekmutt` (root:mecmat 750) |
| ข้อมูล | `/var/lib/mecmat-cekmutt` (mecmat:mecmat 750): `mecmat.db`, `uploads/`, `field.key`, `ip.salt`, `dp-keys/`, `backups/` |
| บริการ | `mecmat-cekmutt.service` ผู้ใช้ `mecmat` พอร์ต `127.0.0.1:5180` |
| สำรองข้อมูล | `/etc/cron.d/mecmat-cekmutt-backup` ทุกวัน 02:40 เก็บ 30 วัน |
| เว็บ | บล็อก `187.52.127.97 { … }` ใน `/etc/caddy/Caddyfile` ([deploy/Caddy-block.txt](../deploy/Caddy-block.txt)) |

> **field.key สำคัญที่สุด** ถ้าหายไป เลขประจำตัวประชาชน เลขบัญชี และ TOTP secret จะอ่านไม่ได้อีกเลย
> backup.sh จึงสำรอง `keys-*.tgz` ไว้ด้วย ควรดึงสำเนาลง Mac เป็นประจำด้วย `deploy/pull-backups.sh`

## ติดตั้งครั้งแรก (ทำครั้งเดียว)
1. บน Mac: `deploy/ship.sh --dry-run` เพื่อดูว่าจะทำอะไรบ้าง จากนั้นรัน `deploy/ship.sh`
   - ship.sh จะสร้างผู้ใช้ `mecmat` และโฟลเดอร์ ส่งโปรแกรมขึ้นไป และติดตั้ง unit กับ cron
   - การ restart ครั้งแรกจะยังไม่มีบริการให้ restart เป็นเรื่องปกติ
2. บนเซิร์ฟเวอร์: `ssh root@187.52.127.97 /opt/mecmat-cekmutt/first-install.sh`
   - คำสั่งนี้ enable และ start บริการ แล้ว**แสดงรหัสผ่านครั้งแรกของ kasan.cha บนจอนั้นเพียงครั้งเดียว** โดยไม่บันทึกลง log
   - first-install.sh ไม่ได้อยู่ในชุด publish ให้คัดลอกขึ้นไปก่อน: `scp deploy/first-install.sh root@187.52.127.97:/opt/mecmat-cekmutt/`
3. Caddy (ต้องระวังที่สุด เพราะถ้าผิด reaction-erp.com จะล่มด้วย):
   ```
   ssh root@187.52.127.97
   caddy version                      # 2.10 ขึ้นไปใช้ใบรับรอง IP ของ Let's Encrypt ได้ ถ้าต่ำกว่าให้ใช้ tls internal
   cp /etc/caddy/Caddyfile /etc/caddy/Caddyfile.bak-$(date +%F)
   nano /etc/caddy/Caddyfile          # วางบล็อกจาก deploy/Caddy-block.txt ต่อท้าย โดยไม่แตะบล็อกของ ERP
   caddy validate --config /etc/caddy/Caddyfile
   systemctl reload caddy
   curl -sI https://reaction-erp.com | head -1     # ERP ยังต้องตอบ 200
   ```
   - ถ้า Caddyfile มีบล็อกแบบ catch-all (`:443` หรือ `http://`) อยู่แล้ว ให้หยุดและปรึกษากันก่อน
4. เปิด `https://187.52.127.97/app` → เข้าระบบด้วยรหัสจากข้อ 2 → ตั้งรหัสผ่านใหม่ → ตั้งค่า Authenticator (บังคับสำหรับ Admin)

## ส่งงานรอบถัดไป
```
deploy/ship.sh --dry-run
deploy/ship.sh
```
- ต้อง commit และ push ก่อน ship.sh จะตรวจให้
- ถ้ามี migration ที่เสี่ยง (Drop, Rename, AlterColumn, FK, Sql) ship.sh จะ backup ให้ก่อน
- ship.sh เก็บเวอร์ชันเดิมไว้ที่ `/opt/mecmat-cekmutt.prev`
- หลัง restart ship.sh จะตรวจ `/api/health` ว่าตรงกับ commit ที่ส่งไป และตรวจว่า reaction-erp.com ยังตอบปกติ

**ย้อนกลับ**: `ssh root@187.52.127.97 'rm -rf /opt/mecmat-cekmutt && mv /opt/mecmat-cekmutt.prev /opt/mecmat-cekmutt && systemctl restart mecmat-cekmutt'`

## Admin ลืมรหัสผ่าน (ระบบไม่มีอีเมล)
```
ssh root@187.52.127.97
sudo -u mecmat env ASPNETCORE_ENVIRONMENT=Production Mecmat__DataRoot=/var/lib/mecmat-cekmutt /opt/mecmat-cekmutt/Mecmat.Server reset-admin kasan.cha
```
- คำสั่งนี้แสดงรหัสใช้ครั้งเดียวบนจอนั้น
- ถ้าทำโทรศัพท์ที่ใช้ Authenticator หาย ให้ใช้รหัสสำรอง 8 ตัวที่ได้ตอนตั้งค่า

## กู้ข้อมูลจาก backup
```
systemctl stop mecmat-cekmutt
cd /var/lib/mecmat-cekmutt
mv mecmat.db mecmat.db.broken-$(date +%F); rm -f mecmat.db-wal mecmat.db-shm
gunzip -c backups/mecmat-YYYYMMDD-HHMMSS.db.gz > mecmat.db
tar -xzf backups/keys-YYYYMMDD-HHMMSS.tgz        # เฉพาะเมื่อ field.key หาย
tar -xzf backups/uploads-…tgz                   # เฉพาะเมื่อ uploads หาย
chown -R mecmat:mecmat /var/lib/mecmat-cekmutt
systemctl start mecmat-cekmutt
```
