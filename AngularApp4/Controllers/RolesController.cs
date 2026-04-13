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

    public RolesController(AppDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
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
                    CanView = x.CanView,
                    CanAdd = x.CanAdd,
                    CanEdit = x.CanEdit,
                    CanDelete = x.CanDelete
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
                CanView = x.CanView,
                CanAdd = x.CanAdd,
                CanEdit = x.CanEdit,
                CanDelete = x.CanDelete
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
}
