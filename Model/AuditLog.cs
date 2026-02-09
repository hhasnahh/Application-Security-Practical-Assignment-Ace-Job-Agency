using System.ComponentModel.DataAnnotations;

namespace AceJobAgencyPortal.Model
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        public string? UserId { get; set; }

        [Required]
        public string Action { get; set; } = "";

        public string? Details { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
