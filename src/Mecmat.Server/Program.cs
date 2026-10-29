using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Mecmat.Auth;
using Mecmat.Bookings;
using Mecmat.Data;
using Mecmat.Finance;
using Mecmat.Services;

// Ids, reminder keys and logs must not depend on the machine's locale: on a Thai-locale Mac
// "yyMMdd" formatted with the Buddhist calendar and produced PUB-691029-… for 2026-10-29.
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);
var isDev = builder.Environment.IsDevelopment();

var dataRoot = builder.Configuration["Mecmat:DataRoot"]
               ?? (isDev ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "mecmat-data") : "/var/lib/mecmat-cekmutt");
Directory.CreateDirectory(dataRoot);
FieldCipher.Initialise(dataRoot);
var options = new AppOptions
{
    DataRoot = dataRoot, IsDevelopment = isDev,
    RequireTotpForApprovers = builder.Configuration.GetValue("Mecmat:RequireTotpForApprovers", true),
    IpSalt = Convert.ToBase64String(KeyFile.LoadOrCreate(Path.Combine(dataRoot, "ip.salt"), 16)),
};

builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://127.0.0.1:5180");
builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = FileStorage.MaxBytes + 1_000_000);
builder.Services.AddSingleton(options);
// Antiforgery and other ASP.NET protections need stable keys; the service user has no home
// directory, so keep them with the data (and therefore in the backups).
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataRoot, "dp-keys")))
    .SetApplicationName("mecmat-cekmutt");
if (!isDev) builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Services.AddSingleton<LoginThrottle>();
builder.Services.AddScoped<AuditContext>();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<AccessService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ApprovalEngine>();
builder.Services.AddScoped<IApprovalHandler, MoneyRequestApprovalHandler>();
builder.Services.AddScoped<IApprovalHandler, BookingApprovalHandler>();
builder.Services.AddScoped<FinanceService>();
builder.Services.AddScoped<FileStorage>();
builder.Services.AddScoped<PublicQueryService>();
builder.Services.AddScoped<TimelineService>();
builder.Services.AddHostedService<ReminderWorker>();
builder.Services.AddHttpClient("crossref", c =>
{
    c.BaseAddress = new Uri("https://api.crossref.org/");
    c.Timeout = TimeSpan.FromSeconds(15);
    // Crossref asks API clients to identify themselves; requests with a contact go to the faster pool.
    c.DefaultRequestHeaders.UserAgent.ParseAdd("MECMAT-WebApp/1.0 (+https://ce.kmutt.ac.th; mailto:kasan.cha@kmutt.ac.th)");
});
builder.Services.AddDbContext<AppDbContext>(o => o
    .UseSqlite($"Data Source={Path.Combine(dataRoot, "mecmat.db")}")
    .AddInterceptors(new SqlitePragmaInterceptor()));
builder.Services.AddControllers(o => o.Filters.Add<AppExceptionFilter>())
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddRazorPages();
// Emit Thai as characters rather than &#xE01; entities: pages are half the size and readable in source.
builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(o =>
    o.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All));
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    // Caddy on the same machine is the only proxy; trust it and nothing else.
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownProxies.Add(System.Net.IPAddress.Loopback);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
    // `Mecmat.Server reset-admin <username>` (run by hand on the server) seeds a fresh database if
    // needed and prints a one-time password to that terminal only.
    if (args.Length >= 2 && args[0] == "reset-admin")
    {
        await Seeder.RunAsync(db, app.Logger);
        var otp = await Seeder.ResetPasswordAsync(db, args[1]);
        Console.WriteLine($"One-time password for {args[1]}: {otp}");
        return;
    }
    var first = await Seeder.RunAsync(db, app.Logger);
    // In production the service's console is the systemd journal, which is kept on disk: never
    // print a password there. The installer runs reset-admin in the admin's own terminal instead.
    if (first != null && !isDev)
        app.Logger.LogWarning("Database seeded. Run `Mecmat.Server reset-admin kasan.cha` as the mecmat user to get the first password.");
    if (first != null && isDev)
    {
        // Shown once, on the console of whoever started the first run; only its hash is stored.
        Console.WriteLine();
        Console.WriteLine("================================================================");
        Console.WriteLine($"  First admin: kasan.cha   one-time password: {first}");
        Console.WriteLine("  It must be changed at first sign-in.");
        Console.WriteLine("================================================================");
        Console.WriteLine();
    }
}

app.UseForwardedHeaders();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseExceptionHandler(e => e.Run(async ctx =>
{
    // Never echo internals: the reference lets the admin find the stack trace in the journal.
    var refId = Tokens.NewToken(6);
    var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    ctx.RequestServices.GetRequiredService<ILogger<Program>>().LogError(ex, "Unhandled error {Ref}", refId);
    ctx.Response.StatusCode = 500;
    if (ctx.Request.Path.StartsWithSegments("/api")) await ctx.Response.WriteAsJsonAsync(new { error = "errors.server", reference = refId });
    else await ctx.Response.WriteAsync("Server error. Reference: " + refId);
}));
app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = c =>
    {
        // App code changes with each deploy; fonts and images do not.
        var p = c.File.Name;
        c.Context.Response.Headers.CacheControl = p.EndsWith(".woff2") || p.EndsWith(".png") || p.EndsWith(".svg") || p.EndsWith(".jpg")
            ? "public, max-age=604800" : "no-cache";
    },
});
// The /en prefix is stripped before routing, so routing must run after it (WebApplication would
// otherwise insert UseRouting at the very start and match "/en/…" against nothing).
app.UseMiddleware<Mecmat.Site.LanguageMiddleware>();
app.UseRouting();
app.UseMiddleware<SessionMiddleware>();
app.MapGet("/api/health", () => Results.Ok(new { ok = true, build = BuildInfo.Version }));
app.MapGet("/app", () => Results.File(Path.Combine(app.Environment.WebRootPath, "app", "index.html"), "text/html; charset=utf-8"));
app.MapControllers();
app.MapRazorPages();
app.MapGet("/robots.txt", (HttpContext ctx) =>
    Results.Text($"User-agent: *\nDisallow: /app\nDisallow: /api/\nSitemap: {ctx.Request.Scheme}://{ctx.Request.Host}/sitemap.xml\n", "text/plain"));
app.MapGet("/sitemap.xml", async (HttpContext ctx, PublicQueryService q) =>
{
    var root = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
    var paths = new List<string> { "/", "/about", "/research", "/people", "/projects", "/publications", "/news", "/join", "/facilities", "/partners", "/contact", "/privacy" };
    paths.AddRange((await q.PeopleAsync()).Select(p => "/people/" + p.Slug));
    paths.AddRange((await q.ProjectsAsync()).Select(p => "/projects/" + p.Id));
    paths.AddRange((await q.NewsAsync(500)).Select(n => "/news/" + Uri.EscapeDataString(n.Slug)));
    var sb = new System.Text.StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n");
    foreach (var p in paths)
        foreach (var prefix in new[] { "", "/en" })
            sb.Append("<url><loc>").Append(System.Security.SecurityElement.Escape(root + prefix + (prefix.Length > 0 && p == "/" ? "" : p))).Append("</loc></url>\n");
    sb.Append("</urlset>\n");
    return Results.Text(sb.ToString(), "application/xml");
});
app.Run();

public static class BuildInfo
{
    /// <summary>Set by deploy/publish.sh from the git commit, so /api/health tells which code is live.</summary>
    public static readonly string Version =
        typeof(BuildInfo).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>().FirstOrDefault()?.InformationalVersion ?? "dev";
}
