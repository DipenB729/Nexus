using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AngularApp4.Model.Hms;
using AngularApp4.Data;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Services.Hms;

public class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; } = 120;
}

public interface IJwtTokenService
{
    Task<string> GenerateTokenAsync(User user, string role, CancellationToken cancellationToken = default);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;
    private readonly AppDbContext _db;

    public JwtTokenService(IOptions<JwtSettings> options, AppDbContext db)
    {
        _settings = options.Value;
        _db = db;
    }

    public async Task<string> GenerateTokenAsync(User user, string role, CancellationToken cancellationToken = default)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var sessionTimeoutMinutes = await _db.SecuritySettings
            .AsNoTracking()
            .OrderBy(x => x.SecuritySettingId)
            .Select(x => (int?)x.SessionTimeoutMinutes)
            .FirstOrDefaultAsync(cancellationToken)
            ?? _settings.ExpiresInMinutes;
        var expires = DateTime.UtcNow.AddMinutes(sessionTimeoutMinutes);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
