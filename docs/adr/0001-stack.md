# ADR 0001 — เทคโนโลยีหลัก

- สถานะ: ตัดสินแล้ว (เจ้าของอนุมัติ 2026-10-28)

## บริบท
CLAUDE.md กำหนดให้ถามเจ้าของก่อนเลือกเทคโนโลยี เจ้าของต้องการ "แนวคิดเดียวกับ Reaction ERP" แต่เป็นโปรเจกต์ที่แยกขาดจากกัน
ระบบมีผู้ใช้ประจำราว 20 คนและขาจรราว 30 คนต่อปี และรันบน VPS คอร์เดียวที่ใช้ร่วมกับ ERP

## ตัดสินใจ
- **.NET 10 LTS** (ได้รับการดูแลถึง พ.ย. 2028) ไม่ใช้ .NET 8 เพราะจะหมดการดูแลวันที่ 10 พ.ย. 2026 ซึ่งเร็วกว่าที่ระบบนี้จะเปิดใช้
- **ASP.NET Core โปรเจกต์เดียว** (`src/Mecmat.Server`) มีทั้ง API controllers และ Razor Pages
- **SQLite + EF Core code-first** ใช้ WAL และ foreign keys และ migrate อัตโนมัติตอนเริ่มโปรแกรม ไฟล์ฐานข้อมูลอยู่ที่ `~/mecmat-data/mecmat.db` (Mac) และ `/var/lib/mecmat-cekmutt/mecmat.db` (เซิร์ฟเวอร์)
- **Frontend หลังบ้านเป็น vanilla ES modules**: ไม่มี npm, bundler หรือ TypeScript และไม่โหลดจาก CDN คอมโพเนนต์ทุกตัวเขียนเองในรูปแบบ factory ที่คืน `{el, get, set, destroy}`
- **NuGet มีเพียง** `Microsoft.EntityFrameworkCore.Sqlite` และ `Microsoft.EntityFrameworkCore.Design` การเพิ่ม package ใด ๆ ถือเป็นการตัดสินใจของเจ้าของ ไม่ใช่ความสะดวกของผู้พัฒนา
- **Build output** อยู่ที่ `~/dev/mecmat-cekmutt-build` เพราะ bin/obj ใน Google Drive ทำให้ sync หนักและเคยทำให้ .git เสียหาย (บทเรียนจาก ERP)

## ผลที่ตามมา
- SQLite รับการเขียนได้ทีละรายการ ซึ่งเพียงพอสำหรับผู้ใช้หลักสิบ และยังช่วยให้การกันจองซ้อนและการตัดยอดเงินทำใน transaction เดียวได้ง่าย
- ถ้าวันหนึ่งผู้ใช้เกินหลักร้อยพร้อมกัน ให้ทบทวน ADR นี้
- ไม่มี build step ฝั่งหน้าเว็บ แก้ไฟล์แล้ว refresh ได้ทันที แต่ต้องเขียนคอมโพเนนต์เองทั้งหมด
