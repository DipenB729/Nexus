using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using AngularApp4.Services.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Policy = "AdminOnly")]
public class RolesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _audit;
    private readonly IPasswordPolicyService _passwordPolicy;

    public RolesController(AppDbContext db, IAuditLogService audit, IPasswordPolicyService passwordPolicy)
    {
        _db = db;
        _audit = audit;
        _passwordPolicy = passwordPolicy;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<RoleDetailsDto>>>> GetAll()
    {
        var roles = await _db.Roles
            .AsNoTracking()
            .OrderByDescending(x => x.Name == "Admin")
            .ThenBy(x => x.Name)
            .ToListAsync();

        var roleIds = roles.Select(x => x.RoleId).ToList();
        var permissions = await _db.RolePermissions
            .AsNoTracking()
            .Where(x => roleIds.Contains(x.RoleId))
            .OrderBy(x => x.ModuleName)
            .ToListAsync();

        var payload = roles.Select(role => new RoleDetailsDto
        {
            RoleId = role.RoleId,
            Name = role.Name,
            Description = role.Description,
            IsActive = role.IsActive,
            IsSystemRole = role.IsSystemRole,
            Permissions = permissions
                .Where(x => x.RoleId == role.RoleId)
                .Select(x => new RolePermissionDto
                {
                    ModuleKey = x.ModuleKey,
                    ModuleName = x.ModuleName,
                    MenuKey = x.MenuKey ?? string.Empty,
                    PageRoute = x.PageRoute ?? string.Empty,
                    CanAccessMenu = x.CanAccessMenu ?? false,
                    CanAccessPage = x.CanAccessPage ?? false,
                    CanView = x.CanView ?? false,
                    CanAdd = x.CanAdd ?? false,
                    CanEdit = x.CanEdit ?? false,
                    CanDelete = x.CanDelete ?? false
                })
                .ToList()
        }).ToList();

        return Ok(ApiResponse<IEnumerable<RoleDetailsDto>>.Ok(payload));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleDetailsDto>>> Create([FromBody] CreateRoleDto dto)
    {
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(ApiResponse<RoleDetailsDto>.Fail("Role name is required"));
        }

        var exists = await _db.Roles.AnyAsync(x => x.Name.ToLower() == name.ToLower());
        if (exists)
        {
            return BadRequest(ApiResponse<RoleDetailsDto>.Fail("Role name already exists"));
        }

        var role = new Role
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            IsActive = true,
            IsSystemRole = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        if (dto.Permissions is not null)
        {
            await ReplacePermissionsAsync(role.RoleId, dto.Permissions);
        }
        await _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.Administration,
            Action = "Created",
            EntityName = "Role",
            EntityId = role.RoleId,
            TargetDisplayName = role.Name,
            Summary = $"Role {role.Name} was created with {dto.Permissions?.Count() ?? 0} permission rows."
        });

        var created = await BuildRoleDtoAsync(role.RoleId);
        return Ok(ApiResponse<RoleDetailsDto>.Ok(created, "Role created"));
    }

    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<IEnumerable<AdminUserDto>>>> GetUsers()
    {
        var protectedPortalRoles = new[] { "User" };
        var roles = await _db.Roles
            .AsNoTracking()
            .Where(x => !protectedPortalRoles.Contains(x.Name))
            .ToListAsync();

        var roleNames = roles.ToDictionary(x => x.RoleId, x => x.Name);
        var roleIds = roleNames.Keys.ToList();
        var users = await _db.Users
            .AsNoTracking()
            .Where(x => roleIds.Contains(x.RoleId))
            .ToListAsync();

        var payload = users
            .Select(x => ToAdminUserDto(x, roleNames[x.RoleId]))
            .OrderBy(x => x.RoleName)
            .ThenBy(x => x.FullName)
            .ToList();

        return Ok(ApiResponse<IEnumerable<AdminUserDto>>.Ok(payload));
    }

    [HttpPost("users")]
    public async Task<ActionResult<ApiResponse<AdminUserDto>>> CreateUser([FromBody] CreateRoleAccountDto dto)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(x => x.RoleId == dto.RoleId && x.IsActive);
        if (role is null || role.Name is "User")
        {
            return BadRequest(ApiResponse<AdminUserDto>.Fail("Select an active staff/admin role"));
        }

        var fullName = dto.FullName.Trim();
        var email = dto.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(ApiResponse<AdminUserDto>.Fail("Full name and email are required"));
        }

        if (await _db.Users.AnyAsync(x => x.Email == email))
        {
            return BadRequest(ApiResponse<AdminUserDto>.Fail("Email already exists"));
        }

        var policyValidation = await _passwordPolicy.ValidateAsync(dto.Password);
        if (!policyValidation.IsValid)
        {
            return BadRequest(ApiResponse<AdminUserDto>.Fail(policyValidation.Errors.First()));
        }

        CreatePasswordHash(dto.Password, out var hash, out var salt);
        var user = new User
        {
            RoleId = role.RoleId,
            FullName = fullName,
            Email = email,
            Phone = Normalize(dto.Phone),
            PasswordHash = hash,
            PasswordSalt = salt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        await _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.Administration,
            Action = "Created",
            EntityName = "User",
            EntityId = user.UserId,
            TargetDisplayName = user.FullName,
            Summary = $"Account {user.FullName} was created for role {role.Name}."
        });

        return Ok(ApiResponse<AdminUserDto>.Ok(ToAdminUserDto(user, role.Name), "Account created"));
    }

    [HttpPut("users/{userId:long}")]
    public async Task<ActionResult<ApiResponse<AdminUserDto>>> UpdateUser(long userId, [FromBody] UpdateRoleAccountDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == userId);
        if (user is null)
        {
            return NotFound(ApiResponse<AdminUserDto>.Fail("Account not found"));
        }

        var role = await _db.Roles.FirstOrDefaultAsync(x => x.RoleId == dto.RoleId && x.IsActive);
        if (role is null || role.Name is "User")
        {
            return BadRequest(ApiResponse<AdminUserDto>.Fail("Select an active staff/admin role"));
        }

        var fullName = dto.FullName.Trim();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return BadRequest(ApiResponse<AdminUserDto>.Fail("Full name is required"));
        }

        user.FullName = fullName;
        user.Phone = Normalize(dto.Phone);
        user.RoleId = role.RoleId;
        user.IsActive = dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.Administration,
            Action = "Updated",
            EntityName = "User",
            EntityId = user.UserId,
            TargetDisplayName = user.FullName,
            Summary = $"Account {user.FullName} was updated for role {role.Name}."
        });

        return Ok(ApiResponse<AdminUserDto>.Ok(ToAdminUserDto(user, role.Name), "Account updated"));
    }

    [HttpGet("access-profile")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<AccessProfileDto>>> GetAccessProfile()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized(ApiResponse<AccessProfileDto>.Fail("Not authenticated"));
        }

        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive);
        if (user is null)
        {
            return NotFound(ApiResponse<AccessProfileDto>.Fail("Account not found"));
        }

        var role = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.RoleId == user.RoleId);
        if (role is null)
        {
            return NotFound(ApiResponse<AccessProfileDto>.Fail("Role not found"));
        }

        var permissions = await _db.RolePermissions
            .AsNoTracking()
            .Where(x => x.RoleId == user.RoleId)
            .OrderBy(x => x.ModuleName)
            .Select(x => new RolePermissionDto
            {
                ModuleKey = x.ModuleKey,
                ModuleName = x.ModuleName,
                MenuKey = x.MenuKey ?? string.Empty,
                PageRoute = x.PageRoute ?? string.Empty,
                CanAccessMenu = x.CanAccessMenu ?? false,
                CanAccessPage = x.CanAccessPage ?? false,
                CanView = x.CanView ?? false,
                CanAdd = x.CanAdd ?? false,
                CanEdit = x.CanEdit ?? false,
                CanDelete = x.CanDelete ?? false
            })
            .ToListAsync();

        return Ok(ApiResponse<AccessProfileDto>.Ok(new AccessProfileDto
        {
            Role = role.Name,
            Permissions = permissions
        }));
    }

    [HttpPut("{roleId:long}")]
    public async Task<ActionResult<ApiResponse<RoleDetailsDto>>> Update(long roleId, [FromBody] UpdateRoleDto dto)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(x => x.RoleId == roleId);
        if (role is null)
        {
            return NotFound(ApiResponse<RoleDetailsDto>.Fail("Role not found"));
        }

        var nextName = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(nextName))
        {
            return BadRequest(ApiResponse<RoleDetailsDto>.Fail("Role name is required"));
        }

        var isProtectedName = role.IsSystemRole && (role.Name == "Admin" || role.Name == "User");
        if (!isProtectedName && !string.Equals(role.Name, nextName, StringComparison.OrdinalIgnoreCase))
        {
            var duplicate = await _db.Roles.AnyAsync(x => x.RoleId != roleId && x.Name.ToLower() == nextName.ToLower());
            if (duplicate)
            {
                return BadRequest(ApiResponse<RoleDetailsDto>.Fail("Role name already exists"));
            }

            role.Name = nextName;
        }

        role.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        role.IsActive = dto.IsActive;

        await ReplacePermissionsAsync(roleId, dto.Permissions);
        await _db.SaveChangesAsync();
        await _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.Administration,
            Action = "Updated",
            EntityName = "Role",
            EntityId = role.RoleId,
            TargetDisplayName = role.Name,
            Summary = $"Role {role.Name} was updated with {dto.Permissions.Count()} permission rows."
        });

        var updated = await BuildRoleDtoAsync(roleId);
        return Ok(ApiResponse<RoleDetailsDto>.Ok(updated, "Role updated"));
    }

    private async Task ReplacePermissionsAsync(long roleId, IEnumerable<RolePermissionDto> permissions)
    {
        var existing = await _db.RolePermissions.Where(x => x.RoleId == roleId).ToListAsync();
        if (existing.Count != 0)
        {
            _db.RolePermissions.RemoveRange(existing);
            await _db.SaveChangesAsync();
        }

        var rows = permissions
            .GroupBy(x => x.ModuleKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(permission => new RolePermission
            {
                RoleId = roleId,
                ModuleKey = permission.ModuleKey.Trim(),
                ModuleName = permission.ModuleName.Trim(),
                MenuKey = permission.MenuKey.Trim(),
                PageRoute = permission.PageRoute.Trim(),
                CanAccessMenu = permission.CanAccessMenu,
                CanAccessPage = permission.CanAccessPage,
                CanView = permission.CanView,
                CanAdd = permission.CanAdd,
                CanEdit = permission.CanEdit,
                CanDelete = permission.CanDelete,
                UpdatedAt = DateTime.UtcNow
            })
            .ToList();

        if (rows.Count != 0)
        {
            _db.RolePermissions.AddRange(rows);
            await _db.SaveChangesAsync();
        }
    }

    private async Task<RoleDetailsDto> BuildRoleDtoAsync(long roleId)
    {
        var role = await _db.Roles.AsNoTracking().FirstAsync(x => x.RoleId == roleId);
        var permissions = await _db.RolePermissions
            .AsNoTracking()
            .Where(x => x.RoleId == roleId)
            .OrderBy(x => x.ModuleName)
            .Select(x => new RolePermissionDto
            {
                ModuleKey = x.ModuleKey,
                ModuleName = x.ModuleName,
                MenuKey = x.MenuKey ?? string.Empty,
                PageRoute = x.PageRoute ?? string.Empty,
                CanAccessMenu = x.CanAccessMenu ?? false,
                CanAccessPage = x.CanAccessPage ?? false,
                CanView = x.CanView ?? false,
                CanAdd = x.CanAdd ?? false,
                CanEdit = x.CanEdit ?? false,
                CanDelete = x.CanDelete ?? false
            })
            .ToListAsync();

        return new RoleDetailsDto
        {
            RoleId = role.RoleId,
            Name = role.Name,
            Description = role.Description,
            IsActive = role.IsActive,
            IsSystemRole = role.IsSystemRole,
            Permissions = permissions
        };
    }

    private static AdminUserDto ToAdminUserDto(User user, string roleName) => new()
    {
        UserId = user.UserId,
        RoleId = user.RoleId,
        RoleName = roleName,
        FullName = user.FullName,
        Email = user.Email,
        Phone = user.Phone,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };

    private static void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
    {
        using var hmac = new HMACSHA512();
        salt = hmac.Key;
        hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
