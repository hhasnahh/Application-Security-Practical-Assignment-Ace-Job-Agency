using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AceJobAgencyPortal.Model;
using AceJobAgencyPortal.Services;
using System.ComponentModel.DataAnnotations;

namespace AceJobAgencyPortal.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PasswordPolicyService _policy;
        private readonly AuditLogger _audit;

        public ResetPasswordModel(UserManager<ApplicationUser> userManager, PasswordPolicyService policy, AuditLogger audit)
        {
            _userManager = userManager;
            _policy = policy;
            _audit = audit;
        }

        [BindProperty(SupportsGet = true)]
        public string Email { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public string Token { get; set; } = "";

        [BindProperty, Required]
        public string NewPassword { get; set; } = "";

        [BindProperty, Required, Compare(nameof(NewPassword))]
        public string ConfirmPassword { get; set; } = "";

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid reset request.");
                return Page();
            }

            var reuse = await _policy.CheckNotReusedAsync(user, NewPassword);
            if (!reuse.ok)
            {
                ModelState.AddModelError("", reuse.msg);
                return Page();
            }

            await _policy.RecordHistoryAsync(user);

            var result = await _userManager.ResetPasswordAsync(user, Token, NewPassword);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                await _audit.LogAsync("RESET_PW_FAIL", user.Id);
                return Page();
            }

            user.PasswordLastChangedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            await _audit.LogAsync("RESET_PW_SUCCESS", user.Id);
            return RedirectToPage("/Login");
        }
    }
}
