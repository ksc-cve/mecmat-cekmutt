#!/usr/bin/env python3
"""Lists t('…') keys used in the app that have no translation in tools/i18n/*.tsv.
Dynamic keys (t('pubfield.' + key)) are expanded from the lists the code builds them from.
    python3 -I tools/i18n-check.py
"""
import pathlib, re, sys

root = pathlib.Path(__file__).resolve().parent
app = root.parent / "src" / "Mecmat.Server" / "wwwroot" / "app" / "js"
have = set()
for f in (root / "i18n").glob("*.tsv"):
    for line in f.read_text(encoding="utf-8").splitlines():
        if line.strip():
            have.add(line.split("\t")[0])
used = set()
for f in app.rglob("*.js"):
    if f.parent.name == "i18n":
        continue
    src = f.read_text(encoding="utf-8")
    used.update(re.findall(r"\bt\(\s*'([a-zA-Z0-9_.\-]+)'", src))
    used.update(re.findall(r"(?:label|titleKey|hint|emptyText):\s*'([a-zA-Z0-9_.\-]+\.[a-zA-Z0-9_.\-]+)'", src))
# Dynamic families
mods = ["people", "sensitive", "organizations", "users", "approvals-config", "publications", "projects", "finance", "assets", "resources", "bookings", "tasks", "meetings", "documents", "cms", "inbox", "dashboard", "audit"]
used |= {f"mod.{m}" for m in mods} | {f"mod.{m}.hint" for m in mods}
used |= {f"pubfield.{k}" for k in ["photo", "positionText", "academicRank", "workEmail", "workPhone", "officeRoom", "lineId", "expertise", "bio", "orcid", "scopus", "scholar", "researchgate", "website", "education", "employment", "licenses", "awards", "publications", "projects", "timeline", "mentorship", "network"]}
used |= {f"role.{k}" for k in ["admin", "group-head", "faculty", "researcher", "finance", "lab-manager", "student", "external", "transient"]}
used |= {f"audit.action.{a}" for a in ["Create", "Update", "Delete", "Restore", "Login", "LoginFailed", "Logout", "Register", "PasswordChanged", "PasswordReset", "PasswordResetFromShell", "TotpEnabled", "TotpFailed", "TotpReset", "Decide"]}
used |= {f"appr.subject.{s}" for s in ["MoneyRequest", "Booking"]}
used |= {f"tl.kind.{k}" for k in ["education","position","rank","award","publication","project","supervision","news","visit","talk","collaboration","grant","milestone","other"]}
used |= {f"cal.kind.{k}" for k in ["task", "milestone", "report", "advance", "recruitment"]}
used |= {f"site.hint.{k}" for k in ["pages", "areas", "settings"]}
used |= {f"enum.SubjectType.{k}" for k in ["MoneyRequest", "Booking"]} | {f"enum.Quartile.Q{i}" for i in range(1, 5)}
used = {u for u in used if not u.endswith(".") and not u.startswith("f.") or u in have or re.match(r"^f\.[a-zA-Z]+$", u)}
missing = sorted(u for u in used if u not in have)
print("\n".join(missing))
print(f"-- {len(missing)} missing", file=sys.stderr)
