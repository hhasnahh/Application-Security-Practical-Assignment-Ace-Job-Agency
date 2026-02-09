using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace AceJobAgencyPortal.Pages
{
    public class ErrorModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int Code { get; set; } = 500;

        public string? TraceId { get; private set; }

        public void OnGet()
        {
            TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        }
    }
}
