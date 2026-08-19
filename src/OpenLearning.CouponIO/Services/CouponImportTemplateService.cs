using ClosedXML.Excel;

namespace OpenLearning.CouponIO.Services;

/// <summary>Builds the downloadable coupon bulk-import template.</summary>
public static class CouponImportTemplateService
{
    public static byte[] GetTemplateBytes()
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Coupons");
            sheet.Cell(1, 1).Value = "Code";
            sheet.Cell(1, 2).Value = "DiscountType";
            sheet.Cell(1, 3).Value = "DiscountValue";
            sheet.Cell(1, 4).Value = "ValidFrom";
            sheet.Cell(1, 5).Value = "ValidTo";
            sheet.Cell(1, 6).Value = "MaxRedemptions";
            sheet.Cell(2, 1).Value = "SUMMER10";
            sheet.Cell(2, 2).Value = "Percent";
            sheet.Cell(2, 3).Value = 10;
            sheet.Cell(2, 4).Value = new DateTime(DateTime.Today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            sheet.Cell(2, 5).Value = new DateTime(DateTime.Today.Year, 12, 31, 0, 0, 0, DateTimeKind.Utc);
            sheet.Cell(2, 6).Value = 100;
            sheet.Columns().AdjustToContents();
            workbook.SaveAs(stream);
        }

        stream.Position = 0;
        return stream.ToArray();
    }
}
