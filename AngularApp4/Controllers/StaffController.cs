using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
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

    public StaffController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<StaffMasterDto>>>> GetAll([FromQuery] string? search = null, [FromQuery] bool? isActive = null)
    {
        var query = _db.Staff
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.DepartmentMaster)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.FullName.Contains(term) ||
                x.Designation.Contains(term) ||
                (x.EmployeeCode ?? string.Empty).Contains(term) ||
                (x.DepartmentMaster != null && x.DepartmentMaster.Name.Contains(term)) ||
                (x.Branch != null && x.Branch.Name.Contains(term)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        var items = await query
            .OrderBy(x => x.FullName)
            .Select(x => new StaffMasterDto
            {
                StaffId = x.StaffId,
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                DepartmentId = x.DepartmentId,
                DepartmentName = x.DepartmentMaster != null ? x.DepartmentMaster.Name : x.Department,
                FullName = x.FullName,
                EmployeeCode = x.EmployeeCode,
                Designation = x.Designation,
                Shift = x.Shift,
                Email = x.Email,
                Phone = x.Phone,
                JoinDate = x.JoinDate,
                IsActive = x.IsActive
            })
            .ToListAsync();

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
        return Ok(ApiResponse<object>.Ok(null, "Staff deactivated"));
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
        return await _db.Staff
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.DepartmentMaster)
            .Where(x => x.StaffId == staffId)
            .Select(x => new StaffMasterDto
            {
                StaffId = x.StaffId,
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                DepartmentId = x.DepartmentId,
                DepartmentName = x.DepartmentMaster != null ? x.DepartmentMaster.Name : x.Department,
                FullName = x.FullName,
                EmployeeCode = x.EmployeeCode,
                Designation = x.Designation,
                Shift = x.Shift,
                Email = x.Email,
                Phone = x.Phone,
                JoinDate = x.JoinDate,
                IsActive = x.IsActive
            })
            .FirstAsync();
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
