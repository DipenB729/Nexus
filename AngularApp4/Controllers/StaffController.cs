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
    public async Task<ActionResult<ApiResponse<PagedResult<Staff>>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var query = _db.Staff.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.FullName.Contains(search) || x.Department.Contains(search));

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(ApiResponse<PagedResult<Staff>>.Ok(new PagedResult<Staff> { Items = items, Page = page, PageSize = pageSize, TotalCount = total }));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Staff>>> Create(Staff dto)
    {
        _db.Staff.Add(dto);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<Staff>.Ok(dto, "Staff created"));
    }

    [HttpPut("{staffId:long}")]
    public async Task<ActionResult<ApiResponse<Staff>>> Update(long staffId, Staff dto)
    {
        var item = await _db.Staff.FindAsync(staffId);
        if (item is null) return NotFound(ApiResponse<Staff>.Fail("Staff not found"));

        item.FullName = dto.FullName;
        item.Department = dto.Department;
        item.Designation = dto.Designation;
        item.Email = dto.Email;
        item.Phone = dto.Phone;
        item.JoinDate = dto.JoinDate;
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<Staff>.Ok(item, "Staff updated"));
    }

    [HttpDelete("{staffId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long staffId)
    {
        var item = await _db.Staff.FindAsync(staffId);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Staff not found"));
        _db.Staff.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null, "Staff deleted"));
    }
}
