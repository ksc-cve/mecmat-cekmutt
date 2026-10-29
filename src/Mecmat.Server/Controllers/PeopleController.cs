using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mecmat.Auth;
using Mecmat.Data;
using Mecmat.Domain;
using Mecmat.Services;

namespace Mecmat.Controllers;

[ApiController, Route("api/people")]
public sealed class PeopleController(AppDbContext db, CurrentUser cu, TimelineService timeline) : ControllerBase
{
    /// <summary>The LinkedIn-style view: everything dated about a person, plus advisors, students,
    /// co-authors and organisations. Members see the full picture of anyone they may view.</summary>
    [HttpGet("{id}/career")]
    public async Task<IActionResult> Career(string id)
    {
        if (!cu.IsAuthenticated) throw new AppException("auth.required", 401);
        var scope = await Scope.LoadAsync(db, cu.UserId!, cu.PersonId!);
        if (!cu.Can(Modules.People, PermAction.View, scope.RelatedPersonIds.Contains(id))) throw new AppException("errors.forbidden", 403);
        if (!await db.Set<Person>().AnyAsync(p => p.Id == id)) throw new AppException("errors.notFound", 404);
        return Ok(await timeline.BuildAsync(id, publicOnly: false));
    }
}
