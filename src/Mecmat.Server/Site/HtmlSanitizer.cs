using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Mecmat.Site;

/// <summary>Allow-list sanitiser for rich text shown on public pages (news bodies, biographies).
/// The CSP already blocks scripts; this keeps markup and attributes to a harmless set as well, so
/// an editor's paste cannot restyle or frame the site.</summary>
public static partial class HtmlSanitizer
{
    private static readonly HashSet<string> Allowed =
        ["p", "br", "b", "strong", "i", "em", "u", "ul", "ol", "li", "h3", "h4", "a", "blockquote", "sub", "sup"];

    public static string Clean(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";
        var sb = new StringBuilder();
        var pos = 0;
        foreach (Match m in Tag().Matches(html))
        {
            sb.Append(Text(html[pos..m.Index]));
            pos = m.Index + m.Length;
            var closing = m.Groups[1].Value == "/";
            var name = m.Groups[2].Value.ToLowerInvariant();
            if (!Allowed.Contains(name)) continue;
            if (closing) { sb.Append("</").Append(name).Append('>'); continue; }
            if (name == "a")
            {
                var href = Href().Match(m.Groups[3].Value);
                var url = href.Success ? WebUtility.HtmlDecode(href.Groups[2].Value.Trim()) : "";
                if (Regex.IsMatch(url, "^(https?:|mailto:|/|#)", RegexOptions.IgnoreCase))
                    sb.Append("<a href=\"").Append(WebUtility.HtmlEncode(url)).Append("\" rel=\"noopener noreferrer\">");
                else sb.Append("<a>");
                continue;
            }
            sb.Append('<').Append(name).Append(name == "br" ? " />" : ">");
        }
        sb.Append(Text(html[pos..]));
        return sb.ToString();
    }

    // Text between tags is decoded and re-encoded, so a stray "<" or an entity trick cannot open a tag.
    private static string Text(string s) => WebUtility.HtmlEncode(WebUtility.HtmlDecode(s));

    [GeneratedRegex(@"<\s*(/?)\s*([a-zA-Z0-9]+)([^>]*)>", RegexOptions.Singleline)]
    private static partial Regex Tag();

    [GeneratedRegex("href\\s*=\\s*([\"'])(.*?)\\1", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex Href();
}
