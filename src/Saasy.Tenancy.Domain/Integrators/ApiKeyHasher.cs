using System.Security.Cryptography;
using System.Text;

namespace Saasy.Tenancy.Domain.Integrators;

// PBKDF2-HMAC-SHA256, 310 000 iterations, 32-byte derived key.
// Parameters follow OWASP 2023 guidance for PBKDF2-HMAC-SHA256:
//   https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html
// Salt is caller-supplied (16 bytes minimum) and stored alongside the hash.
// Output is the raw derived-key bytes (32 bytes); callers encode as Base64 for storage.
//
// Stored format: "{base64(salt)}.{base64(hash)}"
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

    // Verifies a plaintext candidate against the stored "{base64(salt)}.{base64(hash)}" value.
    // Always uses CryptographicOperations.FixedTimeEquals so execution time does not
    // reveal whether the salt parse succeeded or which byte first differed.
    public static bool Verify(string candidate, string storedHashedSecret)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(storedHashedSecret);

        var dot = storedHashedSecret.IndexOf('.');
        if (dot < 0)
            return false;

        byte[] salt;
        byte[] storedHash;
        try
        {
            salt = Convert.FromBase64String(storedHashedSecret[..dot]);
            storedHash = Convert.FromBase64String(storedHashedSecret[(dot + 1)..]);
        }
        catch (FormatException)
        {
            return false;
        }

        var candidateHash = Hash(candidate, salt);

        return CryptographicOperations.FixedTimeEquals(candidateHash, storedHash);
    }
}
