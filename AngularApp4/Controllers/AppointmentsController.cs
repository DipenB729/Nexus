using System.Security.Claims;
using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AppointmentsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    [Authorize(Policy = "UserOnly")]
    public async Task<ActionResult<ApiResponse<Appointment>>> Create([FromBody] AppointmentCreateDto dto)
    {
        var userId = GetUserId();
        var patient = await _db.Patients.FirstOrDefaultAsync(x => x.UserId == userId);
        if (patient is null) return BadRequest(ApiResponse<Appointment>.Fail("Patient profile not found"));

        var doctorExists = await _db.Doctors.AnyAsync(x => x.DoctorId == dto.DoctorId && x.IsActive);
        if (!doctorExists) return BadRequest(ApiResponse<Appointment>.Fail("Doctor not found/inactive"));

        var isBooked = await _db.Appointments.AnyAsync(x =>
            x.DoctorId == dto.DoctorId &&
            x.AppointmentDate.Date == dto.AppointmentDate.Date &&
            x.SlotStartTime == dto.SlotStartTime &&
            x.Status != AppointmentStatus.Cancelled);

        if (isBooked) return BadRequest(ApiResponse<Appointment>.Fail("Selected slot is already booked"));

        var appointment = new Appointment
        {
            PatientId = patient.PatientId,
            DoctorId = dto.DoctorId,
            ScheduleId = dto.ScheduleId,
            ServiceId = dto.ServiceId,
            AppointmentDate = dto.AppointmentDate.Date,
            SlotStartTime = dto.SlotStartTime,
            SlotEndTime = dto.SlotEndTime,
            Reason = dto.Reason,
            CreatedByUserId = userId,
            Status = AppointmentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<Appointment>.Ok(appointment, "Appointment created"));
    }

    [HttpGet("my")]
    [Authorize(Policy = "UserOnly")]
    public async Task<ActionResult<ApiResponse<IEnumerable<Appointment>>>> My([FromQuery] AppointmentStatus? status = null)
    {
        var userId = GetUserId();
        var patient = await _db.Patients.FirstOrDefaultAsync(x => x.UserId == userId);
        if (patient is null) return Ok(ApiResponse<IEnumerable<Appointment>>.Ok(Array.Empty<Appointment>()));

        var query = _db.Appointments.Where(x => x.PatientId == patient.PatientId);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);

        var items = await query.OrderByDescending(x => x.AppointmentDate).ToListAsync();
        return Ok(ApiResponse<IEnumerable<Appointment>>.Ok(items));
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<IEnumerable<Appointment>>>> GetAll([FromQuery] AppointmentStatus? status = null)
    {
        var query = _db.Appointments.AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);

        var items = await query.OrderByDescending(x => x.AppointmentDate).ToListAsync();
        return Ok(ApiResponse<IEnumerable<Appointment>>.Ok(items));
    }

    [HttpPut("{appointmentId:long}/status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<Appointment>>> UpdateStatus(long appointmentId, [FromBody] AppointmentStatusUpdateDto dto)
    {
        var item = await _db.Appointments.FindAsync(appointmentId);
        if (item is null) return NotFound(ApiResponse<Appointment>.Fail("Appointment not found"));

        item.Status = dto.Status;
        item.AdminRemarks = dto.AdminRemarks;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ApiResponse<Appointment>.Ok(item, "Appointment status updated"));
    }

    private long GetUserId() => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
