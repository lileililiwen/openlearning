using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth.Models;

namespace OpenLearning.Auth.Services;

/// <summary>
/// Issues and verifies one-time phone verification codes. Codes are six
/// digits, expire after 10 minutes, are single-use, and lock a phone number
/// out after five failed attempts.
/// </summary>
public class PhoneCodeService
{
    public const int MaxAttempts = 5;

    private readonly DbContext _db;

    public PhoneCodeService(DbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Issues a new code for the phone number, replacing any outstanding one.
    /// Returns the code so the dev-only on-screen fallback can show it.
    /// </summary>
    public async Task<(bool Ok, string? Error, string? Code)> IssueAsync(string phoneNumber)
    {
        var normalized = Normalize(phoneNumber);
        if (normalized.Length == 0)
        {
            return (false, "A phone number is required.", null);
        }

        // Delete stale rows for this phone to keep the table tidy.
        var stale = await _db.Set<PhoneCode>()
            .Where(c => c.PhoneNumber == normalized)
            .ToListAsync();
        foreach (var row in stale)
        {
            _db.Set<PhoneCode>().Remove(row);
        }

        await _db.SaveChangesAsync();

        var code = GenerateCode();
        _db.Set<PhoneCode>().Add(new PhoneCode
        {
            PhoneNumber = normalized,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
        });
        await _db.SaveChangesAsync();
        return (true, null, code);
    }

    /// <summary>
    /// Verifies a code for the phone. Consumes the code on success; failed
    /// attempts count toward the per-phone lockout.
    /// </summary>
    public async Task<(bool Ok, string? Error)> VerifyAsync(string phoneNumber, string code)
    {
        var normalized = Normalize(phoneNumber);
        var trimmedCode = code?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || trimmedCode.Length == 0)
        {
            return (false, "Phone number and code are required.");
        }

        var record = await _db.Set<PhoneCode>()
            .Where(c => c.PhoneNumber == normalized && c.UsedAt == null)
            .OrderByDescending(c => c.ExpiresAt)
            .FirstOrDefaultAsync();
        if (record is null)
        {
            return (false, "No code was issued for this phone number.");
        }

        if (record.UsedAt is not null)
        {
            return (false, "This code has already been used.");
        }

        if (record.Attempts >= MaxAttempts)
        {
            return (false, "Too many failed attempts. Request a new code.");
        }

        if (record.ExpiresAt < DateTime.UtcNow)
        {
            _db.Set<PhoneCode>().Remove(record);
            await _db.SaveChangesAsync();
            return (false, "This code has expired. Request a new code.");
        }

        if (!CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(record.Code),
                System.Text.Encoding.UTF8.GetBytes(trimmedCode)))
        {
            record.Attempts++;
            await _db.SaveChangesAsync();
            var remaining = MaxAttempts - record.Attempts;
            if (remaining <= 0)
            {
                return (false, "Too many failed attempts. Request a new code.");
            }

            var attemptsLabel = remaining == 1 ? "attempt" : "attempts";
            return (false, $"Incorrect code. {remaining} {attemptsLabel} remaining.");
        }

        record.UsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>Normalizes a phone number to a comparable form (digits only, leading + kept).</summary>
    public static string Normalize(string phoneNumber)
    {
        var trimmed = phoneNumber?.Trim() ?? string.Empty;
        var builder = new System.Text.StringBuilder(trimmed.Length);
        foreach (var ch in trimmed)
        {
            if (char.IsDigit(ch) || (ch == '+' && builder.Length == 0))
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }

    private static string GenerateCode()
    {
        // RandomNumberGenerator avoids the predictability of Random for a 6-digit code.
        return RandomNumberGenerator.GetInt32(1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }
}
