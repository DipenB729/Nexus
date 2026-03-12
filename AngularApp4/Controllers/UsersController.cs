using System.Security.Claims;
using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<object>>> GetMe()
    {
        var userId = GetUserId();
        var user = await _db.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.UserId == userId);
        if (user is null) return NotFound(ApiResponse<object>.Fail("User not found"));

        return Ok(ApiResponse<object>.Ok(new
        {
            user.UserId,
            user.FullName,
            user.Email,
            user.Phone,
            Role = user.Role?.Name
        }));
    }

    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateMe([FromBody] UpdateProfileDto dto)
    {
        var userId = GetUserId();
        var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == userId);
        if (user is null) return NotFound(ApiResponse<object>.Fail("User not found"));

        if (!string.IsNullOrWhiteSpace(dto.FullName)) user.FullName = dto.FullName;
        if (!string.IsNullOrWhiteSpace(dto.Phone)) user.Phone = dto.Phone;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null, "Profile updated"));
    }

    private long GetUserId() => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
