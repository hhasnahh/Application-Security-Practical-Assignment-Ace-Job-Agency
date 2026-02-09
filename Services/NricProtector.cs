using Microsoft.AspNetCore.DataProtection;

namespace AceJobAgencyPortal.Services
{
    public class NricProtector
    {
        private readonly IDataProtector _protector;

        public NricProtector(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("AceJobAgencyPortal.NRIC.v1");
        }

        public string Protect(string plain) => _protector.Protect(plain);
        public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);
    }
}
