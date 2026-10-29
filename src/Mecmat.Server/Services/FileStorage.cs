using System.Security.Cryptography;
using Mecmat.Data;
using Mecmat.Domain;

namespace Mecmat.Services;

/// <summary>Uploads live in DataRoot/uploads under GUID names, never under wwwroot, so nothing a
/// user uploads can be served as part of the site itself.</summary>
public sealed class FileStorage(AppOptions opt, AppDbContext db)
{
    public const long MaxBytes = 100L * 1024 * 1024;

    public static readonly Dictionary<string, string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf", [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".doc"] = "application/msword", [".xls"] = "application/vnd.ms-excel", [".ppt"] = "application/vnd.ms-powerpoint",
        [".txt"] = "text/plain", [".csv"] = "text/csv", [".png"] = "image/png", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg",
        [".webp"] = "image/webp", [".gif"] = "image/gif", [".zip"] = "application/zip", [".tex"] = "text/plain",
        [".bib"] = "text/plain", [".dwg"] = "application/acad", [".dxf"] = "application/dxf", [".mp4"] = "video/mp4",
        [".svg"] = "image/svg+xml", [".json"] = "application/json", [".m"] = "text/plain", [".py"] = "text/plain",
    };

    /// <summary>Types a browser could execute or render as a page: always sent as a download under
    /// a sandbox CSP, never inline.</summary>
    public static readonly HashSet<string> NeverInline = new(StringComparer.OrdinalIgnoreCase) { ".svg", ".html", ".htm", ".xml", ".json" };

    public async Task<FileBlob> SaveAsync(IFormFile file, bool isPublic)
    {
        var ext = Path.GetExtension(file.FileName);
        if (!Allowed.TryGetValue(ext, out var type)) throw new AppException("files.typeNotAllowed", 400, new { ext });
        if (file.Length <= 0 || file.Length > MaxBytes) throw new AppException("files.tooLarge");
        Directory.CreateDirectory(opt.UploadsPath);
        var stored = Guid.NewGuid().ToString("N") + ext.ToLowerInvariant();
        var path = Path.Combine(opt.UploadsPath, stored);
        await using (var fs = File.Create(path))
            await file.CopyToAsync(fs);
        string sha;
        await using (var fs = File.OpenRead(path))
            sha = Convert.ToHexString(await SHA256.HashDataAsync(fs));
        var blob = new FileBlob
        {
            StoredName = stored, OriginalName = Path.GetFileName(file.FileName), ContentType = type, SizeBytes = file.Length, Sha256 = sha, IsPublic = isPublic,
        };
        db.Add(blob);
        await db.SaveChangesAsync();
        return blob;
    }

    public string PathOf(FileBlob b) => Path.Combine(opt.UploadsPath, b.StoredName);
}
