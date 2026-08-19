using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CouponIO.Services;

namespace OpenLearning.Web.Pages.Admin.Coupons;

[Authorize(Policy = Policies.RequireAdmin)]
public class TemplateModel : PageModel
{
    private const string _contentTypeXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public IActionResult OnGet()
    {
        return File(CouponImportTemplateService.GetTemplateBytes(), _contentTypeXlsx, "coupon-import-template.xlsx");
    }
}
