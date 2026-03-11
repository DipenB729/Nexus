using AngularApp4.Contracts.Auth;
using AngularApp4.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AngularApp4.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
            if (existingUser is not null)
            {
                return Conflict("Email is already registered.");
            }

            var user = new ApplicationUser
            {
                UserName = normalizedEmail,
                Email = normalizedEmail,
                FullName = request.FullName.Trim(),
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                return BadRequest(createResult.Errors.Select(e => e.Description));
            }

            var role = request.Role.Trim();
            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                return BadRequest(roleResult.Errors.Select(e => e.Description));
            }

            return Ok(ToAuthResponse(user, role));
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await _userManager.FindByEmailAsync(normalizedEmail);
            if (user is null)
            {
                return Unauthorized("Invalid credentials.");
            }

            var isValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isValid)
            {
                return Unauthorized("Invalid credentials.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "User";

            return Ok(ToAuthResponse(user, role));
        }

        private static AuthResponse ToAuthResponse(ApplicationUser user, string role) => new()
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = role
        };
    }
}
