using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using AngularApp4.Services.Hms;
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
    private readonly IAuditLogService _audit;

    public BedsController(AppDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<BedDto>>>> GetAll([FromQuery] string? search = null, [FromQuery] bool? isActive = null)
    {
        var beds = await _db.Beds.AsNoTracking().ToListAsync();
        var wards = await _db.Wards.AsNoTracking().ToDictionaryAsync(x => x.WardId);
        var branches = await _db.Branches.AsNoTracking().ToDictionaryAsync(x => x.BranchId);
        var departments = await _db.Departments.AsNoTracking().ToDictionaryAsync(x => x.DepartmentId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            beds = beds.Where(x =>
                x.BedNumber.Contains(term) ||
                (wards.TryGetValue(x.WardId, out var ward) && ward.Name.Contains(term)) ||
                (branches.TryGetValue(x.BranchId, out var branch) && branch.Name.Contains(term)) ||
                (x.DepartmentId.HasValue && departments.TryGetValue(x.DepartmentId.Value, out var department) && department.Name.Contains(term)))
                .ToList();
        }

        if (isActive.HasValue)
        {
            beds = beds.Where(x => x.IsActive == isActive.Value).ToList();
        }

        var items = beds
            .OrderBy(x => branches.TryGetValue(x.BranchId, out var branch) ? branch.Name : string.Empty)
            .ThenBy(x => wards.TryGetValue(x.WardId, out var ward) ? ward.Name : string.Empty)
            .ThenBy(x => x.BedNumber)
            .Select(x => MapBed(x, wards, branches, departments))
            .ToList();

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
        await WriteAuditAsync("Created", item.BedId, item.BedNumber, $"Bed {item.BedNumber} was created.");

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
        await WriteAuditAsync("Updated", item.BedId, item.BedNumber, $"Bed {item.BedNumber} was updated.");

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
        await WriteAuditAsync(dto.IsActive ? "Activated" : "Deactivated", item.BedId, item.BedNumber, $"Bed {item.BedNumber} was {(dto.IsActive ? "activated" : "deactivated")}.");

        return Ok(ApiResponse<object>.Ok(null, dto.IsActive ? "Bed activated" : "Bed deactivated"));
    }

    private Task WriteAuditAsync(string action, long entityId, string targetDisplayName, string summary)
    {
        return _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.MasterSetup,
            Action = action,
            EntityName = "Bed",
            EntityId = entityId,
            TargetDisplayName = targetDisplayName,
            Summary = summary
        });
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
        var bed = await _db.Beds.AsNoTracking().FirstAsync(x => x.BedId == bedId);
        var wards = await _db.Wards.AsNoTracking().ToDictionaryAsync(x => x.WardId);
        var branches = await _db.Branches.AsNoTracking().ToDictionaryAsync(x => x.BranchId);
        var departments = await _db.Departments.AsNoTracking().ToDictionaryAsync(x => x.DepartmentId);
        return MapBed(bed, wards, branches, departments);
    }

    private static BedDto MapBed(Bed bed, IReadOnlyDictionary<long, Ward> wards, IReadOnlyDictionary<long, Branch> branches, IReadOnlyDictionary<long, Department> departments) => new()
    {
        BedId = bed.BedId,
        WardId = bed.WardId,
        WardName = wards.TryGetValue(bed.WardId, out var ward) ? ward.Name : string.Empty,
        BranchId = bed.BranchId,
        BranchName = branches.TryGetValue(bed.BranchId, out var branch) ? branch.Name : string.Empty,
        DepartmentId = bed.DepartmentId,
        DepartmentName = bed.DepartmentId.HasValue && departments.TryGetValue(bed.DepartmentId.Value, out var department) ? department.Name : string.Empty,
        BedNumber = bed.BedNumber,
        ChargePerDay = bed.ChargePerDay,
        IsOccupied = bed.IsOccupied,
        IsActive = bed.IsActive
    };
}
