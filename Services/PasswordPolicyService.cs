using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AceJobAgencyPortal.Model;

namespace AceJobAgencyPortal.Services
{
    public class PasswordPolicyService
    {
        private readonly AuthDbContext _db;
        private readonly IPasswordHasher<ApplicationUser> _hasher;

        public static readonly TimeSpan MinAge = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(2);
        public const int HistoryCount = 2;

        public PasswordPolicyService(AuthDbContext db, IPasswordHasher<ApplicationUser> hasher)
        {
            _db = db;
            _hasher = hasher;
        }

        public (bool ok, string msg) CheckMinAge(ApplicationUser user)
        {
            var age = DateTime.UtcNow - user.PasswordLastChangedAt;
            if (age < MinAge)
            {
                var wait = MinAge - age;
                return (false, $"You can only change password after {Math.Ceiling(wait.TotalMinutes)} minute(s).");
            }
            return (true, "");
        }

        public bool IsExpired(ApplicationUser user) =>
            (DateTime.UtcNow - user.PasswordLastChangedAt) > MaxAge;

        public async Task<(bool ok, string msg)> CheckNotReusedAsync(ApplicationUser user, string newPassword)
        {
            // current
            if (_hasher.VerifyHashedPassword(user, user.PasswordHash!, newPassword) == PasswordVerificationResult.Success)
                return (false, "You cannot reuse your current password.");

            // last N
            var last = await _db.PasswordHistories
                .Where(h => h.UserId == user.Id)
                .OrderByDescending(h => h.CreatedAt)
                .Take(HistoryCount)
                .ToListAsync();

            foreach (var h in last)
            {
                if (_hasher.VerifyHashedPassword(user, h.PasswordHash, newPassword) == PasswordVerificationResult.Success)
                    return (false, $"You cannot reuse your last {HistoryCount} passwords.");
            }

            return (true, "");
        }

        public async Task RecordHistoryAsync(ApplicationUser user)
        {
            if (string.IsNullOrWhiteSpace(user.PasswordHash)) return;

            _db.PasswordHistories.Add(new PasswordHistory
            {
                UserId = user.Id,
                PasswordHash = user.PasswordHash!,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            var all = await _db.PasswordHistories
                .Where(h => h.UserId == user.Id)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();

            var extra = all.Skip(HistoryCount).ToList();
            if (extra.Count > 0)
            {
                _db.PasswordHistories.RemoveRange(extra);
                await _db.SaveChangesAsync();
            }
        }
    }
}
