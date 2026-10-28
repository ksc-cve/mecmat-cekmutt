# mecmat-cekmutt — อ่านไฟล์นี้ก่อนเริ่มงานทุกครั้ง

เว็บไซต์นี้เป็น**โปรเจกต์แยก**จาก Reaction ERP (เจ้าของสั่ง 2026-10-28:
"แยกระหว่างสองเว็บไซต์ออกจากกันให้ชัดเจน ไม่ใช้อะไรร่วมกันเลย ทั้งโฟลเดอร์ ฐานข้อมูล และ Backup ต่าง ๆ")

> ประเภทของเว็บ (static / .NET / PHP / อื่น ๆ) **ยังไม่ได้ตัดสิน** — ถามเจ้าของก่อนเลือกเทคโนโลยี

## 🔴 กฎการแยกจาก Reaction ERP — ห้ามละเมิด

| สิ่งของ | Reaction ERP (ห้ามแตะ) | mecmat-cekmutt (ของเรา) |
|---|---|---|
| โฟลเดอร์โค้ดบน Mac | `VS_Code/Engineering_Management Platform` | `VS_Code/mecmat-cekmutt` |
| GitHub | `ksc-cve/reaction-erp` | `ksc-cve/mecmat-cekmutt` |
| ข้อมูลบน Mac | `~/emp-data`, `VS_Code/emp-data` | `~/mecmat-data` |
| พอร์ตบน Mac และเซิร์ฟเวอร์ | 5170 (และ 5171–5172 ใช้ทดสอบ) | **5180** |
| โปรแกรมบนเซิร์ฟเวอร์ | `/opt/reaction-erp` | `/opt/mecmat-cekmutt` |
| ข้อมูลบนเซิร์ฟเวอร์ | `/var/lib/reaction-erp` | `/var/lib/mecmat-cekmutt` |
| สำรองข้อมูล | `/var/lib/reaction-erp/backups` + `backup.sh` ของ ERP | `/var/lib/mecmat-cekmutt/backups` + สคริปต์และตารางเวลาของตัวเอง |
| ผู้ใช้ Linux | `erp` | `mecmat` (อ่านโฟลเดอร์ของ ERP ไม่ได้ — `/var/lib/reaction-erp` เป็น 750 erp:erp) |
| บริการ systemd | `reaction-erp` | `mecmat-cekmutt` |
| สคริปต์ส่งงานขึ้นเซิร์ฟเวอร์ | `deploy/ship.sh` ของ ERP | สคริปต์ของ repo นี้เอง |

**สิ่งที่ใช้ร่วมกันได้เพียงสองอย่าง และหลีกเลี่ยงไม่ได้**:
1. **ตัวเครื่อง VPS** `187.52.127.97` (Hostinger KVM 1 · 1 vCPU · RAM 3.9 GB) — CPU คอร์เดียว ถ้าเว็บนี้หนัก ERP จะช้าตาม
2. **Caddy** — โปรแกรมเดียวในเครื่องที่ถือพอร์ต 80/443 ได้ เว็บนี้คือ "บล็อกโดเมนหนึ่งบล็อก" ใน `/etc/caddy/Caddyfile`
   **แก้เฉพาะบล็อกของเรา** แล้ว `caddy validate --config /etc/caddy/Caddyfile` ก่อน `systemctl reload caddy` ทุกครั้ง
   (Caddyfile ที่ผิดทำให้ reaction-erp.com ล่มไปด้วย)

ห้ามโค้ด สคริปต์ หรือ cron ของ repo นี้อ่าน/เขียน/สำรอง/ลบ อะไรใต้ `/opt/reaction-erp`, `/var/lib/reaction-erp`, `~/emp-data`
