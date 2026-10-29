using Microsoft.EntityFrameworkCore;
using Mecmat.Data;
using Mecmat.Domain;
using Mecmat.Services;

namespace Mecmat.Auth;

/// <summary>Resolves the session cookie into <see cref="CurrentUser"/>, enforces CSRF on state-
/// changing API calls, and fences off a session that still owes a password change or a TOTP code.</summary>
public sealed class SessionMiddleware(RequestDelegate next)
{
    // Endpoints a session may call while it still must change its password or pass TOTP.
    private static readonly string[] GatedAllow =
        ["/api/auth/me", "/api/auth/logout", "/api/auth/password", "/api/auth/totp", "/api/i18n"];
    // Anonymous POSTs that cannot carry a CSRF header because no session exists yet.
    private static readonly string[] CsrfExempt =
        ["/api/auth/login", "/api/auth/register", "/api/auth/forgot"];

    public async Task InvokeAsync(HttpContext http, SessionService sessions, AccessService access,
        AppDbContext db, CurrentUser cu, AuditContext audit, AppOptions opt)
    {
        var path = http.Request.Path.Value ?? "";
        audit.IpHash = Tokens.HashIp(http.Connection.RemoteIpAddress?.ToString(), opt.IpSalt);

        var session = await sessions.FindLiveAsync(http.Request.Cookies[SessionService.CookieName]);
        if (session != null)
        {
            var user = await db.Set<UserAccount>().FirstOrDefaultAsync(u => u.Id == session.UserId);
            if (user is { Status: AccountStatus.Active } && !Expired(user))
            {
                cu.UserId = user.Id; cu.PersonId = user.PersonId; cu.IsAdmin = user.IsAdmin;
                cu.Language = user.Language; cu.Session = session; cu.Account = user;
                audit.UserId = user.Id; audit.PersonId = user.PersonId;
                await access.LoadGrantsAsync(cu, user);
                await sessions.TouchAsync(session);
                if (opt.RequireTotpForApprovers && !user.TotpEnabled
                    && (user.IsAdmin || await access.IsMoneyApproverAsync(user.PersonId)))
                    http.Items["mustSetupTotp"] = true;
            }
        }

        if (path.StartsWith("/api/", StringComparison.Ordinal))
        {
            var mutating = !HttpMethods.IsGet(http.Request.Method) && !HttpMethods.IsHead(http.Request.Method);
            if (mutating && cu.Session != null && !CsrfExempt.Contains(path))
            {
                var sent = http.Request.Headers["X-CSRF"].ToString();
                if (sent.Length == 0 || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                        System.Text.Encoding.ASCII.GetBytes(sent), System.Text.Encoding.ASCII.GetBytes(cu.Session.CsrfToken)))
                {
                    await Problem(http, 403, "errors.csrf");
                    return;
                }
            }
            if (cu.Session != null && cu.Account != null && !GatedAllow.Any(p => path.StartsWith(p, StringComparison.Ordinal)))
            {
                if (cu.Account.MustChangePassword) { await Problem(http, 403, "auth.mustChangePassword"); return; }
                if (cu.Account.TotpEnabled && !cu.Session.TotpPassed) { await Problem(http, 403, "auth.totpRequired"); return; }
                if (http.Items.ContainsKey("mustSetupTotp")) { await Problem(http, 403, "auth.mustSetupTotp"); return; }
            }
        }
        await next(http);
    }

    private static bool Expired(UserAccount u) => u.ExpiresAt is { } e && e < Clock.TodayBangkok();

    public static Task Problem(HttpContext http, int status, string key, object? detail = null)
    {
        http.Response.StatusCode = status;
        return http.Response.WriteAsJsonAsync(new { error = key, detail }, Json.Options);
    }
}
