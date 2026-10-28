using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Mecmat.Auth;

namespace Mecmat.Data;

/// <summary>SQLite has no time-zone type; reading back gives Kind=Unspecified, which then
/// serialises without "Z" and the browser shows it 7 hours off. Every DateTime is UTC.</summary>
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

public sealed class EncryptedStringConverter() : ValueConverter<string?, string?>(
    v => FieldCipher.Current.Encrypt(v),
    v => FieldCipher.Current.Decrypt(v));
