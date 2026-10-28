# mecmat-cekmutt — อ่านไฟล์นี้ก่อนเริ่มงานทุกครั้ง

เว็บไซต์นี้เป็น**โปรเจกต์แยก**จาก Reaction ERP (เจ้าของสั่ง 2026-10-28:
"แยกระหว่างสองเว็บไซต์ออกจากกันให้ชัดเจน ไม่ใช้อะไรร่วมกันเลย ทั้งโฟลเดอร์ ฐานข้อมูล และ Backup ต่าง ๆ")

**ตัวงาน**: เว็บของกลุ่มวิจัย MECMAT (Mechanics and Materials Research Group, ภาควิชาวิศวกรรมโยธา มจธ.)
ประกอบด้วยหน้าเว็บประชาสัมพันธ์ (TH/EN) และหลังบ้านสำหรับบริหารกลุ่มวิจัย ซึ่งใช้ฐานข้อมูลเดียวกัน

**เทคโนโลยี (เจ้าของตัดสินแล้ว 2026-10-28, [docs/adr/0001-stack.md](docs/adr/0001-stack.md))**
- .NET 10 LTS · ASP.NET Core โปรเจกต์เดียว
- SQLite + EF Core
- หน้าสาธารณะ: Razor Pages
- หลังบ้าน: SPA แบบ vanilla JS ไม่มี npm

**เอกสารที่ต้องอ่านก่อนทำงาน**
- [docs/data-model.md](docs/data-model.md)
- [docs/authorization.md](docs/authorization.md)
- [docs/adr/](docs/adr/)
- [docs/OUTSTANDING.md](docs/OUTSTANDING.md)

## สิ่งที่เจ้าของสั่งไว้ (ห้ามทำเกิน)
- **ทำทั้งระบบพร้อมกัน** เพราะข้อมูลทุกส่วนเชื่อมโยงกัน ให้ออกแบบสคีมาทั้งหมดก่อน แล้วเปิดใช้งานพร้อมกัน
- **ไม่ใช้อีเมล** แจ้งเตือนในแอปเท่านั้น และ Admin เป็นผู้รีเซ็ตรหัสผ่าน
- **การเงินเป็นตัวเลขเท่านั้น** ไม่ออกเอกสารหรือแบบฟอร์ม PDF และไม่มีระบบจ่ายเงินจริง
- **ใช้ IP `187.52.127.97` ไปก่อน** จนกว่าจะมีโดเมน ([ADR 0006](docs/adr/0006-hosting-ip-first.md))
- ผู้ใช้เลือกเองว่าจะแสดงฟิลด์ใดบนหน้าสาธารณะ ([ADR 0007](docs/adr/0007-privacy.md))
- CI ใช้ตามคู่มือ มจธ. 2022 โทนสีน้ำเงิน ส้ม เหลือง เทา และแดงเลือดหมู
- Admin คนแรกคือ ดร.กสาน จันทร์โต
- ระหว่างทำงาน **ต้องรายงานความคืบหน้าเป็นระยะ** ห้ามเงียบหายไป

## กฎการพัฒนา (แนวคิดเดียวกับ ERP เขียนใหม่ทั้งหมด ไม่ใช้โค้ดร่วมกัน)
- NuGet มีแค่ EF Core Sqlite และ Design การเพิ่ม dependency ทุกตัว (NuGet, ไฟล์ JS/CSS ภายนอก) ต้องให้เจ้าของตัดสิน
- ห้ามใช้ npm, bundler หรือ CDN ฟอนต์และไอคอนให้ self-host ใน `wwwroot/assets`
- ข้อความที่ผู้ใช้เห็นต้องผ่าน `t()` ถ้าโหมดไทยยังมีภาษาอังกฤษค้างอยู่ ถือเป็นข้อบกพร่อง
- ตรวจสิทธิ์ที่เซิร์ฟเวอร์ และห้ามแสดงปุ่มที่กดแล้วเซิร์ฟเวอร์จะปฏิเสธ
- การเปลี่ยนสถานะทุกครั้งต้องลง ChangeLog ใน transaction เดียวกับการแก้ไข
- ลบแบบ soft delete เท่านั้น
- Enum เก็บเป็นข้อความ
- Id ให้เซิร์ฟเวอร์สร้าง
- เงินเก็บเป็น `long` หน่วยสตางค์
- หน้าสาธารณะอ่านข้อมูลผ่าน `PublicQueryService` เท่านั้น
- ก่อน migration ที่เสี่ยง (Drop, Rename, AlterColumn, FK, Sql) ต้อง backup ก่อน และ backup ต้องใช้ `sqlite3 .backup` ห้ามใช้ `cp`
- ข้อมูลจริงห้ามอยู่ใน git หรือใน Google Drive (ใช้ `~/mecmat-data`) และ build output อยู่ที่ `~/dev/mecmat-cekmutt-build`
- Commit และ push บ่อย ๆ เพราะ Google Drive เคยทำ .git หาย
- Comment อธิบาย *ทำไม* ไม่ใช่ *ทำอะไร*
- การตัดสินใจสำคัญให้บันทึกเป็น ADR

## 🔴 กฎการแยกจาก Reaction ERP — ห้ามละเมิด

| สิ่งของ | Reaction ERP (ห้ามแตะ) | mecmat-cekmutt (ของเรา) |
|---|---|---|
| โฟลเดอร์โค้ดบน Mac | `MyWebApp/ReactionERP/reaction-erp` | `MyWebApp/mecmat-cekmutt` |
| GitHub | `ksc-cve/reaction-erp` | `ksc-cve/mecmat-cekmutt` |
| ข้อมูลบน Mac | `~/emp-data`, `MyWebApp/ReactionERP/` | `~/mecmat-data` |
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
