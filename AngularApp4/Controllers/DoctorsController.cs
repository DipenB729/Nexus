using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/doctors")]
public class DoctorsController : ControllerBase
{
    private readonly AppDbContext _db;

    public DoctorsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<Doctor>>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var query = _db.Doctors.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.FullName.Contains(search) || x.Specialization.Contains(search));
        }

        var total = await query.CountAsync();
        var items = await query.OrderBy(x => x.FullName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(ApiResponse<PagedResult<Doctor>>.Ok(new PagedResult<Doctor>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        }));
    }

    [HttpGet("{doctorId:long}")]
    public async Task<ActionResult<ApiResponse<Doctor>>> GetById(long doctorId)
    {
        var doctor = await _db.Doctors.FindAsync(doctorId);
        return doctor is null
            ? NotFound(ApiResponse<Doctor>.Fail("Doctor not found"))
            : Ok(ApiResponse<Doctor>.Ok(doctor));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<Doctor>>> Create(Doctor doctor)
    {
        doctor.CreatedAt = DateTime.UtcNow;
        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<Doctor>.Ok(doctor, "Doctor created"));
    }

    [HttpPut("{doctorId:long}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<Doctor>>> Update(long doctorId, Doctor dto)
    {
        var doctor = await _db.Doctors.FindAsync(doctorId);
        if (doctor is null) return NotFound(ApiResponse<Doctor>.Fail("Doctor not found"));

        doctor.FullName = dto.FullName;
        doctor.Specialization = dto.Specialization;
        doctor.Email = dto.Email;
        doctor.Phone = dto.Phone;
        doctor.ExperienceYears = dto.ExperienceYears;
        doctor.Qualification = dto.Qualification;
        doctor.ConsultationFee = dto.ConsultationFee;
        doctor.IsActive = dto.IsActive;
        doctor.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ApiResponse<Doctor>.Ok(doctor, "Doctor updated"));
    }

    [HttpDelete("{doctorId:long}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long doctorId)
    {
        var doctor = await _db.Doctors.FindAsync(doctorId);
        if (doctor is null) return NotFound(ApiResponse<object>.Fail("Doctor not found"));

        _db.Doctors.Remove(doctor);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null, "Doctor deleted"));
    }

    [HttpGet("{doctorId:long}/available-slots")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetAvailableSlots(long doctorId, [FromQuery] DateTime date)
    {
        var dayOfWeek = ((int)date.DayOfWeek + 6) % 7 + 1;
        var schedule = await _db.DoctorSchedules.FirstOrDefaultAsync(x => x.DoctorId == doctorId && x.DayOfWeek == dayOfWeek && x.IsActive);
        if (schedule is null) return Ok(ApiResponse<IEnumerable<object>>.Ok(Array.Empty<object>(), "No schedule"));

        var booked = await _db.Appointments
            .Where(x => x.DoctorId == doctorId && x.AppointmentDate.Date == date.Date && x.Status != AppointmentStatus.Cancelled)
            .Select(x => x.SlotStartTime)
            .ToListAsync();

        var slots = new List<object>();
        var cursor = schedule.StartTime;
        while (cursor + TimeSpan.FromMinutes(schedule.SlotDurationMinutes) <= schedule.EndTime)
        {
            var end = cursor + TimeSpan.FromMinutes(schedule.SlotDurationMinutes);
            if (!booked.Contains(cursor))
            {
                slots.Add(new { StartTime = cursor, EndTime = end });
            }
            cursor = end;
        }

        return Ok(ApiResponse<IEnumerable<object>>.Ok(slots));
    }
}
