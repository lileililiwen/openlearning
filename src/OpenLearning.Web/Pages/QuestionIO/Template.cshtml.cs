using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.QuestionIO.Services;

namespace OpenLearning.Web.Pages.QuestionIO;

[Authorize(Policy = Policies.RequireInstructorOrAdmin)]
public class TemplateModel : PageModel
{
    public IActionResult OnGet(bool bank)
    {
        var bytes = QuestionTemplateService.GetTemplateBytes(bank);
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            bank ? "question-bank-import-template.xlsx" : "question-import-template.xlsx");
    }
}
