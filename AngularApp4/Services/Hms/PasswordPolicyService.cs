using AngularApp4.Data;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Services.Hms;

public sealed class PasswordPolicyValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();
}

public interface IPasswordPolicyService
{
    Task<PasswordPolicyValidationResult> ValidateAsync(string? password, CancellationToken cancellationToken = default);
}

public sealed class PasswordPolicyService : IPasswordPolicyService
{
    private readonly AppDbContext _db;

    public PasswordPolicyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PasswordPolicyValidationResult> ValidateAsync(string? password, CancellationToken cancellationToken = default)
    {
        var result = new PasswordPolicyValidationResult();
        var policy = await _db.SecuritySettings
            .AsNoTracking()
            .OrderBy(x => x.SecuritySettingId)
            .FirstOrDefaultAsync(cancellationToken);

        var minLength = policy?.MinPasswordLength ?? 8;
        var requireUppercase = policy?.RequireUppercase ?? true;
        var requireLowercase = policy?.RequireLowercase ?? true;
        var requireDigit = policy?.RequireDigit ?? true;
        var requireSpecial = policy?.RequireSpecialCharacter ?? true;

        if (string.IsNullOrWhiteSpace(password))
        {
            result.Errors.Add("Password is required.");
            return result;
        }

        if (password.Length < minLength)
        {
            result.Errors.Add($"Password must be at least {minLength} characters.");
        }

        if (requireUppercase && !password.Any(char.IsUpper))
        {
            result.Errors.Add("Password must include at least one uppercase letter.");
        }

        if (requireLowercase && !password.Any(char.IsLower))
        {
            result.Errors.Add("Password must include at least one lowercase letter.");
        }

        if (requireDigit && !password.Any(char.IsDigit))
        {
            result.Errors.Add("Password must include at least one number.");
        }

        if (requireSpecial && !password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            result.Errors.Add("Password must include at least one special character.");
        }

        return result;
    }
}
