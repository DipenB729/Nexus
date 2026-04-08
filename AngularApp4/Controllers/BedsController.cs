using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/beds")]
[Authorize(Policy = "AdminOnly")]
public class BedsController : ControllerBase
{
    private readonly AppDbContext _db;

    public BedsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<BedDto>>>> GetAll([FromQuery] string? search = null, [FromQuery] bool? isActive = null)
    {
        var query = _db.Beds
            .AsNoTracking()
            .Include(x => x.Ward)
            .Include(x => x.Branch)
            .Include(x => x.Department)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.BedNumber.Contains(term) ||
                x.Ward!.Name.Contains(term) ||
                x.Branch!.Name.Contains(term) ||
                (x.Department != null && x.Department.Name.Contains(term)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        var items = await query
            .OrderBy(x => x.Branch!.Name)
            .ThenBy(x => x.Ward!.Name)
            .ThenBy(x => x.BedNumber)
            .Select(x => new BedDto
            {
                BedId = x.BedId,
                WardId = x.WardId,
                WardName = x.Ward != null ? x.Ward.Name : string.Empty,
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                DepartmentId = x.DepartmentId,
                DepartmentName = x.Department != null ? x.Department.Name : string.Empty,
                BedNumber = x.BedNumber,
                ChargePerDay = x.ChargePerDay,
                IsOccupied = x.IsOccupied,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<BedDto>>.Ok(items));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BedDto>>> Create([FromBody] SaveBedDto dto)
    {
        var validation = await ValidateMappingsAsync(dto.WardId, dto.BranchId, dto.DepartmentId);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<BedDto>.Fail(validation));
        }

        var item = new Bed
        {
            WardId = dto.WardId,
            BranchId = dto.BranchId,
            DepartmentId = dto.DepartmentId,
            BedNumber = dto.BedNumber.Trim().ToUpperInvariant(),
            ChargePerDay = dto.ChargePerDay,
            IsOccupied = dto.IsOccupied,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Beds.Add(item);
        await _db.SaveChangesAsync();

        var payload = await GetBedAsync(item.BedId);
        return Ok(ApiResponse<BedDto>.Ok(payload, "Bed created"));
    }

    [HttpPut("{bedId:long}")]
    public async Task<ActionResult<ApiResponse<BedDto>>> Update(long bedId, [FromBody] SaveBedDto dto)
    {
        var item = await _db.Beds.FirstOrDefaultAsync(x => x.BedId == bedId);
        if (item is null)
        {
            return NotFound(ApiResponse<BedDto>.Fail("Bed not found"));
        }

        var validation = await ValidateMappingsAsync(dto.WardId, dto.BranchId, dto.DepartmentId);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<BedDto>.Fail(validation));
        }

        item.WardId = dto.WardId;
        item.BranchId = dto.BranchId;
        item.DepartmentId = dto.DepartmentId;
        item.BedNumber = dto.BedNumber.Trim().ToUpperInvariant();
        item.ChargePerDay = dto.ChargePerDay;
        item.IsOccupied = dto.IsOccupied;
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var payload = await GetBedAsync(item.BedId);
        return Ok(ApiResponse<BedDto>.Ok(payload, "Bed updated"));
    }

    [HttpPut("{bedId:long}/status")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateStatus(long bedId, [FromBody] StatusUpdateDto dto)
    {
        var item = await _db.Beds.FirstOrDefaultAsync(x => x.BedId == bedId);
        if (item is null)
        {
            return NotFound(ApiResponse<object>.Fail("Bed not found"));
        }

        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(null, dto.IsActive ? "Bed activated" : "Bed deactivated"));
    }

    private async Task<string?> ValidateMappingsAsync(long wardId, long branchId, long? departmentId)
    {
        var ward = await _db.Wards.FirstOrDefaultAsync(x => x.WardId == wardId);
        if (ward is null)
        {
            return "Ward not found";
        }

        if (ward.BranchId != branchId)
        {
            return "Ward does not belong to the selected branch";
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

    private async Task<BedDto> GetBedAsync(long bedId)
    {
        return await _db.Beds
            .AsNoTracking()
            .Include(x => x.Ward)
            .Include(x => x.Branch)
            .Include(x => x.Department)
            .Where(x => x.BedId == bedId)
            .Select(x => new BedDto
            {
                BedId = x.BedId,
                WardId = x.WardId,
                WardName = x.Ward != null ? x.Ward.Name : string.Empty,
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                DepartmentId = x.DepartmentId,
                DepartmentName = x.Department != null ? x.Department.Name : string.Empty,
                BedNumber = x.BedNumber,
                ChargePerDay = x.ChargePerDay,
                IsOccupied = x.IsOccupied,
                IsActive = x.IsActive
            })
            .FirstAsync();
    }
}
