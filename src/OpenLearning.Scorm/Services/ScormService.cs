using System.IO.Compression;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Scorm.Models;

namespace OpenLearning.Scorm.Services;

public class ScormService
{
    private readonly DbContext _db;

    public ScormService(DbContext db)
    {
        _db = db;
    }

    public Task<ScormPackage?> GetForLessonAsync(int lessonId)
        => _db.Set<ScormPackage>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.LessonId == lessonId);

    public Task<bool> IsLessonOwnerAsync(int lessonId, string userId)
        => _db.Set<Lesson>().AsNoTracking()
            .AnyAsync(l => l.Id == lessonId && l.Module!.Course!.InstructorId == userId);

    public async Task<(ScormPackage? Package, string? Error)> UploadAsync(
        int lessonId, string ownerId, string webRootPath, Stream zipStream, string fileName)
    {
        if (!await IsLessonOwnerAsync(lessonId, ownerId))
        {
            return (null, "You do not own this lesson's course.");
        }

        if (!fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return (null, "SCORM packages must be uploaded as a .zip file.");
        }

        string entryPoint;
        try
        {
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
            var manifestEntry = archive.GetEntry("imsmanifest.xml");
            if (manifestEntry is null)
            {
                return (null, "The package must contain imsmanifest.xml at its root.");
            }

            using var manifestStream = manifestEntry.Open();
            entryPoint = ParseEntryPoint(manifestStream);
            if (string.IsNullOrWhiteSpace(entryPoint))
            {
                return (null, "The manifest does not define a launchable item.");
            }

            var package = new ScormPackage
            {
                LessonId = lessonId,
                Title = Path.GetFileNameWithoutExtension(fileName),
                EntryPoint = entryPoint.Trim(),
            };
            _db.Set<ScormPackage>().Add(package);
            await _db.SaveChangesAsync();

            // Extract to wwwroot/scorm/<id>/ (path-traversal safe).
            var targetRoot = Path.Combine(webRootPath, "scorm", package.Id.ToString());
            Directory.CreateDirectory(targetRoot);
            foreach (var entry in archive.Entries)
            {
                var targetPath = SafeCombine(targetRoot, entry.FullName);
                if (targetPath is null)
                {
                    continue;
                }

                if (entry.FullName.EndsWith("/"))
                {
                    Directory.CreateDirectory(targetPath);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                using var destination = File.Create(targetPath);
                await using var source = entry.Open();
                await source.CopyToAsync(destination);
            }

            package.PackagePath = Path.Combine("scorm", package.Id.ToString()).Replace('\\', '/');
            await _db.SaveChangesAsync();
            return (package, null);
        }
        catch (InvalidDataException)
        {
            return (null, "The file is not a valid zip archive.");
        }
        catch (Exception ex)
        {
            return (null, $"Could not process the package: {ex.Message}");
        }
    }

    public async Task<bool> RemoveAsync(int packageId, string ownerId, string webRootPath)
    {
        var package = await _db.Set<ScormPackage>()
            .Include(p => p.Lesson).ThenInclude(l => l!.Module).ThenInclude(m => m!.Course)
            .FirstOrDefaultAsync(p => p.Id == packageId);
        if (package?.Lesson?.Module?.Course is null || package.Lesson.Module.Course.InstructorId != ownerId)
        {
            return false;
        }

        var folder = Path.Combine(webRootPath, "scorm", package.Id.ToString());
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }

        _db.Set<ScormPackage>().Remove(package);
        await _db.SaveChangesAsync();
        return true;
    }

    private static string? SafeCombine(string root, string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        var full = Path.GetFullPath(Path.Combine(root, normalized));
        var rootFull = Path.GetFullPath(root);
        if (!full.StartsWith(rootFull + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            return null;
        }

        return full;
    }

    private static string ParseEntryPoint(Stream manifestStream)
    {
        var doc = XDocument.Load(manifestStream);
        var item = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "item");
        if (item is null)
        {
            return string.Empty;
        }

        var identifierRef = item.Attribute("identifierref")?.Value;
        if (string.IsNullOrEmpty(identifierRef))
        {
            return string.Empty;
        }

        var resource = doc.Descendants().FirstOrDefault(e =>
            e.Name.LocalName == "resource" && e.Attribute("identifier")?.Value == identifierRef);
        var href = resource?.Attribute("href")?.Value;
        if (string.IsNullOrEmpty(href))
        {
            href = resource?.Elements()
                .FirstOrDefault(e => e.Name.LocalName == "file")?.Attribute("href")?.Value;
        }

        return href?.Trim() ?? string.Empty;
    }
}
