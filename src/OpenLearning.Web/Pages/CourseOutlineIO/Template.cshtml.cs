using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.CourseOutlineIO.Services;

namespace OpenLearning.Web.Pages.CourseOutlineIO;

[Authorize(Policy = OpenLearning.Auth.Policies.RequireInstructorOrAdmin)]
public class TemplateModel : PageModel
{
    private const string _contentTypeXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public IActionResult OnGet()
    {
        return File(OutlineTemplateService.GetTemplateBytes(), _contentTypeXlsx, "course-outline-template.xlsx");
    }
}
