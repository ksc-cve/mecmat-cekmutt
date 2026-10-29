using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mecmat.Auth;
using Mecmat.Data;
using Mecmat.Domain;
using Mecmat.Services;

namespace Mecmat.Controllers;

[ApiController, Route("api/files")]
public sealed class FilesController(AppDbContext db, CurrentUser cu, FileStorage files) : ControllerBase
{
    [HttpPost, RequestSizeLimit(FileStorage.MaxBytes + 1_000_000)]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] bool isPublic = false,
        [FromForm] string? ownerType = null, [FromForm] string? ownerId = null, [FromForm] string? label = null)
    {
        if (!cu.IsAuthenticated) throw new AppException("auth.required", 401);
        // Images for public pages (photos, covers) are marked public at upload; only images may be.
        if (isPublic && !file.ContentType.StartsWith("image/", StringComparison.Ordinal)) isPublic = false;
        var blob = await files.SaveAsync(file, isPublic);
        if (ownerType != null && ownerId != null)
        {
            db.Add(new Attachment { OwnerType = ownerType, OwnerId = ownerId, FileBlobId = blob.Id, Label = label });
            await db.SaveChangesAsync();
        }
        return Ok(new { blob.Id, blob.OriginalName, blob.ContentType, blob.SizeBytes });
    }

    /// <summary>New version of a document: the old file stays, the document points at the new one.</summary>
    [HttpPost("documents/{documentId}/versions"), RequestSizeLimit(FileStorage.MaxBytes + 1_000_000)]
    public async Task<IActionResult> AddVersion(string documentId, IFormFile file, [FromForm] string? changeNote)
    {
        if (!cu.IsAuthenticated) throw new AppException("auth.required", 401);
        var doc = await db.Set<Document>().FirstOrDefaultAsync(d => d.Id == documentId && d.RecordStatus == RecordStatus.Active)
                  ?? throw new AppException("errors.notFound", 404);
        var scope = await Scope.LoadAsync(db, cu.UserId!, cu.PersonId!);
        var related = scope.DocumentIds.Contains(doc.Id) || doc.OwnerPersonId == cu.PersonId;
        if (!cu.Can(Modules.Documents, PermAction.Edit, related)) throw new AppException("errors.forbidden", 403);
        var blob = await files.SaveAsync(file, false);
        var next = (await db.Set<DocumentVersion>().Where(v => v.DocumentId == doc.Id).MaxAsync(v => (int?)v.VersionNo) ?? 0) + 1;
        var ver = new DocumentVersion { DocumentId = doc.Id, VersionNo = next, FileBlobId = blob.Id, ChangeNote = changeNote };
        db.Add(ver);
        await db.SaveChangesAsync();
        doc.CurrentVersionId = ver.Id;
        await db.SaveChangesAsync();
        return Ok(new { ver.Id, ver.VersionNo, blob.OriginalName, blob.SizeBytes });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Download(string id, [FromQuery] bool inline = false)
    {
        var blob = await db.Set<FileBlob>().AsNoTracking().FirstOrDefaultAsync(b => b.Id == id && b.RecordStatus == RecordStatus.Active)
                   ?? throw new AppException("errors.notFound", 404);
        if (!blob.IsPublic) await RequireAccessAsync(blob);
        var path = files.PathOf(blob);
        if (!System.IO.File.Exists(path)) throw new AppException("errors.notFound", 404);
        var ext = Path.GetExtension(blob.StoredName);
        var asAttachment = !inline || FileStorage.NeverInline.Contains(ext);
        Response.Headers.ContentSecurityPolicy = "sandbox; default-src 'none'; img-src 'self'; style-src 'unsafe-inline'";
        Response.Headers.CacheControl = blob.IsPublic ? "public, max-age=86400" : "private, no-store";
        return asAttachment
            ? PhysicalFile(path, blob.ContentType, blob.OriginalName, enableRangeProcessing: true)
            : PhysicalFile(path, blob.ContentType, enableRangeProcessing: true);
    }

    /// <summary>Ids are sequential and therefore guessable, so a non-public file is served only to its
    /// uploader, an admin, or someone who can see the record it belongs to.</summary>
    private async Task RequireAccessAsync(FileBlob blob)
    {
        if (!cu.IsAuthenticated) throw new AppException("auth.required", 401);
        if (cu.IsAdmin || blob.CreatedBy == cu.UserId) return;
        var scope = await Scope.LoadAsync(db, cu.UserId!, cu.PersonId!);
        var version = await db.Set<DocumentVersion>().AsNoTracking().FirstOrDefaultAsync(v => v.FileBlobId == blob.Id);
        if (version != null)
        {
            if (cu.Can(Modules.Documents, PermAction.View, scope.DocumentIds.Contains(version.DocumentId))) return;
            throw new AppException("errors.forbidden", 403);
        }
        var att = await db.Set<Attachment>().AsNoTracking().FirstOrDefaultAsync(a => a.FileBlobId == blob.Id);
        if (att != null)
        {
            var ok = att.OwnerType switch
            {
                nameof(MoneyRequest) => await db.Set<MoneyRequest>().AnyAsync(r => r.Id == att.OwnerId &&
                    (r.RequesterPersonId == cu.PersonId || scope.ApprovalSubjectIds.Contains(r.Id) || scope.AccountIds.Contains(r.AccountId)))
                    || cu.CanAll(Modules.Finance, PermAction.View),
                nameof(TaskItem) => cu.Can(Modules.Tasks, PermAction.View, scope.TaskIds.Contains(att.OwnerId)),
                nameof(Meeting) => cu.Can(Modules.Meetings, PermAction.View, scope.MeetingIds.Contains(att.OwnerId)),
                nameof(Project) => cu.Can(Modules.Projects, PermAction.View, scope.ProjectIds.Contains(att.OwnerId)),
                nameof(Publication) => cu.Can(Modules.Publications, PermAction.View, scope.PublicationIds.Contains(att.OwnerId)),
                nameof(Asset) => cu.CanAny(Modules.Assets, PermAction.View),
                nameof(Person) => cu.Can(Modules.People, PermAction.View, scope.RelatedPersonIds.Contains(att.OwnerId)),
                _ => cu.CanAll(Modules.Documents, PermAction.View),
            };
            if (ok) return;
            throw new AppException("errors.forbidden", 403);
        }
        // Applications' CVs and certificate files are referenced directly by id.
        if (await db.Set<Application>().AnyAsync(a => a.CvFileId == blob.Id))
        {
            if (cu.CanAll(Modules.Inbox, PermAction.View)) return;
            throw new AppException("errors.forbidden", 403);
        }
        // Remaining references (photos, logos, covers before they are published) are fine for any
        // signed-in member to see.
    }
}
