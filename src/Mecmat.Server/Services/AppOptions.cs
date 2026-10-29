namespace Mecmat.Services;

public sealed class AppOptions
{
    public string DataRoot { get; set; } = "";
    public bool IsDevelopment { get; set; }
    /// <summary>Admins and people on a money approval chain must use TOTP (ADR 0003).</summary>
    public bool RequireTotpForApprovers { get; set; } = true;
    public string IpSalt { get; set; } = "";
    public string UploadsPath => Path.Combine(DataRoot, "uploads");
}
