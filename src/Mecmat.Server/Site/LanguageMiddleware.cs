namespace Mecmat.Site;

/// <summary>"/en/…" serves the English version of "/…". The prefix is stripped before routing so
/// each Razor page needs only one route, and the language travels in HttpContext.Items.</summary>
public sealed class LanguageMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext http)
    {
        var path = http.Request.Path;
        if (path.StartsWithSegments("/en", out var rest))
        {
            http.Items["lang"] = "en";
            http.Request.Path = rest.HasValue ? rest : "/";
        }
        else http.Items["lang"] = "th";
        return next(http);
    }
}
