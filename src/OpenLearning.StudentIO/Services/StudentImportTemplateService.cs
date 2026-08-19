using ClosedXML.Excel;

namespace OpenLearning.StudentIO.Services;

/// <summary>Builds the downloadable .xlsx student-import template.</summary>
public static class StudentImportTemplateService
{
    private static readonly string[] _headers =
    [
        "Action", "Email", "Phone", "DisplayName", "Password", "CourseIds", "ClassGroupIds",
    ];

    public static byte[] GetTemplateBytes()
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Students");
            for (var i = 0; i < _headers.Length; i++)
            {
                sheet.Cell(1, i + 1).Value = _headers[i];
            }

            sheet.Cell(2, 1).Value = "CreateAndEnroll";
            sheet.Cell(2, 2).Value = "student@example.com";
            sheet.Cell(2, 3).Value = "13800000000";
            sheet.Cell(2, 4).Value = "张同学";
            sheet.Cell(2, 5).Value = string.Empty;
            sheet.Cell(2, 6).Value = "1;2";
            sheet.Cell(2, 7).Value = string.Empty;

            sheet.Columns().AdjustToContents();
            workbook.SaveAs(stream);
        }

        return stream.ToArray();
    }
}
