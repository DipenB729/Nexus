using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using AngularApp4.Services.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize(Policy = "AdminOnly")]
public class DepartmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _audit;

    public DepartmentsController(AppDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<DepartmentDto>>>> GetAll([FromQuery] string? search = null, [FromQuery] bool? isActive = null)
    {
        var departments = await _db.Departments.AsNoTracking().ToListAsync();
        var branches = await _db.Branches.AsNoTracking().ToDictionaryAsync(x => x.BranchId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            departments = departments
                .Where(x => x.Name.Contains(term) ||
                            x.Code.Contains(term) ||
                            (x.Description ?? string.Empty).Contains(term) ||
                            (branches.TryGetValue(x.BranchId, out var branch) && branch.Name.Contains(term)))
                .ToList();
        }

        if (isActive.HasValue)
        {
            departments = departments.Where(x => x.IsActive == isActive.Value).ToList();
        }

        var items = departments
            .OrderBy(x => branches.TryGetValue(x.BranchId, out var branch) ? branch.Name : string.Empty)
            .ThenBy(x => x.Name)
            .Select(x => MapDepartment(x, branches))
            .ToList();

        return Ok(ApiResponse<IEnumerable<DepartmentDto>>.Ok(items));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Create([FromBody] SaveDepartmentDto dto)
    {
        if (!await _db.Branches.AnyAsync(x => x.BranchId == dto.BranchId))
        {
            return BadRequest(ApiResponse<DepartmentDto>.Fail("Branch not found"));
        }

        var department = new Department
        {
            BranchId = dto.BranchId,
            Name = dto.Name.Trim(),
            Code = dto.Code.Trim().ToUpperInvariant(),
            Description = Normalize(dto.Description),
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Departments.Add(department);
        await _db.SaveChangesAsync();
        await WriteAuditAsync("Created", department.DepartmentId, department.Name, $"Department {department.Name} was created.");

        var payload = await GetDepartmentAsync(department.DepartmentId);
        return Ok(ApiResponse<DepartmentDto>.Ok(payload, "Department created"));
    }

    [HttpPut("{departmentId:long}")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Update(long departmentId, [FromBody] SaveDepartmentDto dto)
    {
        var department = await _db.Departments.FirstOrDefaultAsync(x => x.DepartmentId == departmentId);
        if (department is null)
        {
            return NotFound(ApiResponse<DepartmentDto>.Fail("Department not found"));
        }

        if (!await _db.Branches.AnyAsync(x => x.BranchId == dto.BranchId))
        {
            return BadRequest(ApiResponse<DepartmentDto>.Fail("Branch not found"));
        }

        department.BranchId = dto.BranchId;
        department.Name = dto.Name.Trim();
        department.Code = dto.Code.Trim().ToUpperInvariant();
        department.Description = Normalize(dto.Description);
        department.IsActive = dto.IsActive;
        department.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await WriteAuditAsync("Updated", department.DepartmentId, department.Name, $"Department {department.Name} was updated.");

        var payload = await GetDepartmentAsync(department.DepartmentId);
        return Ok(ApiResponse<DepartmentDto>.Ok(payload, "Department updated"));
    }

    [HttpPut("{departmentId:long}/status")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateStatus(long departmentId, [FromBody] StatusUpdateDto dto)
    {
        var department = await _db.Departments.FirstOrDefaultAsync(x => x.DepartmentId == departmentId);
        if (department is null)
        {
            return NotFound(ApiResponse<object>.Fail("Department not found"));
        }

        department.IsActive = dto.IsActive;
        department.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await WriteAuditAsync(dto.IsActive ? "Activated" : "Deactivated", department.DepartmentId, department.Name, $"Department {department.Name} was {(dto.IsActive ? "activated" : "deactivated")}.");

        return Ok(ApiResponse<object>.Ok(null, dto.IsActive ? "Department activated" : "Department deactivated"));
    }

    private Task WriteAuditAsync(string action, long entityId, string targetDisplayName, string summary)
    {
        return _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.MasterSetup,
            Action = action,
            EntityName = "Department",
            EntityId = entityId,
            TargetDisplayName = targetDisplayName,
            Summary = summary
        });
    }

    private async Task<DepartmentDto> GetDepartmentAsync(long departmentId)
    {
        var department = await _db.Departments.AsNoTracking().FirstAsync(x => x.DepartmentId == departmentId);
        var branches = await _db.Branches.AsNoTracking().ToDictionaryAsync(x => x.BranchId);
        return MapDepartment(department, branches);
    }

    private static DepartmentDto MapDepartment(Department department, IReadOnlyDictionary<long, Branch> branches) => new()
    {
        DepartmentId = department.DepartmentId,
        BranchId = department.BranchId,
        BranchName = branches.TryGetValue(department.BranchId, out var branch) ? branch.Name : string.Empty,
        Name = department.Name,
        Code = department.Code,
        Description = department.Description,
        IsActive = department.IsActive
    };

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
