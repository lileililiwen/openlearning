using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Payments.Models;
using OpenLearning.Payments.Services;

namespace OpenLearning.Web.Pages.Payments;

[Authorize]
public sealed class SandboxModel(PaymentService payments) : PageModel
{
    public PaymentIntent? Intent { get; private set; }
    public async Task OnGetAsync(Guid id)
    {
        Intent = await payments.GetAsync(id);
    }
}
