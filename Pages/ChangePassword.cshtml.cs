using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AceJobAgencyPortal.Model;
using AceJobAgencyPortal.Services;
using System.ComponentModel.DataAnnotations;

namespace AceJobAgencyPortal.Pages
{
    [Authorize]
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly PasswordPolicyService _policy;
        private readonly AuditLogger _audit;

        public ChangePasswordModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            PasswordPolicyService policy,
            AuditLogger audit)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _policy = policy;
            _audit = audit;
        }

        [BindProperty, Required]
        public string CurrentPassword { get; set; } = "";

        [BindProperty, Required]
        public string NewPassword { get; set; } = "";

        [BindProperty, Required, Compare(nameof(NewPassword))]
        public string ConfirmPassword { get; set; } = "";

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Login");

            var min = _policy.CheckMinAge(user);
            if (!min.ok)
            {
                ModelState.AddModelError("", min.msg);
                return Page();
            }

            var reuse = await _policy.CheckNotReusedAsync(user, NewPassword);
            if (!reuse.ok)
            {
                ModelState.AddModelError("", reuse.msg);
                return Page();
            }

            await _policy.RecordHistoryAsync(user);

            var result = await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                await _audit.LogAsync("CHANGE_PW_FAIL", user.Id);
                return Page();
            }

            user.PasswordLastChangedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            await _audit.LogAsync("CHANGE_PW_SUCCESS", user.Id);
            await _signInManager.RefreshSignInAsync(user);

            return RedirectToPage("/Index");
        }
    }
}
