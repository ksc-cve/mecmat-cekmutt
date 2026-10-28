using System.Security.Cryptography;
using System.Text;

namespace Mecmat.Auth;

/// <summary>AES-256-GCM for columns marked [Sensitive]. The key is a file in DataRoot, not in
/// the database, so a copied mecmat.db alone does not reveal ID or bank numbers (ADR 0007).</summary>
public sealed class FieldCipher
{
    private const string Prefix = "v1:";
    private readonly byte[] _key;

    private static FieldCipher? _current;
    /// <summary>EF value converters are created by the model, not by DI, so they reach the
    /// cipher through this process-wide instance set once at startup.</summary>
    public static FieldCipher Current => _current ?? throw new InvalidOperationException("FieldCipher not initialised.");

    private FieldCipher(byte[] key) => _key = key;

    public static FieldCipher Initialise(string dataRoot)
    {
        var path = Path.Combine(dataRoot, "field.key");
        var key = KeyFile.LoadOrCreate(path, 32);
        _current = new FieldCipher(key);
        return _current;
    }

    public string? Encrypt(string? plain)
    {
        if (string.IsNullOrEmpty(plain)) return plain;
        var data = Encoding.UTF8.GetBytes(plain);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var cipher = new byte[data.Length];
        var tag = new byte[16];
        using (var gcm = new AesGcm(_key, 16)) gcm.Encrypt(nonce, data, cipher, tag);
        return Prefix + Convert.ToBase64String([.. nonce, .. cipher, .. tag]);
    }

    public string? Decrypt(string? stored)
    {
        if (string.IsNullOrEmpty(stored) || !stored.StartsWith(Prefix, StringComparison.Ordinal)) return stored;
        var raw = Convert.FromBase64String(stored[Prefix.Length..]);
        var nonce = raw.AsSpan(0, 12);
        var tag = raw.AsSpan(raw.Length - 16);
        var cipher = raw.AsSpan(12, raw.Length - 28);
        var plain = new byte[cipher.Length];
        using (var gcm = new AesGcm(_key, 16)) gcm.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }

    /// <summary>Shows only the last few characters, e.g. "•••••••••12-3".</summary>
    public static string? Mask(string? plain, int visible = 4)
    {
        if (string.IsNullOrEmpty(plain)) return plain;
        if (plain.Length <= visible) return new string('•', plain.Length);
        return new string('•', plain.Length - visible) + plain[^visible..];
    }
}

public static class KeyFile
{
    /// <summary>Creates the key on first run and re-tightens permissions to 0600 on every start,
    /// because a restore or a copy can quietly loosen them.</summary>
    public static byte[] LoadOrCreate(string path, int length)
    {
        if (!File.Exists(path))
        {
            File.WriteAllBytes(path, RandomNumberGenerator.GetBytes(length));
        }
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        var key = File.ReadAllBytes(path);
        if (key.Length != length) throw new InvalidOperationException($"{path} has the wrong length.");
        return key;
    }
}
