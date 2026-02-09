using AceJobAgencyPortal.Model;

namespace AceJobAgencyPortal.Services
{
    public class AuditLogger
    {
        private readonly AuthDbContext _db;

        public AuditLogger(AuthDbContext db) { _db = db; }

        public async Task LogAsync(string action, string? userId, string? details = null)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                Action = action,
                UserId = userId,
                Details = details
            });
            await _db.SaveChangesAsync();
        }
    }
}
