using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mecmat.Auth;
using Mecmat.Data;
using Mecmat.Domain;
using Mecmat.Services;

namespace Mecmat.Controllers;

/// <summary>Names for pickers and labels. Every signed-in member may know who is in the group and
/// what projects and resources exist — names only, nothing private.</summary>
[ApiController, Route("api/lookup")]
public sealed class LookupController(AppDbContext db, CurrentUser cu) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> All()
    {
        if (!cu.IsAuthenticated) throw new AppException("auth.required", 401);
        var active = RecordStatus.Active;
        return Ok(new
        {
            people = await db.Set<Person>().AsNoTracking().Where(p => p.RecordStatus == active).OrderBy(p => p.DisplayOrder).ThenBy(p => p.FirstNameTh)
                .Select(p => new { p.Id, p.TitleTh, p.FirstNameTh, p.LastNameTh, p.FirstNameEn, p.LastNameEn, p.Nickname, p.HasDoctorate, p.AcademicRank, p.Category, p.Kind, p.PhotoFileId }).ToListAsync(),
            organizations = await db.Set<Organization>().AsNoTracking().Where(o => o.RecordStatus == active).OrderBy(o => o.NameTh)
                .Select(o => new { o.Id, o.NameTh, o.NameEn, o.ShortName, o.Kind }).ToListAsync(),
            projects = await db.Set<Project>().AsNoTracking().Where(p => p.RecordStatus == active).OrderByDescending(p => p.StartDate)
                .Select(p => new { p.Id, p.Code, p.TitleTh, p.TitleEn, p.Status, p.ParentProjectId, p.Kind }).ToListAsync(),
            accounts = await db.Set<Account>().AsNoTracking().Where(a => a.RecordStatus == active).OrderBy(a => a.Code)
                .Select(a => new { a.Id, a.Code, a.NameTh, a.NameEn, a.ParentAccountId, a.ProjectId }).ToListAsync(),
            resources = await db.Set<Resource>().AsNoTracking().Where(r => r.RecordStatus == active).OrderBy(r => r.NameTh)
                .Select(r => new { r.Id, r.NameTh, r.NameEn, r.Kind, r.Color, r.RequiresApproval, r.SlotMinutes, r.IsActive, r.Location, r.ManagerPersonId, r.OpenHoursJson, r.MinDurationMinutes, r.MaxDurationMinutes }).ToListAsync(),
            positions = await db.Set<GroupPosition>().AsNoTracking().Where(p => p.RecordStatus == active).OrderBy(p => p.Rank)
                .Select(p => new { p.Id, p.NameTh, p.NameEn, p.Rank }).ToListAsync(),
            researchAreas = await db.Set<ResearchArea>().AsNoTracking().Where(r => r.RecordStatus == active).OrderBy(r => r.DisplayOrder)
                .Select(r => new { r.Id, r.NameTh, r.NameEn }).ToListAsync(),
            folders = await db.Set<Folder>().AsNoTracking().Where(f => f.RecordStatus == active).OrderBy(f => f.Name)
                .Select(f => new { f.Id, f.Name, f.ParentFolderId, f.ProjectId }).ToListAsync(),
            materials = await db.Set<Material>().AsNoTracking().Where(m => m.RecordStatus == active).OrderBy(m => m.NameTh)
                .Select(m => new { m.Id, m.NameTh, m.NameEn, m.Unit }).ToListAsync(),
            assets = await db.Set<Asset>().AsNoTracking().Where(a => a.RecordStatus == active).OrderBy(a => a.NameTh)
                .Select(a => new { a.Id, a.NameTh, a.NameEn, a.AssetCode }).ToListAsync(),
            roles = await db.Set<Role>().AsNoTracking().Where(r => r.RecordStatus == active).OrderBy(r => r.Key)
                .Select(r => new { r.Id, r.Key, r.NameTh, r.NameEn }).ToListAsync(),
            publicFieldKeys = Crud.Registry.PublicFieldKeys,
        });
    }
}
