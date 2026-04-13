using System.Security.Cryptography;
using System.Text;
using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using AngularApp4.Services.Hms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly IAuditLogService _audit;
    private readonly IPasswordPolicyService _passwordPolicy;

    public AuthController(AppDbContext db, IJwtTokenService jwt, IAuditLogService audit, IPasswordPolicyService passwordPolicy)
    {
        _db = db;
        _jwt = jwt;
        _audit = audit;
        _passwordPolicy = passwordPolicy;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register(RegisterRequestDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(x => x.Email == email))
        {
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("Email already exists"));
        }

        var policyValidation = await _passwordPolicy.ValidateAsync(dto.Password);
        if (!policyValidation.IsValid)
        {
            return BadRequest(ApiResponse<AuthResponseDto>.Fail(policyValidation.Errors.First()));
        }

        const string roleName = "User";
        var role = await _db.Roles.FirstAsync(x => x.Name == roleName);

        CreatePasswordHash(dto.Password, out var hash, out var salt);

        var user = new User
        {
            FullName = dto.FullName,
            Email = email,
            RoleId = role.RoleId,
            PasswordHash = hash,
            PasswordSalt = salt,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        if (roleName == "User")
        {
            _db.Patients.Add(new Patient { UserId = user.UserId, CreatedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync();
        }

        await _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.Authentication,
            Action = "Registered",
            EntityName = "User",
            EntityId = user.UserId,
            TargetDisplayName = user.FullName,
            Summary = $"User {user.FullName} registered a new portal account.",
            Metadata = new { user.Email, Role = roleName },
            PerformedByUserId = user.UserId,
            PerformedByName = user.FullName,
            PerformedByRole = roleName,
            ActorEmail = user.Email
        });

        var token = await _jwt.GenerateTokenAsync(user, roleName);
        return Ok(ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Token = token,
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = roleName
        }, "Registration successful"));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(LoginRequestDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Email == email && x.IsActive);
        if (user is null || !VerifyPassword(dto.Password, user.PasswordHash, user.PasswordSalt))
        {
            await _audit.WriteAsync(new AuditLogRequest
            {
                Category = AuditLogCategories.Authentication,
                Action = "LoginFailed",
                EntityName = "UserSession",
                TargetDisplayName = email,
                Summary = $"Failed login attempt for {email}.",
                ActorEmail = email
            });
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail("Invalid credentials"));
        }

        var role = user.Role?.Name ?? "User";
        await _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.Authentication,
            Action = "LoginSucceeded",
            EntityName = "UserSession",
            EntityId = user.UserId,
            TargetDisplayName = user.FullName,
            Summary = $"{user.FullName} signed in successfully.",
            Metadata = new { user.Email, Role = role },
            PerformedByUserId = user.UserId,
            PerformedByName = user.FullName,
            PerformedByRole = role,
            ActorEmail = user.Email
        });

        var token = await _jwt.GenerateTokenAsync(user, role);
        return Ok(ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Token = token,
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = role
        }, "Login successful"));
    }

    private static void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
    {
        using var hmac = new HMACSHA512();
        salt = hmac.Key;
        hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }

    private static bool VerifyPassword(string password, byte[] storedHash, byte[] storedSalt)
    {
        using var hmac = new HMACSHA512(storedSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return computedHash.SequenceEqual(storedHash);
    }
}
