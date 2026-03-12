using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api")]
public class DoctorSchedulesController : ControllerBase
{
    private readonly AppDbContext _db;

    public DoctorSchedulesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("doctors/{doctorId:long}/schedules")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DoctorSchedule>>>> GetByDoctor(long doctorId)
    {
        var items = await _db.DoctorSchedules.Where(x => x.DoctorId == doctorId).ToListAsync();
        return Ok(ApiResponse<IEnumerable<DoctorSchedule>>.Ok(items));
    }

    [HttpPost("doctors/{doctorId:long}/schedules")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<DoctorSchedule>>> Create(long doctorId, DoctorSchedule dto)
    {
        var overlap = await _db.DoctorSchedules.AnyAsync(x => x.DoctorId == doctorId && x.DayOfWeek == dto.DayOfWeek && dto.StartTime < x.EndTime && x.StartTime < dto.EndTime);
        if (overlap) return BadRequest(ApiResponse<DoctorSchedule>.Fail("Schedule overlap detected"));

        dto.DoctorId = doctorId;
        dto.CreatedAt = DateTime.UtcNow;
        _db.DoctorSchedules.Add(dto);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<DoctorSchedule>.Ok(dto, "Schedule created"));
    }

    [HttpPut("schedules/{scheduleId:long}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<DoctorSchedule>>> Update(long scheduleId, DoctorSchedule dto)
    {
        var item = await _db.DoctorSchedules.FindAsync(scheduleId);
        if (item is null) return NotFound(ApiResponse<DoctorSchedule>.Fail("Schedule not found"));

        item.DayOfWeek = dto.DayOfWeek;
        item.StartTime = dto.StartTime;
        item.EndTime = dto.EndTime;
        item.SlotDurationMinutes = dto.SlotDurationMinutes;
        item.MaxPatientsPerSlot = dto.MaxPatientsPerSlot;
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ApiResponse<DoctorSchedule>.Ok(item, "Schedule updated"));
    }

    [HttpDelete("schedules/{scheduleId:long}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long scheduleId)
    {
        var item = await _db.DoctorSchedules.FindAsync(scheduleId);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Schedule not found"));

        _db.DoctorSchedules.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null, "Schedule deleted"));
    }
}
