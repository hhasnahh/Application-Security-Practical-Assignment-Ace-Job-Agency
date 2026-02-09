using AceJobAgencyPortal.Model;
using AceJobAgencyPortal.Services;
using AceJobAgencyPortal.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Win32;
using System.Security.Claims;

namespace AceJobAgencyPortal.Pages
{
	public class RegisterModel : PageModel
	{
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly AuthDbContext _db;
		private readonly NricProtector _nric;
		private readonly AuditLogger _audit;
		private readonly RecaptchaV3 _recaptcha;

		[BindProperty]
		public RegisterVM RModel { get; set; } = new();

		public RegisterModel(
			UserManager<ApplicationUser> userManager,
			SignInManager<ApplicationUser> signInManager,
			AuthDbContext db,
			NricProtector nric,
			AuditLogger audit,
			RecaptchaV3 recaptcha)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_db = db;
			_nric = nric;
			_audit = audit;
			_recaptcha = recaptcha;
		}

		public void OnGet()
		{
			RModel.Dob = DateTime.Today;
		}

		public async Task<IActionResult> OnPostAsync()
		{
			if (!ModelState.IsValid) return Page();

			// ✅ DOB rules: not future + at least 16 years old
			var today = DateTime.Today;

			if (RModel.Dob.Date > today)
			{
				ModelState.AddModelError("RModel.Dob", "DOB cannot be in the future.");
				return Page();
			}

			var minDob = today.AddYears(-16); // must be born on or before this date
			if (RModel.Dob.Date > minDob)
			{
				ModelState.AddModelError("RModel.Dob", "You must be at least 16 years old to register.");
				return Page();
			}

			// ✅ reCAPTCHA check
			if (string.IsNullOrWhiteSpace(RModel.RecaptchaToken))
			{
				ModelState.AddModelError("", "reCAPTCHA token missing.");
				return Page();
			}

			var (ok, score, err) = await _recaptcha.VerifyAsync(RModel.RecaptchaToken, "register");
			if (!ok)
			{
				ModelState.AddModelError("", $"reCAPTCHA failed ({err}), score={score:0.00}");
				return Page();
			}

			// =========================
			// ✅ PHOTO validation (.jpg only)
			// =========================
			byte[]? photoBytes = null;
			string? photoContentType = null;

			if (RModel.Photo != null && RModel.Photo.Length > 0)
			{
				const long maxPhotoBytes = 2 * 1024 * 1024; // 2MB
				if (RModel.Photo.Length > maxPhotoBytes)
				{
					ModelState.AddModelError("RModel.Photo", "Photo too large (max 2MB).");
					return Page();
				}

				var ext = Path.GetExtension(RModel.Photo.FileName).ToLowerInvariant();

				// ✅ Only .jpg allowed
				if (ext != ".jpg")
				{
					ModelState.AddModelError("RModel.Photo", "Only .jpg photos are allowed.");
					return Page();
				}

				var ct = (RModel.Photo.ContentType ?? "").ToLowerInvariant();

				// ✅ Only JPEG content-type
				if (ct != "image/jpeg")
				{
					ModelState.AddModelError("RModel.Photo", "Invalid photo type. Only JPG (image/jpeg) is allowed.");
					return Page();
				}

				using var ms = new MemoryStream();
				await RModel.Photo.CopyToAsync(ms);
				photoBytes = ms.ToArray();
				photoContentType = ct;
			}


			// =========================
			// ✅ Resume validation (.pdf/.docx)
			// =========================
			string? storedPath = null;
			string? originalName = null;

			if (RModel.Resume != null && RModel.Resume.Length > 0)
			{
				var ext = Path.GetExtension(RModel.Resume.FileName).ToLowerInvariant();
				var allowed = new[] { ".pdf", ".docx" };

				if (!allowed.Contains(ext))
				{
					ModelState.AddModelError("RModel.Resume", "Only .pdf or .docx files are allowed.");
					return Page();
				}

				const long maxBytes = 5 * 1024 * 1024; // 5MB
				if (RModel.Resume.Length > maxBytes)
				{
					ModelState.AddModelError("RModel.Resume", "File size must be under 5MB.");
					return Page();
				}

				originalName = RModel.Resume.FileName;

				var folder = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "Resumes");
				Directory.CreateDirectory(folder);

				var safeName = $"{Guid.NewGuid()}{ext}";
				storedPath = Path.Combine(folder, safeName);

				using var fs = System.IO.File.Create(storedPath);
				await RModel.Resume.CopyToAsync(fs);
			}

			var user = new ApplicationUser
			{
				UserName = RModel.Email,
				Email = RModel.Email
			};

			var result = await _userManager.CreateAsync(user, RModel.Password);

			if (!result.Succeeded)
			{
				foreach (var e in result.Errors)
					ModelState.AddModelError("", e.Description);

				await _audit.LogAsync("REGISTER_FAIL", null,
					string.Join(" | ", result.Errors.Select(x => x.Description)));
				return Page();
			}

			user.TwoFactorEnabled = true;

			user.ActiveSessionId = Guid.NewGuid().ToString("N");
			await _userManager.UpdateAsync(user);

			_db.MemberProfiles.Add(new MemberProfile
			{
				UserId = user.Id,
				FirstName = RModel.FirstName,
				LastName = RModel.LastName,
				Gender = RModel.Gender,
				Dob = RModel.Dob,
				NricProtected = _nric.Protect(RModel.Nric),
				ResumeOriginalFileName = originalName,
				ResumeStoredPath = storedPath,
				WhoAmI = RModel.WhoAmI,

				ProfilePhoto = photoBytes,
				ProfilePhotoContentType = photoContentType
			});

			await _db.SaveChangesAsync();

			await _audit.LogAsync("REGISTER_SUCCESS", user.Id);

			// Sign in with "sid" claim
			await _signInManager.SignOutAsync();

			await _signInManager.SignInWithClaimsAsync(
				user,
				isPersistent: false,
				additionalClaims: new[] { new Claim("sid", user.ActiveSessionId!) }
			);

			return RedirectToPage("Index");
		}
	}
}
