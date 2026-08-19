using ClosedXML.Excel;

namespace OpenLearning.QuestionIO.Services;

/// <summary>Builds the downloadable .xlsx import template.</summary>
public static class QuestionTemplateService
{
    private static readonly string[] _headers =
    [
        "RowId(可选)", "QuestionType", "Stem", "OptionA", "OptionB", "OptionC", "OptionD",
        "CorrectAnswer", "Explanation", "Difficulty", "KnowledgeTag",
    ];

    public static byte[] GetTemplateBytes(bool includeBankTopic)
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Questions");
            for (var i = 0; i < _headers.Length; i++)
            {
                sheet.Cell(1, i + 1).Value = _headers[i];
            }

            if (includeBankTopic)
            {
                sheet.Cell(1, _headers.Length + 1).Value = "BankTopic";
            }

            var col = 1;
            sheet.Cell(2, col++).Value = "q-001";
            sheet.Cell(2, col++).Value = "SingleChoice";
            sheet.Cell(2, col++).Value = "Which planet is closest to the Sun?";
            sheet.Cell(2, col++).Value = "Mercury";
            sheet.Cell(2, col++).Value = "Venus";
            sheet.Cell(2, col++).Value = "Earth";
            sheet.Cell(2, col++).Value = "Mars";
            sheet.Cell(2, col++).Value = "A";
            sheet.Cell(2, col++).Value = "Mercury is the innermost planet.";
            sheet.Cell(2, col++).Value = "Easy";
            sheet.Cell(2, col++).Value = "Astronomy";
            if (includeBankTopic)
            {
                sheet.Cell(2, col).Value = "General Science";
            }

            sheet.Columns().AdjustToContents();
            workbook.SaveAs(stream);
        }

        return stream.ToArray();
    }
}
