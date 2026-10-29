using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mecmat.Services;

/// <summary>An expected refusal with a t() key the UI can show as-is, e.g. "finance.insufficient".</summary>
public sealed class AppException(string key, int status = 400, object? detail = null) : Exception(key)
{
    public string Key { get; } = key;
    public int Status { get; } = status;
    public object? Detail { get; } = detail;
}

public sealed class AppExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext ctx)
    {
        switch (ctx.Exception)
        {
            case AppException a:
                ctx.Result = new ObjectResult(new { error = a.Key, detail = a.Detail }) { StatusCode = a.Status };
                ctx.ExceptionHandled = true;
                break;
            case ApprovalException ap:
                ctx.Result = new ObjectResult(new { error = ap.Key }) { StatusCode = 400 };
                ctx.ExceptionHandled = true;
                break;
            case Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException:
                ctx.Result = new ObjectResult(new { error = "errors.conflict" }) { StatusCode = 409 };
                ctx.ExceptionHandled = true;
                break;
        }
    }
}
