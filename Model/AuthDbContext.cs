using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AceJobAgencyPortal.Model
{
    public class AuthDbContext : IdentityDbContext<ApplicationUser>
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

        public DbSet<MemberProfile> MemberProfiles => Set<MemberProfile>();
        public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    }
}
