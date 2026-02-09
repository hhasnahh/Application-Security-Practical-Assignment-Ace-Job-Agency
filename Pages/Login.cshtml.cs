using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AceJobAgencyPortal.Model;
using AceJobAgencyPortal.Services;
using System.Security.Claims;
using System.Diagnostics; 
// ✅ for Debug.WriteLine

namespace AceJobAgencyPortal.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditLogger _audit;
        private readonly RecaptchaV3 _recaptcha;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            AuditLogger audit,
            RecaptchaV3 recaptcha)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _audit = audit;
            _recaptcha = recaptcha;
        }

        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Password { get; set; } = "";

        [BindProperty]
        public string? RecaptchaToken { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ModelState.AddModelError("", "Email and Password are required.");
                return Page();
            }

            if (string.IsNullOrWhiteSpace(RecaptchaToken))
            {
                ModelState.AddModelError("", "reCAPTCHA token missing.");
                return Page();
            }

            // ✅ If your RecaptchaV3 returns tuple, this is fine.
            // If it returns an object instead, adjust like we did before.
            var (ok, score, err) = await _recaptcha.VerifyAsync(RecaptchaToken, "login");
            if (!ok)
            {
                ModelState.AddModelError("", $"reCAPTCHA failed ({err}), score={score:0.00}");
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                await _audit.LogAsync("LOGIN_FAIL", null, "unknown email");
                ModelState.AddModelError("", "Invalid login attempt.");
                return Page();
            }

            // ✅ Ensure 2FA is enabled for this user (in case older accounts exist)
            // (Safe: doesn't harm anything)
            // NOTE: If you don't want auto-enable here, remove this.
            // user.TwoFactorEnabled = true;
            // await _userManager.UpdateAsync(user);

            // ✅ Use the user overload for best 2FA behavior
            var result = await _signInManager.PasswordSignInAsync(
                user,
                Password,
                isPersistent: false,
                lockoutOnFailure: true
            );

            // ✅ 2FA required → generate OTP and go Verify2FA
            if (result.RequiresTwoFactor)
            {
                var code = await _userManager.GenerateTwoFactorTokenAsync(
                    user,
                    TokenOptions.DefaultEmailProvider
                );

                // ✅ Demo: shows in Visual Studio Output window (Debug)
                System.Diagnostics.Debug.WriteLine($"[2FA OTP for {user.Email}] {code}");
                return RedirectToPage("/Verify2FA", new { email = user.Email });
            }

            if (result.Succeeded)
            {
                await _audit.LogAsync("LOGIN_SUCCESS", user.Id);

                // Refresh ActiveSessionId + sign in with sid claim (multi-login detection)
                user.ActiveSessionId = Guid.NewGuid().ToString("N");
                await _userManager.UpdateAsync(user);

                await _signInManager.SignOutAsync();
                await _signInManager.SignInWithClaimsAsync(
                    user,
                    isPersistent: false,
                    additionalClaims: new[] { new Claim("sid", user.ActiveSessionId!) }
                );

                return RedirectToPage("/Index");
            }

            if (result.IsLockedOut)
            {
                await _audit.LogAsync("LOGIN_LOCKED", user.Id);
                ModelState.AddModelError("", "Account locked after 3 failed attempts. Try again later.");
                return Page();
            }

            await _audit.LogAsync("LOGIN_FAIL", user.Id);
            ModelState.AddModelError("", "Invalid login attempt.");
            return Page();
        }
    }
}
