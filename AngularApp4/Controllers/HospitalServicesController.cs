using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/services")]
public class HospitalServicesController : ControllerBase
{
    private readonly AppDbContext _db;

    public HospitalServicesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<HospitalService>>>> GetAll()
    {
        var items = await _db.HospitalServices.Where(x => x.IsActive).OrderBy(x => x.ServiceName).ToListAsync();
        return Ok(ApiResponse<IEnumerable<HospitalService>>.Ok(items));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<HospitalService>>> Create(HospitalService dto)
    {
        _db.HospitalServices.Add(dto);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<HospitalService>.Ok(dto, "Service created"));
    }

    [HttpPut("{serviceId:long}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<HospitalService>>> Update(long serviceId, HospitalService dto)
    {
        var item = await _db.HospitalServices.FindAsync(serviceId);
        if (item is null) return NotFound(ApiResponse<HospitalService>.Fail("Service not found"));

        item.ServiceName = dto.ServiceName;
        item.Description = dto.Description;
        item.Price = dto.Price;
        item.EstimatedDurationMinutes = dto.EstimatedDurationMinutes;
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<HospitalService>.Ok(item, "Service updated"));
    }

    [HttpDelete("{serviceId:long}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long serviceId)
    {
        var item = await _db.HospitalServices.FindAsync(serviceId);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Service not found"));
        _db.HospitalServices.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null, "Service deleted"));
    }
}
