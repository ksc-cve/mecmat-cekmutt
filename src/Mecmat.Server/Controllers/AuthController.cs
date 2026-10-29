using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mecmat.Auth;
using Mecmat.Data;
using Mecmat.Domain;
using Mecmat.Services;

namespace Mecmat.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(
    AppDbContext db, CurrentUser cu, SessionService sessions, AccessService access,
    NotificationService notify, LoginThrottle throttle, AuditContext audit, AppOptions opt) : ControllerBase
{
    public sealed record LoginBody(string? Username, string? Password);
    public sealed record RegisterBody(
        string? Username, string? Password, string? FirstNameTh, string? LastNameTh, string? FirstNameEn,
        string? LastNameEn, string? Email, string? Phone, PersonCategory? Category, string? Note);
    public sealed record ForgotBody(string? Username, string? Note);
    public sealed record PasswordBody(string? Current, string? New);
    public sealed record CodeBody(string? Code);

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginBody b)
    {
        var username = (b.Username ?? "").Trim().ToLowerInvariant();
        var ipKey = "ip:" + audit.IpHash;
        var userKey = "u:" + username;
        if (throttle.Blocked(ipKey, userKey)) return StatusCode(429, new { error = "auth.tooManyAttempts" });

        var user = await db.Set<UserAccount>().FirstOrDefaultAsync(u => u.Username == username);
        // Verify even when the user does not exist, so response time does not reveal which
        // usernames are real.
        var ok = PasswordHasher.Verify(b.Password ?? "", user?.PasswordHash ?? DummyHash, out var rehash) && user != null;
        if (!ok)
        {
            throttle.Fail(ipKey, userKey);
            db.LogEvent(nameof(UserAccount), user?.Id, "LoginFailed", new { username });
            await db.SaveChangesAsync();
            return Unauthorized(new { error = "auth.invalid" });
        }
        if (user!.Status == AccountStatus.PendingApproval) return StatusCode(403, new { error = "auth.pending" });
        if (user.Status != AccountStatus.Active || (user.ExpiresAt is { } e && e < Clock.TodayBangkok()))
            return StatusCode(403, new { error = "auth.inactive" });

        throttle.Reset(userKey);
        if (rehash) user.PasswordHash = PasswordHasher.Hash(b.Password!);
        user.LastLoginAt = DateTime.UtcNow;
        user.FailedLoginCount = 0;
        audit.UserId = user.Id; audit.PersonId = user.PersonId;
        db.LogEvent(nameof(UserAccount), user.Id, "Login");
        var (session, _) = await sessions.CreateAsync(user, HttpContext, totpPassed: !user.TotpEnabled);
        cu.UserId = user.Id; cu.PersonId = user.PersonId; cu.IsAdmin = user.IsAdmin; cu.Account = user; cu.Session = session;
        await access.LoadGrantsAsync(cu, user);
        return Ok(await MeAsync());
    }

    private static readonly string DummyHash = PasswordHasher.Hash(Tokens.NewToken());

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (cu.Session != null)
        {
            cu.Session.RevokedAt = DateTime.UtcNow;
            cu.Session.RevokeReason = "logout";
            db.LogEvent(nameof(UserAccount), cu.UserId, "Logout");
            await db.SaveChangesAsync();
        }
        SessionService.ClearCookie(HttpContext);
        return Ok(new { ok = true });
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me() => cu.IsAuthenticated ? Ok(await MeAsync()) : Ok(new { authenticated = false });

    private async Task<object> MeAsync()
    {
        var u = cu.Account!;
        var p = await db.Set<Person>().AsNoTracking().FirstAsync(x => x.Id == u.PersonId);
        var mustSetupTotp = opt.RequireTotpForApprovers && !u.TotpEnabled
                            && (u.IsAdmin || await access.IsMoneyApproverAsync(u.PersonId));
        return new
        {
            authenticated = true,
            user = new { u.Id, u.Username, u.IsAdmin, u.Language, u.Theme, u.TotpEnabled, u.MustChangePassword, u.ExpiresAt },
            person = new { p.Id, p.TitleTh, p.FirstNameTh, p.LastNameTh, p.FirstNameEn, p.LastNameEn, p.HasDoctorate, p.AcademicRank, p.PhotoFileId, p.Category },
            csrf = cu.Session!.CsrfToken,
            totpPending = u.TotpEnabled && !cu.Session.TotpPassed,
            mustSetupTotp,
            grants = Modules.All.ToDictionary(m => m, m => new { related = cu.GrantFor(m).Related, all = cu.GrantFor(m).All }),
        };
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterBody b)
    {
        var ipKey = "reg:" + audit.IpHash;
        if (throttle.Blocked(ipKey)) return StatusCode(429, new { error = "auth.tooManyAttempts" });
        throttle.Fail(ipKey); // every sign-up counts, so a script cannot open hundreds of pending accounts

        var username = (b.Username ?? "").Trim().ToLowerInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(username, "^[a-z0-9._-]{3,40}$"))
            return BadRequest(new { error = "auth.username.invalid" });
        if (string.IsNullOrWhiteSpace(b.FirstNameTh) || string.IsNullOrWhiteSpace(b.LastNameTh))
            return BadRequest(new { error = "auth.name.required" });
        var pwError = PasswordHasher.Validate(b.Password, username);
        if (pwError != null) return BadRequest(new { error = pwError });
        if (await db.Set<UserAccount>().AnyAsync(u => u.Username == username))
            return BadRequest(new { error = "auth.username.taken" });

        var category = b.Category ?? PersonCategory.Researcher;
        var person = new Person
        {
            Kind = category is PersonCategory.IndustryPartner or PersonCategory.Collaborator or PersonCategory.Sponsor
                ? PersonKind.External : PersonKind.Internal,
            Category = category,
            FirstNameTh = b.FirstNameTh!.Trim(), LastNameTh = b.LastNameTh!.Trim(),
            FirstNameEn = b.FirstNameEn?.Trim(), LastNameEn = b.LastNameEn?.Trim(),
            WorkEmail = b.Email?.Trim(), MobilePhone = b.Phone?.Trim(),
        };
        db.Add(person);
        await db.SaveChangesAsync();
        var user = new UserAccount
        {
            PersonId = person.Id, Username = username, PasswordHash = PasswordHasher.Hash(b.Password!),
            PasswordChangedAt = DateTime.UtcNow, Status = AccountStatus.PendingApproval,
        };
        db.Add(user);
        db.LogEvent(nameof(UserAccount), null, "Register", new { username, note = b.Note });
        await notify.NotifyUserManagersAsync("account.signup", "notify.signup",
            System.Text.Json.JsonSerializer.Serialize(new { name = $"{person.FirstNameTh} {person.LastNameTh}", username }, Json.Options),
            "#/admin/users?status=PendingApproval", "account.signup");
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpPost("forgot")]
    public async Task<IActionResult> Forgot(ForgotBody b)
    {
        var ipKey = "fgt:" + audit.IpHash;
        if (throttle.Blocked(ipKey)) return StatusCode(429, new { error = "auth.tooManyAttempts" });
        throttle.Fail(ipKey);
        var username = (b.Username ?? "").Trim().ToLowerInvariant();
        if (username.Length is > 0 and <= 80)
        {
            var user = await db.Set<UserAccount>().AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
            db.Add(new PasswordResetRequest { UserId = user?.Id, UsernameTyped = username, Note = b.Note?.Trim() });
            if (user != null)
                await notify.NotifyUserManagersAsync("account.reset", "notify.resetRequest",
                    System.Text.Json.JsonSerializer.Serialize(new { username }, Json.Options), "#/admin/users?reset=1", "account.reset");
            await db.SaveChangesAsync();
        }
        // Same answer whether or not the username exists: the form must not confirm accounts.
        return Ok(new { ok = true });
    }

    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword(PasswordBody b)
    {
        if (!cu.IsAuthenticated) return Unauthorized(new { error = "auth.required" });
        var u = cu.Account!;
        if (!PasswordHasher.Verify(b.Current ?? "", u.PasswordHash, out _)) return BadRequest(new { error = "auth.currentWrong" });
        var err = PasswordHasher.Validate(b.New, u.Username);
        if (err != null) return BadRequest(new { error = err });
        if (b.New == b.Current) return BadRequest(new { error = "auth.password.same" });
        u.PasswordHash = PasswordHasher.Hash(b.New!);
        u.PasswordChangedAt = DateTime.UtcNow;
        u.MustChangePassword = false;
        await sessions.RevokeAllAsync(u.Id, "password-changed", exceptSessionId: cu.Session!.Id);
        db.LogEvent(nameof(UserAccount), u.Id, "PasswordChanged");
        await db.SaveChangesAsync();
        return Ok(await MeAsync());
    }

    [HttpPost("totp/setup")]
    public async Task<IActionResult> TotpSetup()
    {
        if (!cu.IsAuthenticated) return Unauthorized(new { error = "auth.required" });
        var u = cu.Account!;
        if (u.TotpEnabled) return BadRequest(new { error = "auth.totp.alreadyOn" });
        u.TotpSecret = Totp.NewSecret();
        await db.SaveChangesAsync();
        return Ok(new { secret = u.TotpSecret, uri = Totp.OtpAuthUri(u.TotpSecret, u.Username) });
    }

    [HttpPost("totp/enable")]
    public async Task<IActionResult> TotpEnable(CodeBody b)
    {
        if (!cu.IsAuthenticated) return Unauthorized(new { error = "auth.required" });
        var u = cu.Account!;
        if (u.TotpEnabled || u.TotpSecret == null) return BadRequest(new { error = "auth.totp.notSetUp" });
        if (!Totp.Verify(u.TotpSecret, b.Code)) return BadRequest(new { error = "auth.totp.wrong" });
        var codes = Enumerable.Range(0, 10).Select(_ => Tokens.NewToken(6)[..8].ToLowerInvariant()).ToList();
        u.RecoveryCodesHash = string.Join(",", codes.Select(Tokens.Sha256));
        u.TotpEnabled = true;
        cu.Session!.TotpPassed = true;
        db.LogEvent(nameof(UserAccount), u.Id, "TotpEnabled");
        await db.SaveChangesAsync();
        return Ok(new { recoveryCodes = codes });
    }

    [HttpPost("totp/verify")]
    public async Task<IActionResult> TotpVerify(CodeBody b)
    {
        if (!cu.IsAuthenticated) return Unauthorized(new { error = "auth.required" });
        var u = cu.Account!;
        var key = "totp:" + u.Id;
        if (throttle.Blocked(key)) return StatusCode(429, new { error = "auth.tooManyAttempts" });
        var code = (b.Code ?? "").Trim().ToLowerInvariant();
        var ok = u.TotpSecret != null && Totp.Verify(u.TotpSecret, code);
        if (!ok && u.RecoveryCodesHash != null && code.Length == 8)
        {
            // A recovery code works once: it is removed from the list as it is used.
            var hashes = u.RecoveryCodesHash.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            if (hashes.Remove(Tokens.Sha256(code))) { ok = true; u.RecoveryCodesHash = string.Join(",", hashes); }
        }
        if (!ok)
        {
            throttle.Fail(key);
            db.LogEvent(nameof(UserAccount), u.Id, "TotpFailed");
            await db.SaveChangesAsync();
            return BadRequest(new { error = "auth.totp.wrong" });
        }
        cu.Session!.TotpPassed = true;
        throttle.Reset(key);
        await db.SaveChangesAsync();
        return Ok(await MeAsync());
    }

    public sealed record PrefsBody(string? Language, string? Theme);

    [HttpPut("prefs")]
    public async Task<IActionResult> Prefs(PrefsBody b)
    {
        if (!cu.IsAuthenticated) return Unauthorized(new { error = "auth.required" });
        var u = cu.Account!;
        if (b.Language is "th" or "en") u.Language = b.Language;
        if (b.Theme is "light" or "dark" or "auto") u.Theme = b.Theme;
        await db.SaveChangesAsync();
        return Ok(new { u.Language, u.Theme });
    }
}
