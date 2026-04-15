using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using AngularApp4.Services.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api")]
[Authorize(Policy = "AdminOnly")]
public class DoctorSchedulesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IDoctorAvailabilityService _availability;

    public DoctorSchedulesController(AppDbContext db, IDoctorAvailabilityService availability)
    {
        _db = db;
        _availability = availability;
    }

    [HttpGet("doctors/{doctorId:long}/schedules")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DoctorScheduleDto>>>> GetByDoctor(long doctorId)
    {
        var doctorExists = await _db.Doctors.AnyAsync(x => x.DoctorId == doctorId);
        if (!doctorExists)
        {
            return NotFound(ApiResponse<IEnumerable<DoctorScheduleDto>>.Fail("Doctor not found"));
        }

        var items = await _db.DoctorSchedules
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId)
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.StartTime)
            .Select(x => new DoctorScheduleDto
            {
                ScheduleId = x.ScheduleId,
                DoctorId = x.DoctorId,
                DayOfWeek = x.DayOfWeek,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                SlotDurationMinutes = x.SlotDurationMinutes,
                MaxPatientsPerSlot = x.MaxPatientsPerSlot,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<DoctorScheduleDto>>.Ok(items));
    }

    [HttpPost("doctors/{doctorId:long}/schedules")]
    public async Task<ActionResult<ApiResponse<DoctorScheduleDto>>> Create(long doctorId, [FromBody] SaveDoctorScheduleDto dto)
    {
        var doctorExists = await _db.Doctors.AnyAsync(x => x.DoctorId == doctorId);
        if (!doctorExists)
        {
            return NotFound(ApiResponse<DoctorScheduleDto>.Fail("Doctor not found"));
        }

        var validation = await ValidateScheduleAsync(doctorId, dto);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<DoctorScheduleDto>.Fail(validation));
        }

        var item = new DoctorSchedule
        {
            DoctorId = doctorId,
            DayOfWeek = dto.DayOfWeek,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            SlotDurationMinutes = dto.SlotDurationMinutes,
            MaxPatientsPerSlot = dto.MaxPatientsPerSlot,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.DoctorSchedules.Add(item);
        await _db.SaveChangesAsync();
        await _availability.SyncDoctorAvailabilitySummaryAsync(doctorId);

        return Ok(ApiResponse<DoctorScheduleDto>.Ok(MapSchedule(item), "Schedule created"));
    }

    [HttpPut("schedules/{scheduleId:long}")]
    public async Task<ActionResult<ApiResponse<DoctorScheduleDto>>> Update(long scheduleId, [FromBody] SaveDoctorScheduleDto dto)
    {
        var item = await _db.DoctorSchedules.FindAsync(scheduleId);
        if (item is null) return NotFound(ApiResponse<DoctorScheduleDto>.Fail("Schedule not found"));

        var validation = await ValidateScheduleAsync(item.DoctorId, dto, scheduleId);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<DoctorScheduleDto>.Fail(validation));
        }

        item.DayOfWeek = dto.DayOfWeek;
        item.StartTime = dto.StartTime;
        item.EndTime = dto.EndTime;
        item.SlotDurationMinutes = dto.SlotDurationMinutes;
        item.MaxPatientsPerSlot = dto.MaxPatientsPerSlot;
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _availability.SyncDoctorAvailabilitySummaryAsync(item.DoctorId);

        return Ok(ApiResponse<DoctorScheduleDto>.Ok(MapSchedule(item), "Schedule updated"));
    }

    [HttpDelete("schedules/{scheduleId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long scheduleId)
    {
        var item = await _db.DoctorSchedules.FindAsync(scheduleId);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Schedule not found"));

        var doctorId = item.DoctorId;
        _db.DoctorSchedules.Remove(item);
        await _db.SaveChangesAsync();
        await _availability.SyncDoctorAvailabilitySummaryAsync(doctorId);

        return Ok(ApiResponse<object>.Ok(null, "Schedule deleted"));
    }

    private async Task<string?> ValidateScheduleAsync(long doctorId, SaveDoctorScheduleDto dto, long? existingScheduleId = null)
    {
        if (dto.DayOfWeek is < 1 or > 7)
        {
            return "Select a valid day of the week";
        }

        if (dto.EndTime <= dto.StartTime)
        {
            return "End time must be after start time";
        }

        if (dto.SlotDurationMinutes < 5)
        {
            return "Slot duration must be at least 5 minutes";
        }

        if (dto.MaxPatientsPerSlot < 1)
        {
            return "Max patients per slot must be at least 1";
        }

        var totalMinutes = (dto.EndTime - dto.StartTime).TotalMinutes;
        if (totalMinutes < dto.SlotDurationMinutes || totalMinutes % dto.SlotDurationMinutes != 0)
        {
            return "Schedule window must divide evenly into the slot duration";
        }

        if (!dto.IsActive)
        {
            return null;
        }

        var overlap = await _db.DoctorSchedules.AnyAsync(x =>
            x.DoctorId == doctorId &&
            x.ScheduleId != existingScheduleId &&
            x.IsActive &&
            x.DayOfWeek == dto.DayOfWeek &&
            dto.StartTime < x.EndTime &&
            x.StartTime < dto.EndTime);

        return overlap ? "Schedule overlap detected" : null;
    }

    private static DoctorScheduleDto MapSchedule(DoctorSchedule item)
    {
        return new DoctorScheduleDto
        {
            ScheduleId = item.ScheduleId,
            DoctorId = item.DoctorId,
            DayOfWeek = item.DayOfWeek,
            StartTime = item.StartTime,
            EndTime = item.EndTime,
            SlotDurationMinutes = item.SlotDurationMinutes,
            MaxPatientsPerSlot = item.MaxPatientsPerSlot,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
