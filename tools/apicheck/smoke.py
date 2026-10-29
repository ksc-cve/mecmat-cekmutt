#!/usr/bin/env python3
"""End-to-end API check against a running server on a THROWAWAY data folder.

Usage: python3 -I tools/apicheck/smoke.py http://127.0.0.1:5189 <admin-one-time-password>

Walks the main flows the way the app does: first sign-in, forced password change, TOTP set-up,
sign-up approval, permissions per role, money request → approval → settlement, booking clash,
public-field rules. Exits non-zero on the first broken expectation.
"""
import base64, hashlib, hmac, http.cookiejar, json, struct, sys, time, urllib.request, urllib.error

BASE = sys.argv[1].rstrip("/")
ADMIN_OTP = sys.argv[2]
FAILS = []


class Client:
    def __init__(self):
        self.jar = http.cookiejar.CookieJar()
        self.op = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(self.jar))
        self.csrf = None

    def call(self, method, path, body=None, expect=200):
        data = None if body is None else json.dumps(body).encode()
        req = urllib.request.Request(BASE + path, data=data, method=method)
        req.add_header("Content-Type", "application/json")
        if self.csrf:
            req.add_header("X-CSRF", self.csrf)
        try:
            with self.op.open(req) as r:
                status, raw = r.status, r.read()
        except urllib.error.HTTPError as e:
            status, raw = e.code, e.read()
        try:
            out = json.loads(raw) if raw else None
        except ValueError:
            out = raw.decode(errors="replace")
        if expect is not None and status != expect:
            FAILS.append(f"{method} {path}: expected {expect}, got {status} {out}")
            print("  ✗", FAILS[-1])
        if isinstance(out, dict) and out.get("csrf"):
            self.csrf = out["csrf"]
        return out


