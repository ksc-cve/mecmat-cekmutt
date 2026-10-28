using System.Security.Cryptography;

namespace Mecmat.Auth;

/// <summary>RFC 6238 TOTP (SHA-1, 6 digits, 30 s) — the variant every authenticator app supports.
/// Written here because the stack takes no extra packages (ADR 0001).</summary>
public static class Totp
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string NewSecret() => Base32Encode(RandomNumberGenerator.GetBytes(20));

    public static string OtpAuthUri(string secret, string account) =>
        $"otpauth://totp/MECMAT:{Uri.EscapeDataString(account)}?secret={secret}&issuer=MECMAT&digits=6&period=30";

    /// <summary>Accepts the previous, current and next 30-second window to tolerate clock drift
    /// on phones.</summary>
    public static bool Verify(string secret, string? code)
    {
        if (code is null) return false;
        code = code.Replace(" ", "");
        if (code.Length != 6 || !code.All(char.IsAsciiDigit)) return false;
        var key = Base32Decode(secret);
        var step = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        for (var d = -1; d <= 1; d++)
            if (CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.ASCII.GetBytes(Compute(key, step + d)),
                    System.Text.Encoding.ASCII.GetBytes(code)))
                return true;
        return false;
    }

    private static string Compute(byte[] key, long counter)
    {
        Span<byte> msg = stackalloc byte[8];
        for (var i = 7; i >= 0; i--) { msg[i] = (byte)(counter & 0xff); counter >>= 8; }
        var hash = HMACSHA1.HashData(key, msg);
        var offset = hash[^1] & 0x0f;
        var bin = ((hash[offset] & 0x7f) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (bin % 1_000_000).ToString("D6");
    }

    private static string Base32Encode(byte[] data)
    {
        var sb = new System.Text.StringBuilder();
        int buffer = 0, bits = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b; bits += 8;
            while (bits >= 5) { sb.Append(Base32Alphabet[(buffer >> (bits - 5)) & 31]); bits -= 5; }
        }
        if (bits > 0) sb.Append(Base32Alphabet[(buffer << (5 - bits)) & 31]);
        return sb.ToString();
    }

    private static byte[] Base32Decode(string s)
    {
        var bytes = new List<byte>();
        int buffer = 0, bits = 0;
        foreach (var c in s.TrimEnd('=').ToUpperInvariant())
        {
            var v = Base32Alphabet.IndexOf(c);
            if (v < 0) continue;
            buffer = (buffer << 5) | v; bits += 5;
            if (bits >= 8) { bytes.Add((byte)((buffer >> (bits - 8)) & 0xff)); bits -= 8; }
        }
        return [.. bytes];
    }
}
