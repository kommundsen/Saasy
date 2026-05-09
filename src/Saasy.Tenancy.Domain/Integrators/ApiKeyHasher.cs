using System.Security.Cryptography;
using System.Text;

namespace Saasy.Tenancy.Domain.Integrators;

// PBKDF2-HMAC-SHA256, 310 000 iterations, 32-byte derived key.
// Parameters follow OWASP 2023 guidance for PBKDF2-HMAC-SHA256:
//   https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html
// Salt is caller-supplied (16 bytes minimum) and stored alongside the hash.
// Output is the raw derived-key bytes (32 bytes); callers encode as Base64 for storage.
public static class ApiKeyHasher
{
    private const int Iterations = 310_000;
    private const int KeyLength = 32;

    public static byte[] Hash(string secret, byte[] salt)
    {
        ArgumentException.ThrowIfNullOrEmpty(secret);
        ArgumentNullException.ThrowIfNull(salt);

        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(secret),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeyLength);
    }

    public static byte[] GenerateSalt()
    {
        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }
}
