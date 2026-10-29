using Microsoft.EntityFrameworkCore;
using Mecmat.Data;
using Mecmat.Domain;
using Mecmat.Services;

namespace Mecmat.Auth;

public sealed class SessionService(AppDbContext db, AppOptions opt)
{
    public const string CookieName = "mecmat_s";
    public static readonly TimeSpan Idle = TimeSpan.FromHours(12);
    public static readonly TimeSpan Absolute = TimeSpan.FromDays(7);

    public async Task<(Session Session, string Token)> CreateAsync(UserAccount user, HttpContext http, bool totpPassed)
    {
        var token = Tokens.NewToken();
        var now = DateTime.UtcNow;
        var s = new Session
        {
            TokenHash = Tokens.Sha256(token), UserId = user.Id, CsrfToken = Tokens.NewToken(24),
            LastSeenAt = now, IdleExpiresAt = now + Idle, AbsoluteExpiresAt = now + Absolute,
            TotpPassed = totpPassed, IpHash = Tokens.HashIp(http.Connection.RemoteIpAddress?.ToString(), opt.IpSalt),
            UserAgent = Truncate(http.Request.Headers.UserAgent.ToString(), 200),
        };
        db.Add(s);
        await db.SaveChangesAsync();
        WriteCookie(http, token, s.AbsoluteExpiresAt);
        return (s, token);
    }

    public void WriteCookie(HttpContext http, string token, DateTime expires) =>
        http.Response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            // Plain http is only ever the developer's own machine on 127.0.0.1; production sits
            // behind Caddy's HTTPS (ADR 0006).
            Secure = !opt.IsDevelopment || http.Request.IsHttps,
            SameSite = SameSiteMode.Strict, Path = "/", Expires = expires, IsEssential = true,
        });

    public static void ClearCookie(HttpContext http) =>
        http.Response.Cookies.Delete(CookieName, new CookieOptions { Path = "/" });

    public async Task<Session?> FindLiveAsync(string? token)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 100) return null;
        var hash = Tokens.Sha256(token);
        var s = await db.Set<Session>().FirstOrDefaultAsync(x => x.TokenHash == hash);
        var now = DateTime.UtcNow;
        if (s == null || s.RevokedAt != null || s.IdleExpiresAt < now || s.AbsoluteExpiresAt < now) return null;
        return s;
    }

    /// <summary>Slides the idle window at most every five minutes, and without an audit row:
    /// heartbeats are bookkeeping, not history.</summary>
    public async Task TouchAsync(Session s)
    {
        var now = DateTime.UtcNow;
        if (now - s.LastSeenAt < TimeSpan.FromMinutes(5)) return;
        s.LastSeenAt = now;
        s.IdleExpiresAt = now + Idle;
        db.SkipAudit = true;
        try { await db.SaveChangesAsync(); } finally { db.SkipAudit = false; }
    }

    public async Task RevokeAllAsync(string userId, string reason, string? exceptSessionId = null)
    {
        var now = DateTime.UtcNow;
        var live = await db.Set<Session>().Where(s => s.UserId == userId && s.RevokedAt == null && s.Id != exceptSessionId).ToListAsync();
        foreach (var s in live) { s.RevokedAt = now; s.RevokeReason = reason; }
    }

    private static string Truncate(string v, int n) => v.Length <= n ? v : v[..n];
}
