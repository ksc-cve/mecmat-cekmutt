using System.Security.Cryptography;
using System.Text;

namespace Mecmat.Auth;

public static class Tokens
{
    public static string NewToken(int bytes = 32) =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    /// <summary>IP addresses are personal data under PDPA; logs keep a salted hash, which still
    /// lets us see "same address, many failures" without storing the address itself.</summary>
    public static string HashIp(string? ip, string salt) => Sha256(salt + "|" + (ip ?? "?"))[..16];
}
