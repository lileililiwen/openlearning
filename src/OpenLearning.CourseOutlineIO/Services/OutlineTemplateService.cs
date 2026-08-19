using ClosedXML.Excel;

namespace OpenLearning.CourseOutlineIO.Services;

/// <summary>Builds the downloadable outline workbook template.</summary>
public static class OutlineTemplateService
{
    public static byte[] GetTemplateBytes()
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Outline");
            sheet.Cell(1, 1).Value = "ModuleTitle";
            sheet.Cell(1, 2).Value = "ModuleOrder";
            sheet.Cell(1, 3).Value = "LessonTitle";
            sheet.Cell(1, 4).Value = "LessonOrder";
            sheet.Cell(1, 5).Value = "LessonContentUrl";
            sheet.Cell(2, 1).Value = "Module 1";
            sheet.Cell(2, 2).Value = 1;
            sheet.Cell(2, 3).Value = "Lesson 1";
            sheet.Cell(2, 4).Value = 1;
            sheet.Cell(2, 5).Value = "https://example.com/lecture.mp4";
            sheet.Columns().AdjustToContents();
            workbook.SaveAs(stream);
        }

        stream.Position = 0;
        return stream.ToArray();
    }
}
