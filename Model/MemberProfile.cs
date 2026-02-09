using System.ComponentModel.DataAnnotations;

namespace AceJobAgencyPortal.Model
{
    public class MemberProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = "";

        [Required, MaxLength(50)]
        public string FirstName { get; set; } = "";

        [Required, MaxLength(50)]
        public string LastName { get; set; } = "";

        [Required, MaxLength(10)]
        public string? Gender { get; set; }

        public DateTime Dob { get; set; }

        // Protected NRIC
        [Required]
        public string NricProtected { get; set; } = "";

        // Resume
        [Required]
        public string? ResumeOriginalFileName { get; set; }
        
        [Required]
        public string? ResumeStoredPath { get; set; }

        [Required, MaxLength(500)]
        public string? WhoAmI { get; set; }

        [Required]
        public byte[]? ProfilePhoto { get; set; }

        [Required]
        public string? ProfilePhotoContentType { get; set; }

    }
}
