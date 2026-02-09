using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AceJobAgencyPortal.Model;

namespace AceJobAgencyPortal.Pages
{
    [Authorize]
    public class ChangePhotoModel : PageModel
    {
        private readonly AuthDbContext _db;

        public ChangePhotoModel(AuthDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        [Required(ErrorMessage = "Please choose a photo.")]
        public IFormFile? Photo { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            if (Photo == null || Photo.Length == 0)
            {
                ModelState.AddModelError("Photo", "Photo is required.");
                return Page();
            }

            const long maxPhotoBytes = 2 * 1024 * 1024;
            if (Photo.Length > maxPhotoBytes)
            {
                ModelState.AddModelError("Photo", "Photo is too large (max 2MB).");
                return Page();
            }

            var ext = Path.GetExtension(Photo.FileName).ToLowerInvariant();
            var allowedExt = new[] { ".jpg", ".jpeg", ".png" };
            if (!allowedExt.Contains(ext))
            {
                ModelState.AddModelError("Photo", "Only .jpg, .jpeg, or .png images are allowed.");
                return Page();
            }

            var ct = (Photo.ContentType ?? "").ToLowerInvariant();
            var allowedCt = new[] { "image/jpeg", "image/png" };
            if (!allowedCt.Contains(ct))
            {
                ModelState.AddModelError("Photo", "Invalid image content type.");
                return Page();
            }

            using var ms = new MemoryStream();
            await Photo.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var profile = await _db.MemberProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
                return RedirectToPage("/Index");

            profile.ProfilePhoto = bytes;
            profile.ProfilePhotoContentType = ct;

            await _db.SaveChangesAsync();

            return RedirectToPage("/Index");
        }
    }
}
