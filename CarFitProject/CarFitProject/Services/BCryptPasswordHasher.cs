using Microsoft.AspNetCore.Identity;

namespace CarFitProject.Services
{
    /// <summary>
    /// Replaces ASP.NET Identity's default PBKDF2 hasher with BCrypt at
    /// cost factor 12, satisfying NFR-S1. SaltParseException on a legacy
    /// hash format is treated as a failed verification (caller can rotate).
    /// </summary>
    public class BCryptPasswordHasher<TUser> : IPasswordHasher<TUser> where TUser : class
    {
        private const int WorkFactor = 12;

        /// <summary>Hashes the password with BCrypt at cost 12.</summary>
        public string HashPassword(TUser user, string password)
            => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

        /// <summary>Constant-time verify; returns Failed on null/empty inputs or unparsable hashes.</summary>
        public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword) || string.IsNullOrEmpty(providedPassword))
                return PasswordVerificationResult.Failed;

            try
            {
                return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword)
                    ? PasswordVerificationResult.Success
                    : PasswordVerificationResult.Failed;
            }
            catch (BCrypt.Net.SaltParseException)
            {
                return PasswordVerificationResult.Failed;
            }
        }
    }
}
