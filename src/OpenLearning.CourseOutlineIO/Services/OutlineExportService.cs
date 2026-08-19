using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;

namespace OpenLearning.CourseOutlineIO.Services;

/// <summary>Excel export of a course outline (modules + lessons, metadata only).</summary>
public class OutlineExportService
{
    private readonly DbContext _db;

    public OutlineExportService(DbContext db)
    {
        _db = db;
    }

    public async Task<(byte[]? Bytes, string? Error)> ExportAsync(int courseId, string ownerId, bool isAdmin)
    {
        if (!isAdmin &&
            !await _db.Set<Course>().AnyAsync(c => c.Id == courseId && c.InstructorId == ownerId))
        {
            return (null, "您不是该课程的所有者，无法导出大纲。");
        }

        var modules = await _db.Set<Module>().AsNoTracking()
            .Where(m => m.CourseId == courseId)
            .OrderBy(m => m.OrderIndex)
            .ThenBy(m => m.Id)
            .ToListAsync();
        var moduleIds = modules.Select(m => m.Id).ToList();
        var lessons = moduleIds.Count == 0
            ? new List<Lesson>()
            : await _db.Set<Lesson>().AsNoTracking()
                .Where(l => moduleIds.Contains(l.ModuleId))
                .OrderBy(l => l.ModuleId)
                .ThenBy(l => l.OrderIndex)
                .ThenBy(l => l.Id)
                .ToListAsync();
        var lessonsByModule = lessons.GroupBy(l => l.ModuleId).ToDictionary(g => g.Key, g => g.ToList());

        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Outline");
            sheet.Cell(1, 1).Value = "ModuleTitle";
            sheet.Cell(1, 2).Value = "ModuleOrder";
            sheet.Cell(1, 3).Value = "LessonTitle";
            sheet.Cell(1, 4).Value = "LessonOrder";
            sheet.Cell(1, 5).Value = "LessonContentUrl";

            var row = 2;
            foreach (var module in modules)
            {
                var moduleLessons = lessonsByModule.GetValueOrDefault(module.Id) ?? new List<Lesson>();
                if (moduleLessons.Count == 0)
                {
                    // Preserve empty modules as module-only rows.
                    sheet.Cell(row, 1).Value = module.Title;
                    sheet.Cell(row, 2).Value = module.OrderIndex;
                    row++;
                    continue;
                }

                foreach (var lesson in moduleLessons)
                {
                    sheet.Cell(row, 1).Value = module.Title;
                    sheet.Cell(row, 2).Value = module.OrderIndex;
                    sheet.Cell(row, 3).Value = lesson.Title;
                    sheet.Cell(row, 4).Value = lesson.OrderIndex;
                    sheet.Cell(row, 5).Value = lesson.ContentUrlRef ?? string.Empty;
                    row++;
                }
            }

            sheet.Columns().AdjustToContents();
            workbook.SaveAs(stream);
        }

        stream.Position = 0;
        return (stream.ToArray(), null);
    }
}
