using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AceJobAgencyPortal.Model;
using AceJobAgencyPortal.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AceJobAgencyPortal.Pages
{
    public class Verify2FAModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AuditLogger _audit;

        public Verify2FAModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            AuditLogger audit)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _audit = audit;
        }

        // only for display / convenience (not used for security decisions)
        [BindProperty(SupportsGet = true)]
        public string Email { get; set; } = "";

        [BindProperty, Required(ErrorMessage = "OTP is required.")]
        public string Code { get; set; } = "";

        public async Task<IActionResult> OnGetAsync()
        {
            // ✅ MUST be in 2FA pending state
            var twoFaUser = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (twoFaUser == null)
                return RedirectToPage("/Login");

            // show email on the page (optional)
            Email = twoFaUser.Email ?? Email;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            // ✅ MUST get the 2FA pending user
            var twoFaUser = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (twoFaUser == null)
            {
                ModelState.AddModelError("", "Your verification session expired. Please login again.");
                return Page();
            }

            // ✅ Verify OTP and complete sign-in
            var result = await _signInManager.TwoFactorSignInAsync(
                TokenOptions.DefaultEmailProvider,
                Code,
                isPersistent: false,
                rememberClient: false
            );

            if (!result.Succeeded)
            {
                await _audit.LogAsync("LOGIN_2FA_FAIL", twoFaUser.Id);
                ModelState.AddModelError("", "Invalid OTP code.");
                return Page();
            }

            await _audit.LogAsync("LOGIN_2FA_SUCCESS", twoFaUser.Id);

            // ✅ Now apply your single-session sid claim
            twoFaUser.ActiveSessionId = Guid.NewGuid().ToString("N");
            await _userManager.UpdateAsync(twoFaUser);

            // Re-issue cookie with sid claim
            await _signInManager.SignOutAsync();
            await _signInManager.SignInWithClaimsAsync(
                twoFaUser,
                isPersistent: false,
                additionalClaims: new[] { new Claim("sid", twoFaUser.ActiveSessionId!) }
            );

            return RedirectToPage("/Index");
        }
    }
}
