using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AceJobAgencyPortal.Model;
using AceJobAgencyPortal.Services;

namespace AceJobAgencyPortal.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AuthDbContext _db;
        private readonly NricProtector _nric;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PasswordPolicyService _policy;

        public MemberProfile? Profile { get; private set; }
        public string? NricPlain { get; private set; }
        public string? Email { get; private set; }

        public IndexModel(AuthDbContext db, NricProtector nric, UserManager<ApplicationUser> userManager, PasswordPolicyService policy)
        {
            _db = db;
            _nric = nric;
            _userManager = userManager;
            _policy = policy;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // public homepage if not logged in
            if (User?.Identity?.IsAuthenticated != true)
                return Page();

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Page();

            // optional: force change password after max age
            if (_policy.IsExpired(user))
                return RedirectToPage("/ChangePassword");

            Email = user.Email;

            Profile = await _db.MemberProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (Profile != null)
                NricPlain = _nric.Unprotect(Profile.NricProtected);

            return Page();
        }

        // ✅ Serves profile photo
        public async Task<IActionResult> OnGetPhotoAsync()
        {
            if (User?.Identity?.IsAuthenticated != true) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return NotFound();

            var p = await _db.MemberProfiles.FirstOrDefaultAsync(x => x.UserId == userId);

            if (p?.ProfilePhoto == null || string.IsNullOrEmpty(p.ProfilePhotoContentType))
                return NotFound();

            return File(p.ProfilePhoto, p.ProfilePhotoContentType);
        }
        // ✅ Serves resume file (click to download/open)
        public async Task<IActionResult> OnGetResumeAsync()
        {
            if (User?.Identity?.IsAuthenticated != true) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return NotFound();

            var p = await _db.MemberProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
            if (p == null || string.IsNullOrEmpty(p.ResumeStoredPath) || !System.IO.File.Exists(p.ResumeStoredPath))
                return NotFound();

            // use original name if available; fallback name if null
            var downloadName = string.IsNullOrWhiteSpace(p.ResumeOriginalFileName) ? "Resume" : p.ResumeOriginalFileName;

            // basic content type based on extension
            var ext = Path.GetExtension(downloadName).ToLowerInvariant();
            var contentType = ext == ".pdf"
                ? "application/pdf"
                : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            var bytes = await System.IO.File.ReadAllBytesAsync(p.ResumeStoredPath);
            return File(bytes, contentType, downloadName);
        }

    }
}
