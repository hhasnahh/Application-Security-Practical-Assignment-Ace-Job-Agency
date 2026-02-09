using Microsoft.AspNetCore.Identity;
using AceJobAgencyPortal.Model;
using System.Text.RegularExpressions;

namespace AceJobAgencyPortal.Services
{
    public class StrongPasswordValidator : IPasswordValidator<ApplicationUser>
    {
        // small common list for demo (expand if you want)
        private static readonly HashSet<string> Common =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "password", "password1", "password123", "qwerty", "qwerty123",
                "123456789", "12345678", "admin123", "letmein", "welcome123",
                "iloveyou", "singapore", "nanyang", "acejobagency"
            };

        public Task<IdentityResult> ValidateAsync(
            UserManager<ApplicationUser> manager,
            ApplicationUser user,
            string? password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return Task.FromResult(IdentityResult.Failed(
                    new IdentityError { Description = "Password is required." }));

            // No spaces (server enforced)
            if (password.Any(char.IsWhiteSpace))
                return Task.FromResult(IdentityResult.Failed(
                    new IdentityError { Description = "Password cannot contain spaces." }));

            var errors = new List<IdentityError>();

            // Block common passwords
            if (Common.Contains(password))
                errors.Add(new IdentityError { Description = "Password is too common. Please choose a stronger password." });

            // Block repeated single character (e.g. aaaaaaaaaaaa)
            if (password.Distinct().Count() <= 2)
                errors.Add(new IdentityError { Description = "Password is too repetitive. Use more variety." });

            // Block simple sequences (e.g. abcdef..., 123456...)
            if (LooksSequential(password))
                errors.Add(new IdentityError { Description = "Password looks like a sequence. Avoid patterns like 123456 or abcdef." });

            return Task.FromResult(errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed(errors.ToArray()));
        }

        private static bool LooksSequential(string s)
        {
            // Find long digit sequences
            if (Regex.IsMatch(s, @"0123|1234|2345|3456|4567|5678|6789")) return true;
            // Find long alpha sequences
            var lower = s.ToLowerInvariant();
            if (lower.Contains("abcd") || lower.Contains("bcde") || lower.Contains("cdef") || lower.Contains("defg")) return true;

            return false;
        }
    }
}