def totp(secret):
    key = base64.b32decode(secret + "=" * (-len(secret) % 8))
    msg = struct.pack(">Q", int(time.time()) // 30)
    h = hmac.new(key, msg, hashlib.sha1).digest()
    o = h[-1] & 15
    return "%06d" % ((struct.unpack(">I", h[o:o + 4])[0] & 0x7FFFFFFF) % 1000000)


def check(cond, msg):
    if not cond:
        FAILS.append(msg)
        print("  ✗", msg)
    else:
        print("  ✓", msg)


def sign_in_admin():
    a = Client()
    print("· admin first sign-in")
    me = a.call("POST", "/api/auth/login", {"username": "kasan.cha", "password": ADMIN_OTP})
    check(me["user"]["mustChangePassword"], "one-time password forces a change")
    a.call("GET", "/api/r/people", expect=403)
    a.call("POST", "/api/auth/password", {"current": ADMIN_OTP, "new": "Mecmat-Admin-2026!"})
    me = a.call("GET", "/api/auth/me")
    check(me["mustSetupTotp"], "admin must set up TOTP")
    a.call("GET", "/api/r/people", expect=403)
    s = a.call("POST", "/api/auth/totp/setup")
    codes = a.call("POST", "/api/auth/totp/enable", {"code": totp(s["secret"])})
    check(len(codes["recoveryCodes"]) == 10, "ten recovery codes issued")
    a.secret = s["secret"]
    return a


def main():
    anon = Client()
    print("· anonymous")
    anon.call("GET", "/api/r/people", expect=401)
    anon.call("POST", "/api/auth/login", {"username": "kasan.cha", "password": "wrong-password"}, expect=401)

    a = sign_in_admin()
    people = a.call("GET", "/api/r/people")
    check(people["total"] >= 1, "admin lists people")
    kasan = next(r for r in people["rows"] if r["firstNameEn"] == "Kasan")
    check(kasan["firstNameTh"] == "กสาน", "seeded admin profile present")

    print("· sign-up and approval")
    anon.call("POST", "/api/auth/register", {"username": "ra.somchai", "password": "Ra-Pass-123456", "firstNameTh": "สมชาย", "lastNameTh": "ทดสอบ",
                                             "email": "ra@example.com", "category": "ResearchAssistant"})
    anon.call("POST", "/api/auth/login", {"username": "ra.somchai", "password": "Ra-Pass-123456"}, expect=403)
    users = a.call("GET", "/api/users")
    ra_user = next(u for u in users["users"] if u["username"] == "ra.somchai")
    a.call("POST", f"/api/users/{ra_user['id']}/approve", {"roleKeys": ["researcher"]})
    ra = Client()
    me = ra.call("POST", "/api/auth/login", {"username": "ra.somchai", "password": "Ra-Pass-123456"})
    ra_person = me["person"]["id"]
    check(me["grants"]["finance"]["related"] == "Create", "researcher may raise money requests on related accounts")

    print("· people permissions")
    ra.call("PUT", f"/api/r/people/{kasan['id']}", {"nickname": "hacked"}, expect=403)
    ra.call("PUT", f"/api/r/people/{ra_person}", {"nickname": "ชาย", "nationalId": "1234567890123"})
    mine = ra.call("GET", f"/api/r/people/{ra_person}")
    check(mine["nationalId"].endswith("0123") and "•" in mine["nationalId"], "national ID is masked in reads")
    full = ra.call("GET", f"/api/sensitive/people/{ra_person}/nationalId")
    check(full["value"] == "1234567890123", "owner can reveal own national ID")
    ra.call("POST", "/api/r/public-visibility", {"personId": ra_person, "fieldKey": "nationalId", "isPublic": True}, expect=400)
    ra.call("POST", "/api/r/public-visibility", {"personId": ra_person, "fieldKey": "workEmail", "isPublic": True})
    ra.call("POST", "/api/r/person-education", {"personId": kasan["id"], "level": "Bachelor", "field": "x"}, expect=403)

    print("· project and money")
    prj = a.call("POST", "/api/r/projects", {"titleTh": "โครงการทดสอบ", "kind": "Project", "status": "Active"})
    a.call("POST", "/api/r/project-members", {"projectId": prj["id"], "personId": ra_person, "role": "ResearchAssistant"})
    root = a.call("POST", "/api/r/accounts", {"code": "PRJ-T", "nameTh": "บัญชีโครงการทดสอบ", "kind": "Project", "projectId": prj["id"]})
    mat = a.call("POST", "/api/r/accounts", {"code": "PRJ-T-MAT", "nameTh": "ค่าวัสดุ", "kind": "BudgetCategory", "budgetCategory": "Materials", "parentAccountId": root["id"]})
    a.call("POST", "/api/finance/income", {"accountId": mat["id"], "amount": 1000000, "description": "งวดที่ 1"})
    a.call("POST", "/api/finance/income", {"accountId": root["id"], "amount": 100, "description": "parent"}, expect=400)
    tree = a.call("GET", "/api/finance/tree")
    node = next(n for n in tree["accounts"] if n["id"] == root["id"])
    check(node["balance"] == 1000000, "parent balance rolls up from child (10,000.00 THB)")

    req = ra.call("POST", "/api/finance/requests", {"kind": "Reimbursement", "accountId": mat["id"], "purpose": "ซื้อกาว", "lines": [{"description": "กาวอีพ็อกซี", "qty": 2, "unitPrice": 150000}]})
    ra.call("POST", f"/api/finance/requests/{req['id']}/submit")
    big = ra.call("POST", "/api/finance/requests", {"kind": "Payment", "accountId": mat["id"], "purpose": "เกินงบ", "lines": [{"description": "x", "qty": 1, "unitPrice": 800000}]})
    ra.call("POST", f"/api/finance/requests/{big['id']}/submit", expect=400)  # 3,000 committed + 8,000 > 10,000
    ra.call("POST", f"/api/finance/requests/{req['id']}/settle", {}, expect=403)
    inbox = a.call("GET", "/api/approvals/inbox")
    item = next((w for w in inbox["waiting"] if w["subjectId"] == req["id"]), None)
    check(item is not None, "PI (admin who created the project) gets the approval")
    if item:
        ra.call("POST", f"/api/approvals/{item['id']}/decide", {"decision": "Approve"}, expect=400)
        a.call("POST", f"/api/approvals/{item['id']}/decide", {"decision": "Approve"})
    detail = a.call("GET", f"/api/finance/requests/{req['id']}")
    check(detail["request"]["status"] == "Approved", "request approved after single PI step (≤ 5,000 THB)")
    a.call("POST", f"/api/finance/requests/{req['id']}/settle", {})
    tree = a.call("GET", "/api/finance/tree")
    node = next(n for n in tree["accounts"] if n["id"] == mat["id"])
    check(node["balance"] == 700000, "settlement books the expense (7,000.00 left)")
    stmt = a.call("GET", f"/api/finance/accounts/{mat['id']}/statement")
    exp = next(l for l in stmt["lines"] if l["kind"] == "Expense")
    a.call("POST", f"/api/finance/ledger/{exp['id']}/reverse", {"reason": "ทดสอบกลับรายการ"})
    a.call("POST", f"/api/finance/ledger/{exp['id']}/reverse", {"reason": "ซ้ำ"}, expect=400)
    tree = a.call("GET", "/api/finance/tree")
    node = next(n for n in tree["accounts"] if n["id"] == mat["id"])
    check(node["balance"] == 1000000, "reversal restores the balance")

    print("· booking")
    res = a.call("POST", "/api/r/resources", {"nameTh": "เครื่อง UTM 100 kN", "kind": "Equipment", "slotMinutes": 30, "requiresApproval": False,
                                              "openHoursJson": json.dumps([["08:00", "18:00"]] * 5 + [None, None])})
    import datetime as dt
    d = dt.datetime.now(dt.timezone.utc) + dt.timedelta(days=1)
    while d.weekday() >= 5:
        d += dt.timedelta(days=1)
    start = d.replace(hour=3, minute=0, second=0, microsecond=0)  # 10:00 Bangkok
    end = start + dt.timedelta(hours=2)
    ra.call("POST", "/api/bookings", {"resourceId": res["id"], "startAt": start.isoformat(), "endAt": end.isoformat(), "title": "ทดสอบดัด"})
    a.call("POST", "/api/bookings", {"resourceId": res["id"], "startAt": (start + dt.timedelta(hours=1)).isoformat(), "endAt": (end + dt.timedelta(hours=1)).isoformat(), "title": "ชน"}, expect=409)
    late = start.replace(hour=12)  # 19:00 Bangkok — after closing
    a.call("POST", "/api/bookings", {"resourceId": res["id"], "startAt": late.isoformat(), "endAt": (late + dt.timedelta(hours=1)).isoformat(), "title": "ดึก"}, expect=400)
    cal = a.call("GET", f"/api/bookings?from={(start - dt.timedelta(days=1)).isoformat()}&to={(start + dt.timedelta(days=1)).isoformat()}".replace("+", "%2B"))
    check(len(cal["bookings"]) == 1 and cal["bookings"][0]["title"] == "ทดสอบดัด", "admin sees booking with details")

    print("· audit and CSRF")
    log = a.call("GET", "/api/audit?entityType=LedgerEntry")
    check(len(log) >= 3, "ledger writes are in the change log")
    bad = Client(); bad.jar = a.jar; bad.op = a.op; bad.csrf = "nope"
    bad.call("POST", "/api/r/organizations", {"nameTh": "x"}, expect=403)

    print()
    if FAILS:
        print(f"{len(FAILS)} FAILED")
        sys.exit(1)
    print("ALL PASSED")


main()
