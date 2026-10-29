using Microsoft.AspNetCore.Mvc.RazorPages;
using Mecmat.Services;

namespace Mecmat.Site;

/// <summary>Base for every public page: language (from the /en prefix), site settings and the
/// helpers the layout needs. Data comes only through <see cref="PublicQueryService"/>.</summary>
public abstract class SitePageModel(PublicQueryService q) : PageModel
{
    protected PublicQueryService Q { get; } = q;
    public bool En => HttpContext.Items["lang"] as string == "en";
    public Dictionary<string, string?> Settings { get; private set; } = new();
    public string? PageTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? OgImage { get; set; }

    public string L(string key) => SiteText.Get(key, En);
    public string Pick(string? th, string? en) => En ? (string.IsNullOrWhiteSpace(en) ? th ?? "" : en) : (string.IsNullOrWhiteSpace(th) ? en ?? "" : th);
    public string S(string key) => Settings.TryGetValue(key, out var v) ? v ?? "" : "";
    /// <summary>A site path in the current language: "/people" or "/en/people".</summary>
    public string Href(string path) => En ? "/en" + (path == "/" ? "" : path) : path;
    /// <summary>The same page in the other language.</summary>
    public string OtherLangHref()
    {
        var p = HttpContext.Request.Path.Value ?? "/";
        var q = HttpContext.Request.QueryString.Value;
        return (En ? (p.Length == 0 ? "/" : p) : "/en" + (p == "/" ? "" : p)) + q;
    }
    public string Year(int? y) => y == null ? "" : (En ? y.Value.ToString() : (y.Value + 543).ToString());
    public string Date(DateOnly? d)
    {
        if (d == null) return "";
        string[] th = ["ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค."];
        string[] en = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
        return $"{d.Value.Day} {(En ? en : th)[d.Value.Month - 1]} {Year(d.Value.Year)}";
    }
    public string Date(DateTime? t) => t == null ? "" : Date(DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(t.Value, Clock.Bangkok)));
    public string Time(DateTime? t) => t == null ? "" : TimeZoneInfo.ConvertTimeFromUtc(t.Value, Clock.Bangkok).ToString("HH:mm");
    public static string Img(string? fileId) => fileId == null ? "" : $"/api/files/{fileId}?inline=1";

    public override async Task OnPageHandlerExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext context,
        Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutionDelegate next)
    {
        Settings = await Q.SettingsAsync();
        await next();
    }
}
