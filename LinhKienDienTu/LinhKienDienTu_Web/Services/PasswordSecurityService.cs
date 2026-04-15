using System.Security.Cryptography;
using System.Text;

namespace LinhKienDienTu_Web.Services
{
    public class PasswordSecurityService
    {
        private const int SaltSize = 15;
        private const int KeySize = 16;
        private const int Iterations = 100_000;
        private const byte FormatMarker = 1;

        public string HashPassword(string password)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(password);

            var payload = new byte[1 + SaltSize + KeySize];
            payload[0] = FormatMarker;

            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(payload, 1, SaltSize);

            using var deriveBytes = new Rfc2898DeriveBytes(
                password,
                payload.AsSpan(1, SaltSize).ToArray(),
                Iterations,
                HashAlgorithmName.SHA256);

            var key = deriveBytes.GetBytes(KeySize);
            Buffer.BlockCopy(key, 0, payload, 1 + SaltSize, KeySize);

            return Convert.ToBase64String(payload);
        }

        public bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrWhiteSpace(providedPassword))
            {
                return false;
            }

            try
            {
                var payload = Convert.FromBase64String(hashedPassword);
                if (payload.Length != 1 + SaltSize + KeySize || payload[0] != FormatMarker)
                {
                    return false;
                }

                var salt = payload.AsSpan(1, SaltSize).ToArray();
                var expectedKey = payload.AsSpan(1 + SaltSize, KeySize).ToArray();

                using var deriveBytes = new Rfc2898DeriveBytes(
                    providedPassword,
                    salt,
                    Iterations,
                    HashAlgorithmName.SHA256);

                var actualKey = deriveBytes.GetBytes(KeySize);
                return CryptographicOperations.FixedTimeEquals(expectedKey, actualKey);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public bool IsHashedByThisService(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            try
            {
                var payload = Convert.FromBase64String(value);
                return payload.Length == 1 + SaltSize + KeySize && payload[0] == FormatMarker;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
