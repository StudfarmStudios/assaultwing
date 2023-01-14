using System.Security.Cryptography;

namespace AW2.Helpers
{
    /// <summary>
    /// Use a shared salt that is regenerated for every server (and client) instance.
    /// This is then used to generate unique IDs from values that we don't want to
    /// share between clients. The IDs will remain stable for as long the same server process
    /// is running, but clients can't decode each others details.
    /// </summary>
    public class SecureId
    {
        private byte[] Salt;

        private readonly int SaltSize = 128;

        private readonly int IterationCount = 10; // Adjusted to be fast enough using the test SecureIdTest

        private readonly int IdBytes = 16;

        public SecureId()
        {
            Salt = RandomNumberGenerator.GetBytes(SaltSize);
        }

        public string MakeId(string input)
        {
            Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(input, Salt);
            pbkdf2.IterationCount = IterationCount;
            var hashedBytes = pbkdf2.GetBytes(IdBytes);
            var id = Convert.ToBase64String(hashedBytes);
            return id;
        }
    }
}
