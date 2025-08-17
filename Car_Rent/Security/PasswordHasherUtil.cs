using System.Security.Cryptography;

namespace Car_Rent.Security
{
    public static class PasswordHasherUtil
    {
        // Save format : PBKDF2$[iterations]$[salt]$[hash]
        private const string FormatPrefix = "PBKDF2";
        private const int Iterations = 100000;
        private const int SaltSize = 16; // 128 bits
        private const int KeySize = 32; // 256 bits

        public static string HashPassword(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);

            var key = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize
            );

            var saltB64 = Convert.ToBase64String(salt);
            var keyB64 = Convert.ToBase64String(key);
            return $"{FormatPrefix}${Iterations}${saltB64}${keyB64}";
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash)) return false;

            // If it is PBKDF2 format
            if (storedHash.StartsWith(FormatPrefix + "$", StringComparison.Ordinal))
            {
                var parts = storedHash.Split('$');
                if (parts.Length != 4) return false;
                if (!int.TryParse(parts[1], out var iter)) return false;

                var salt = Convert.FromBase64String(parts[2]);
                var keyStored = Convert.FromBase64String(parts[3]);

                var keyComputed = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iter,
                    HashAlgorithmName.SHA256,
                    keyStored.Length
                );

                return CryptographicOperations.FixedTimeEquals(keyStored, keyComputed);
            }

            // Legacy: SHA-256 hex (old format)
            // accepts hex 64 string 
            if (storedHash.Length == 64 && IsHex(storedHash))
            {
                using var sha256 = SHA256.Create();
                var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                var hex = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
                return string.Equals(hex, storedHash, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public static bool NeedsRehash(string storedHash)
        {
            // Rehash if it is legacy SHA-256 or PBKDF2 but iterations < current
            if (!storedHash.StartsWith(FormatPrefix + "$", StringComparison.Ordinal)) return true;
            var parts = storedHash.Split('$');
            if(parts.Length != 4) return true;
            if (!int.TryParse(parts[1], out var iter)) return true;
            return iter < Iterations;
        }

        private static bool IsHex(string s)
        {
            foreach (var c in s)
            {
                bool ok = (c >= '0' && c <= '9') ||
                            (c >= 'a' && c <= 'f') ||
                            (c >= 'A' && c <= 'F'); 
                if (!ok) return false;
            }
            return true;
        }
    }
}
