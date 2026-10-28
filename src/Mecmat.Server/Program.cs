using Microsoft.EntityFrameworkCore;
using Mecmat.Auth;
using Mecmat.Data;

var builder = WebApplication.CreateBuilder(args);

var dataRoot = builder.Configuration["Mecmat:DataRoot"]
               ?? (builder.Environment.IsDevelopment()
                   ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "mecmat-data")
                   : "/var/lib/mecmat-cekmutt");
Directory.CreateDirectory(dataRoot);
FieldCipher.Initialise(dataRoot);

builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://127.0.0.1:5180");
builder.Services.AddScoped<AuditContext>();
builder.Services.AddDbContext<AppDbContext>(o => o
    .UseSqlite($"Data Source={Path.Combine(dataRoot, "mecmat.db")}")
    .AddInterceptors(new SqlitePragmaInterceptor()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
}

app.MapGet("/api/health", () => Results.Ok(new { ok = true, build = BuildInfo.Version }));
app.Run();

public static class BuildInfo
{
    public static readonly string Version =
        typeof(BuildInfo).Assembly.GetName().Version?.ToString() ?? "dev";
}
