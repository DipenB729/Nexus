using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
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

    public WardsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<WardDto>>>> GetAll([FromQuery] string? search = null, [FromQuery] bool? isActive = null)
    {
        var query = _db.Wards
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Department)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.Name.Contains(term) ||
                x.WardType.Contains(term) ||
                x.RoomType.Contains(term) ||
                x.Branch!.Name.Contains(term) ||
                (x.Department != null && x.Department.Name.Contains(term)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        var items = await query
            .OrderBy(x => x.Branch!.Name)
            .ThenBy(x => x.Name)
            .Select(x => new WardDto
            {
                WardId = x.WardId,
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                DepartmentId = x.DepartmentId,
                DepartmentName = x.Department != null ? x.Department.Name : string.Empty,
                Name = x.Name,
                WardType = x.WardType,
                RoomType = x.RoomType,
                ChargePerDay = x.ChargePerDay,
                IsActive = x.IsActive
            })
            .ToListAsync();

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

        return Ok(ApiResponse<object>.Ok(null, dto.IsActive ? "Ward activated" : "Ward deactivated"));
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
        return await _db.Wards
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Department)
            .Where(x => x.WardId == wardId)
            .Select(x => new WardDto
            {
                WardId = x.WardId,
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                DepartmentId = x.DepartmentId,
                DepartmentName = x.Department != null ? x.Department.Name : string.Empty,
                Name = x.Name,
                WardType = x.WardType,
                RoomType = x.RoomType,
                ChargePerDay = x.ChargePerDay,
                IsActive = x.IsActive
            })
            .FirstAsync();
    }
}
