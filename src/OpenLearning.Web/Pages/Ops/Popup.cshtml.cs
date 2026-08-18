using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Operations.Services;

namespace OpenLearning.Web.Pages.Ops;

/// <summary>Serves the active scheduled pop-up as JSON for the layout client.</summary>
public class PopupModel : PageModel
{
    private readonly OperationsService _operations;

    public PopupModel(OperationsService operations)
    {
        _operations = operations;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var popup = await _operations.GetActivePopupAsync();
        if (popup is null)
        {
            return new JsonResult(new { shown = false });
        }

        return new JsonResult(new
        {
            shown = true,
            title = popup.Title,
            body = popup.Body,
            linkUrl = popup.LinkUrl,
        });
    }
}
