using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AceJobAgencyPortal.Model;
using AceJobAgencyPortal.Services;
using System.ComponentModel.DataAnnotations;

namespace AceJobAgencyPortal.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditLogger _audit;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, AuditLogger audit)
        {
            _userManager = userManager;
            _audit = audit;
        }

        [BindProperty, Required, EmailAddress]
        public string Email { get; set; } = "";

        public string? DemoResetLink { get; private set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await _userManager.FindByEmailAsync(Email);

            // Security: do not reveal if email exists
            if (user == null)
            {
                await _audit.LogAsync("RESET_REQUEST", null, "unknown email");
                return Page();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            DemoResetLink = Url.Page("/ResetPassword", null, new { email = Email, token }, Request.Scheme);

            await _audit.LogAsync("RESET_LINK_GENERATED", user.Id);
            return Page();
        }
    }
}
