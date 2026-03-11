using AngularApp4.Contracts.Auth;
using AngularApp4.Data;
using AngularApp4.Model;
using AngularApp4.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AuthController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var exists = await _db.AuthUsers.AnyAsync(u => u.Email == normalizedEmail);
            if (exists)
            {
                return Conflict("Email is already registered.");
            }

            var user = new AuthUser
            {
                FullName = request.FullName.Trim(),
                Email = normalizedEmail,
                PasswordHash = PasswordHasher.Hash(request.Password),
                Role = request.Role,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.AuthUsers.Add(user);
            await _db.SaveChangesAsync();

            return Ok(ToAuthResponse(user));
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await _db.AuthUsers.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
            if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid credentials.");
            }

            return Ok(ToAuthResponse(user));
        }

        private static AuthResponse ToAuthResponse(AuthUser user) => new()
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        };
    }
}
