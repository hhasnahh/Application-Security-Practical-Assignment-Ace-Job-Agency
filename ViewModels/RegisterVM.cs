using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AceJobAgencyPortal.ViewModels
{
	public class RegisterVM
	{
		[Required]
		[RegularExpression(@"^[A-Za-z'-]+$", ErrorMessage = "First name can only contain letters, - and '.")]
		public string FirstName { get; set; } = "";

		[Required]
		[RegularExpression(@"^[A-Za-z'-]+$", ErrorMessage = "Last name can only contain letters, - and '.")]
		public string LastName { get; set; } = "";

		public string? Gender { get; set; }

		[Required]
		[RegularExpression(@"^[A-Za-z]\d{7}[A-Za-z]$", ErrorMessage = "NRIC must be 1 letter, 7 digits, 1 letter (e.g. S1234567D).")]
		public string Nric { get; set; } = "";

		[Required]
		[DataType(DataType.Date)]
		public DateTime Dob { get; set; }

		[Required, MaxLength(500)]
		public string? WhoAmI { get; set; } = "";

		[Required]
		[EmailAddress]
		[RegularExpression(@"^[^@\s]+@[^@\s]+\.[A-Za-z]{2,3}$", ErrorMessage = "Email must be like name@domain.com (2-3 letters after dot).")]
		public string Email { get; set; } = "";

		[Required]
		[RegularExpression(@"^\S+$", ErrorMessage = "Password cannot contain spaces.")]
		public string Password { get; set; } = "";

		[Required]
		[Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
		public string ConfirmPassword { get; set; } = "";

		[Required]
		public IFormFile? Resume { get; set; }

		public string? RecaptchaToken { get; set; }

		[Required]
		[Display(Name = "Profile Photo (.jpg)")]
		public IFormFile? Photo { get; set; }
	}
}
