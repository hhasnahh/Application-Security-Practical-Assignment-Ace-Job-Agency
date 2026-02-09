using Microsoft.AspNetCore.Identity;

namespace AceJobAgencyPortal.Model
{
    public class ApplicationUser : IdentityUser
    {
        public string? ActiveSessionId { get; set; }

        // Password policy timestamps
        public DateTime PasswordLastChangedAt { get; set; } = DateTime.UtcNow;
    }
}
