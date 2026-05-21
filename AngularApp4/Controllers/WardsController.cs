using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using AngularApp4.Services.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/wards")]
[Authorize(Policy = "AdminOnly")]
public class WardsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _audit;

    public WardsController(AppDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<WardDto>>>> GetAll([FromQuery] string? search = null, [FromQuery] bool? isActive = null)
    {
        var wards = await _db.Wards.AsNoTracking().ToListAsync();
        var branches = await _db.Branches.AsNoTracking().ToDictionaryAsync(x => x.BranchId);
        var departments = await _db.Departments.AsNoTracking().ToDictionaryAsync(x => x.DepartmentId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            wards = wards.Where(x =>
                x.Name.Contains(term) ||
                x.WardType.Contains(term) ||
                x.RoomType.Contains(term) ||
                (branches.TryGetValue(x.BranchId, out var branch) && branch.Name.Contains(term)) ||
                (x.DepartmentId.HasValue && departments.TryGetValue(x.DepartmentId.Value, out var department) && department.Name.Contains(term)))
                .ToList();
        }

        if (isActive.HasValue)
        {
            wards = wards.Where(x => x.IsActive == isActive.Value).ToList();
        }

        var items = wards
            .OrderBy(x => branches.TryGetValue(x.BranchId, out var branch) ? branch.Name : string.Empty)
            .ThenBy(x => x.Name)
            .Select(x => MapWard(x, branches, departments))
            .ToList();

        return Ok(ApiResponse<IEnumerable<WardDto>>.Ok(items));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<WardDto>>> Create([FromBody] SaveWardDto dto)
    {
        var validation = await ValidateMappingsAsync(dto.BranchId, dto.DepartmentId);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<WardDto>.Fail(validation));
        }

        var item = new Ward
        {
            BranchId = dto.BranchId,
            DepartmentId = dto.DepartmentId,
            Name = dto.Name.Trim(),
            WardType = dto.WardType.Trim(),
            RoomType = dto.RoomType.Trim(),
            ChargePerDay = dto.ChargePerDay,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Wards.Add(item);
        await _db.SaveChangesAsync();
        await WriteAuditAsync("Created", item.WardId, item.Name, $"Ward {item.Name} was created.");

        var payload = await GetWardAsync(item.WardId);
        return Ok(ApiResponse<WardDto>.Ok(payload, "Ward created"));
    }

    [HttpPut("{wardId:long}")]
    public async Task<ActionResult<ApiResponse<WardDto>>> Update(long wardId, [FromBody] SaveWardDto dto)
    {
        var item = await _db.Wards.FirstOrDefaultAsync(x => x.WardId == wardId);
        if (item is null)
        {
            return NotFound(ApiResponse<WardDto>.Fail("Ward not found"));
        }

        var validation = await ValidateMappingsAsync(dto.BranchId, dto.DepartmentId);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<WardDto>.Fail(validation));
        }

        item.BranchId = dto.BranchId;
        item.DepartmentId = dto.DepartmentId;
        item.Name = dto.Name.Trim();
        item.WardType = dto.WardType.Trim();
        item.RoomType = dto.RoomType.Trim();
        item.ChargePerDay = dto.ChargePerDay;
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await WriteAuditAsync("Updated", item.WardId, item.Name, $"Ward {item.Name} was updated.");

        var payload = await GetWardAsync(item.WardId);
        return Ok(ApiResponse<WardDto>.Ok(payload, "Ward updated"));
    }

    [HttpPut("{wardId:long}/status")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateStatus(long wardId, [FromBody] StatusUpdateDto dto)
    {
        var item = await _db.Wards.FirstOrDefaultAsync(x => x.WardId == wardId);
        if (item is null)
        {
            return NotFound(ApiResponse<object>.Fail("Ward not found"));
        }

        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await WriteAuditAsync(dto.IsActive ? "Activated" : "Deactivated", item.WardId, item.Name, $"Ward {item.Name} was {(dto.IsActive ? "activated" : "deactivated")}.");

        return Ok(ApiResponse<object>.Ok(null, dto.IsActive ? "Ward activated" : "Ward deactivated"));
    }

    private Task WriteAuditAsync(string action, long entityId, string targetDisplayName, string summary)
    {
        return _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.MasterSetup,
            Action = action,
            EntityName = "Ward",
            EntityId = entityId,
            TargetDisplayName = targetDisplayName,
            Summary = summary
        });
    }

    private async Task<string?> ValidateMappingsAsync(long branchId, long? departmentId)
    {
        if (!await _db.Branches.AnyAsync(x => x.BranchId == branchId))
        {
            return "Branch not found";
        }

        if (departmentId.HasValue)
        {
            var department = await _db.Departments.FirstOrDefaultAsync(x => x.DepartmentId == departmentId.Value);
            if (department is null)
            {
                return "Department not found";
            }

            if (department.BranchId != branchId)
            {
                return "Department does not belong to the selected branch";
            }
        }

        return null;
    }

    private async Task<WardDto> GetWardAsync(long wardId)
    {
        var ward = await _db.Wards.AsNoTracking().FirstAsync(x => x.WardId == wardId);
        var branches = await _db.Branches.AsNoTracking().ToDictionaryAsync(x => x.BranchId);
        var departments = await _db.Departments.AsNoTracking().ToDictionaryAsync(x => x.DepartmentId);
        return MapWard(ward, branches, departments);
    }

    private static WardDto MapWard(Ward ward, IReadOnlyDictionary<long, Branch> branches, IReadOnlyDictionary<long, Department> departments) => new()
    {
        WardId = ward.WardId,
        BranchId = ward.BranchId,
        BranchName = branches.TryGetValue(ward.BranchId, out var branch) ? branch.Name : string.Empty,
        DepartmentId = ward.DepartmentId,
        DepartmentName = ward.DepartmentId.HasValue && departments.TryGetValue(ward.DepartmentId.Value, out var department) ? department.Name : string.Empty,
        Name = ward.Name,
        WardType = ward.WardType,
        RoomType = ward.RoomType,
        ChargePerDay = ward.ChargePerDay,
        IsActive = ward.IsActive
    };
}
