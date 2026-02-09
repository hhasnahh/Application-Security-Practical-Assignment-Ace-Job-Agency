using System.ComponentModel.DataAnnotations;

namespace AceJobAgencyPortal.Model
{
    public class PasswordHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = "";

        [Required]
        public string PasswordHash { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
