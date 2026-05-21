using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using AngularApp4.Services.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/staff")]
[Authorize(Policy = "AdminOnly")]
public class StaffController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _audit;

    public StaffController(AppDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<StaffMasterDto>>>> GetAll([FromQuery] string? search = null, [FromQuery] bool? isActive = null)
    {
        var staff = await _db.Staff.AsNoTracking().ToListAsync();
        var branches = await _db.Branches.AsNoTracking().ToDictionaryAsync(x => x.BranchId);
        var departments = await _db.Departments.AsNoTracking().ToDictionaryAsync(x => x.DepartmentId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            staff = staff.Where(x =>
                x.FullName.Contains(term) ||
                x.Designation.Contains(term) ||
                (x.EmployeeCode ?? string.Empty).Contains(term) ||
                (x.DepartmentId.HasValue && departments.TryGetValue(x.DepartmentId.Value, out var department) && department.Name.Contains(term)) ||
                (x.BranchId.HasValue && branches.TryGetValue(x.BranchId.Value, out var branch) && branch.Name.Contains(term)))
                .ToList();
        }

        if (isActive.HasValue)
        {
            staff = staff.Where(x => x.IsActive == isActive.Value).ToList();
        }

        var items = staff
            .OrderBy(x => x.FullName)
            .Select(x => MapStaff(x, branches, departments))
            .ToList();

        return Ok(ApiResponse<IEnumerable<StaffMasterDto>>.Ok(items));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<StaffMasterDto>>> Create([FromBody] SaveStaffDto dto)
    {
        var validation = await ValidateMappingsAsync(dto.BranchId, dto.DepartmentId);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<StaffMasterDto>.Fail(validation));
        }

        var departmentName = await ResolveDepartmentNameAsync(dto.DepartmentId);
        var staff = new Staff
        {
            BranchId = dto.BranchId,
            DepartmentId = dto.DepartmentId,
            Department = departmentName ?? string.Empty,
            FullName = dto.FullName.Trim(),
            EmployeeCode = Normalize(dto.EmployeeCode),
            Designation = dto.Designation.Trim(),
            Shift = Normalize(dto.Shift),
            Email = dto.Email.Trim().ToLowerInvariant(),
            Phone = Normalize(dto.Phone),
            JoinDate = dto.JoinDate.Date,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Staff.Add(staff);
        await _db.SaveChangesAsync();
        await WriteAuditAsync("Created", staff.StaffId, staff.FullName, $"Staff member {staff.FullName} was created.");

        var payload = await GetStaffDtoAsync(staff.StaffId);
        return Ok(ApiResponse<StaffMasterDto>.Ok(payload, "Staff created"));
    }

    [HttpPut("{staffId:long}")]
    public async Task<ActionResult<ApiResponse<StaffMasterDto>>> Update(long staffId, [FromBody] SaveStaffDto dto)
    {
        var item = await _db.Staff.FindAsync(staffId);
        if (item is null) return NotFound(ApiResponse<StaffMasterDto>.Fail("Staff not found"));

        var validation = await ValidateMappingsAsync(dto.BranchId, dto.DepartmentId);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<StaffMasterDto>.Fail(validation));
        }

        item.BranchId = dto.BranchId;
        item.DepartmentId = dto.DepartmentId;
        item.Department = await ResolveDepartmentNameAsync(dto.DepartmentId) ?? string.Empty;
        item.FullName = dto.FullName.Trim();
        item.EmployeeCode = Normalize(dto.EmployeeCode);
        item.Designation = dto.Designation.Trim();
        item.Shift = Normalize(dto.Shift);
        item.Email = dto.Email.Trim().ToLowerInvariant();
        item.Phone = Normalize(dto.Phone);
        item.JoinDate = dto.JoinDate.Date;
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await WriteAuditAsync("Updated", item.StaffId, item.FullName, $"Staff member {item.FullName} was updated.");

        var payload = await GetStaffDtoAsync(item.StaffId);
        return Ok(ApiResponse<StaffMasterDto>.Ok(payload, "Staff updated"));
    }

    [HttpPut("{staffId:long}/status")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateStatus(long staffId, [FromBody] StatusUpdateDto dto)
    {
        var item = await _db.Staff.FindAsync(staffId);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Staff not found"));

        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await WriteAuditAsync(dto.IsActive ? "Activated" : "Deactivated", item.StaffId, item.FullName, $"Staff member {item.FullName} was {(dto.IsActive ? "activated" : "deactivated")}.");

        return Ok(ApiResponse<object>.Ok(null, dto.IsActive ? "Staff activated" : "Staff deactivated"));
    }

    [HttpDelete("{staffId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long staffId)
    {
        var item = await _db.Staff.FindAsync(staffId);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Staff not found"));
        item.IsActive = false;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await WriteAuditAsync("Deleted", item.StaffId, item.FullName, $"Staff member {item.FullName} was deactivated from the master list.");
        return Ok(ApiResponse<object>.Ok(null, "Staff deactivated"));
    }

    private Task WriteAuditAsync(string action, long entityId, string targetDisplayName, string summary)
    {
        return _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.MasterSetup,
            Action = action,
            EntityName = "Staff",
            EntityId = entityId,
            TargetDisplayName = targetDisplayName,
            Summary = summary
        });
    }

    private async Task<string?> ValidateMappingsAsync(long? branchId, long? departmentId)
    {
        if (branchId.HasValue && !await _db.Branches.AnyAsync(x => x.BranchId == branchId.Value))
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

            if (branchId.HasValue && department.BranchId != branchId.Value)
            {
                return "Department does not belong to the selected branch";
            }
        }

        return null;
    }

    private async Task<string?> ResolveDepartmentNameAsync(long? departmentId)
    {
        if (!departmentId.HasValue)
        {
            return null;
        }

        return await _db.Departments
            .Where(x => x.DepartmentId == departmentId.Value)
            .Select(x => x.Name)
            .FirstOrDefaultAsync();
    }

    private async Task<StaffMasterDto> GetStaffDtoAsync(long staffId)
    {
        var staff = await _db.Staff.AsNoTracking().FirstAsync(x => x.StaffId == staffId);
        var branches = await _db.Branches.AsNoTracking().ToDictionaryAsync(x => x.BranchId);
        var departments = await _db.Departments.AsNoTracking().ToDictionaryAsync(x => x.DepartmentId);
        return MapStaff(staff, branches, departments);
    }

    private static StaffMasterDto MapStaff(Staff staff, IReadOnlyDictionary<long, Branch> branches, IReadOnlyDictionary<long, Department> departments) => new()
    {
        StaffId = staff.StaffId,
        BranchId = staff.BranchId,
        BranchName = staff.BranchId.HasValue && branches.TryGetValue(staff.BranchId.Value, out var branch) ? branch.Name : string.Empty,
        DepartmentId = staff.DepartmentId,
        DepartmentName = staff.DepartmentId.HasValue && departments.TryGetValue(staff.DepartmentId.Value, out var department) ? department.Name : staff.Department,
        FullName = staff.FullName,
        EmployeeCode = staff.EmployeeCode,
        Designation = staff.Designation,
        Shift = staff.Shift,
        Email = staff.Email,
        Phone = staff.Phone,
        JoinDate = staff.JoinDate,
        IsActive = staff.IsActive
    };

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
