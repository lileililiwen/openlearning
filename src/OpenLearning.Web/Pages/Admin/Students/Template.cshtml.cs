using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.StudentIO.Services;

namespace OpenLearning.Web.Pages.Admin.Students;

[Authorize(Policy = Policies.RequireFinanceOrAdmin)]
public class TemplateModel : PageModel
{
    public IActionResult OnGet()
    {
        return File(
            StudentImportTemplateService.GetTemplateBytes(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "student-import-template.xlsx");
    }
}
