using System.Security.Cryptography;

namespace Mecmat.Auth;

/// <summary>PBKDF2-HMAC-SHA256, stored as "v1.{iterations}.{salt}.{hash}" so the work factor can
/// be raised later and old hashes upgraded at the next successful sign-in (ADR 0003).</summary>
public static class PasswordHasher
{
    public const int Iterations = 600_000;
    public const int MinLength = 10;

    // The handful of passwords people actually type first. Not a substitute for length, but it
    // stops the worst choices at the door.
    private static readonly HashSet<string> Common = new(StringComparer.OrdinalIgnoreCase)
    {
        "1234567890", "12345678910", "0123456789", "password123", "password1234", "qwertyuiop",
        "1qaz2wsx3edc", "qwerty12345", "iloveyou12", "abcdefghij", "kmutt12345", "mecmat1234",
        "admin12345", "administrator", "passw0rd123", "1111111111", "0000000000", "9876543210",
    };

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"v1.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored, out bool needsRehash)
    {
        needsRehash = false;
        var parts = stored.Split('.');
        if (parts.Length != 4 || parts[0] != "v1" || !int.TryParse(parts[1], out var iter)) return false;
        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iter, HashAlgorithmName.SHA256, expected.Length);
        var ok = CryptographicOperations.FixedTimeEquals(actual, expected);
        needsRehash = ok && iter < Iterations;
        return ok;
    }

    /// <summary>Returns an error key for t(), or null when the password is acceptable.</summary>
    public static string? Validate(string? password, string? username)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength) return "auth.password.tooShort";
        if (password.Length > 256) return "auth.password.tooLong";
        if (Common.Contains(password)) return "auth.password.common";
        if (!string.IsNullOrEmpty(username) && password.Contains(username, StringComparison.OrdinalIgnoreCase))
            return "auth.password.containsUsername";
        if (password.Distinct().Count() < 4) return "auth.password.tooSimple";
        return null;
    }

    /// <summary>One-time password an admin hands to a person: unambiguous characters only, so it
    /// can be read out over the phone.</summary>
    public static string GenerateOneTime()
    {
        const string alphabet = "abcdefghjkmnpqrstuvwxyzACDEFGHJKLMNPQRSTUVWXYZ23456789";
        var chars = new char[14];
        for (var i = 0; i < chars.Length; i++) chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        return new string(chars, 0, 7) + "-" + new string(chars, 7, 7);
    }
}
