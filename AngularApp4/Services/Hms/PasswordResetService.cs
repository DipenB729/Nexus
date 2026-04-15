using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace AngularApp4.Services.Hms;

public sealed class PasswordResetTicket
{
    public string Code { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
}

public interface IPasswordResetService
{
    PasswordResetTicket CreateTicket(string email);
    bool TryConsume(string email, string code);
}

public sealed class PasswordResetService : IPasswordResetService
{
    private static readonly TimeSpan ResetWindow = TimeSpan.FromMinutes(15);

    private readonly IMemoryCache _cache;

    public PasswordResetService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public PasswordResetTicket CreateTicket(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        var ticket = new PasswordResetTicket
        {
            Code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString(CultureInfo.InvariantCulture),
            ExpiresAtUtc = DateTime.UtcNow.Add(ResetWindow)
        };

        _cache.Set(BuildCacheKey(normalizedEmail), ticket, ticket.ExpiresAtUtc);
        return ticket;
    }

    public bool TryConsume(string email, string code)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (!_cache.TryGetValue<PasswordResetTicket>(BuildCacheKey(normalizedEmail), out var ticket))
        {
            return false;
        }

        if (!string.Equals(ticket.Code, code.Trim(), StringComparison.Ordinal))
        {
            return false;
        }

        _cache.Remove(BuildCacheKey(normalizedEmail));
        return true;
    }

    private static string BuildCacheKey(string email) => $"password-reset:{email}";

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
