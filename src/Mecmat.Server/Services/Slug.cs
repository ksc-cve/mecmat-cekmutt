using System.Text;
using System.Text.RegularExpressions;

namespace Mecmat.Services;

public static partial class Slug
{
    /// <summary>URL-safe slug. Thai letters are kept (browsers handle them), everything else
    /// collapses to hyphens.</summary>
    public static string Make(string? text)
    {
        var s = (text ?? "").Trim().ToLowerInvariant().Normalize(NormalizationForm.FormC);
        s = NonSlug().Replace(s, "-").Trim('-');
        if (s.Length > 80) s = s[..80].Trim('-');
        return s.Length == 0 ? "item" : s;
    }

    [GeneratedRegex(@"[^a-z0-9฀-๿]+")]
    private static partial Regex NonSlug();
}

public static class Doi
{
    public static string? Normalise(string? doi)
    {
        if (string.IsNullOrWhiteSpace(doi)) return null;
        var d = doi.Trim();
        foreach (var p in new[] { "https://doi.org/", "http://doi.org/", "https://dx.doi.org/", "doi:" })
            if (d.StartsWith(p, StringComparison.OrdinalIgnoreCase)) d = d[p.Length..];
        return d.Trim();
    }
}
