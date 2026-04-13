using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/admin/appointments")]
[Authorize(Policy = "AdminOnly")]
public class AdminAppointmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminAppointmentsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<AppointmentAdminDto>>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] AppointmentStatus? status = null,
        [FromQuery] long? doctorId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var rows = await BuildAppointmentListAsync();

        if (status.HasValue)
        {
            rows = rows.Where(x => string.Equals(x.Status, status.Value.ToString(), StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (doctorId.HasValue)
        {
            rows = rows.Where(x => x.DoctorId == doctorId.Value).ToList();
        }

        if (fromDate.HasValue)
        {
            rows = rows.Where(x => x.AppointmentDate.Date >= fromDate.Value.Date).ToList();
        }

        if (toDate.HasValue)
        {
            rows = rows.Where(x => x.AppointmentDate.Date <= toDate.Value.Date).ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            rows = rows.Where(x =>
                    x.PatientName.ToLowerInvariant().Contains(term) ||
                    x.DoctorName.ToLowerInvariant().Contains(term) ||
                    x.ServiceName.ToLowerInvariant().Contains(term) ||
                    x.MedicalRecordNumber.ToLowerInvariant().Contains(term) ||
                    (x.TokenNumber ?? string.Empty).ToLowerInvariant().Contains(term) ||
                    x.Status.ToLowerInvariant().Contains(term))
                .ToList();
        }

        return Ok(ApiResponse<IEnumerable<AppointmentAdminDto>>.Ok(rows));
    }

    [HttpPut("{appointmentId:long}/manage")]
    public async Task<ActionResult<ApiResponse<AppointmentAdminDto>>> Manage(long appointmentId, [FromBody] ManageAppointmentDto dto)
    {
        var appointment = await _db.Appointments.FirstOrDefaultAsync(x => x.AppointmentId == appointmentId);
        if (appointment is null)
        {
            return NotFound(ApiResponse<AppointmentAdminDto>.Fail("Appointment not found"));
        }

        if (!Enum.TryParse<AppointmentStatus>(dto.Status, true, out var nextStatus))
        {
            return BadRequest(ApiResponse<AppointmentAdminDto>.Fail("Invalid appointment status"));
        }

        var nextDoctorId = dto.DoctorId ?? appointment.DoctorId;
        var nextDate = dto.AppointmentDate?.Date ?? appointment.AppointmentDate.Date;
        var nextStart = dto.SlotStartTime ?? appointment.SlotStartTime;
        var nextEnd = dto.SlotEndTime ?? appointment.SlotEndTime;
        var nextScheduleId = dto.ScheduleId ?? appointment.ScheduleId;

        if (nextEnd <= nextStart)
        {
            return BadRequest(ApiResponse<AppointmentAdminDto>.Fail("End time must be after start time"));
        }

        var doctor = await _db.Doctors.FirstOrDefaultAsync(x => x.DoctorId == nextDoctorId && x.IsActive);
        if (doctor is null)
        {
            return BadRequest(ApiResponse<AppointmentAdminDto>.Fail("Selected doctor is unavailable"));
        }

        if (nextScheduleId.HasValue)
        {
            var scheduleExists = await _db.DoctorSchedules.AnyAsync(x =>
                x.ScheduleId == nextScheduleId.Value &&
                x.DoctorId == nextDoctorId &&
                x.IsActive);

            if (!scheduleExists)
            {
                return BadRequest(ApiResponse<AppointmentAdminDto>.Fail("Selected schedule is not valid for this doctor"));
            }
        }

        var slotTaken = await _db.Appointments.AnyAsync(x =>
            x.AppointmentId != appointmentId &&
            x.DoctorId == nextDoctorId &&
            x.AppointmentDate.Date == nextDate &&
            x.SlotStartTime == nextStart &&
            x.Status != AppointmentStatus.Cancelled);

        if (slotTaken)
        {
            return BadRequest(ApiResponse<AppointmentAdminDto>.Fail("The selected slot is already booked"));
        }

        appointment.DoctorId = nextDoctorId;
        appointment.ScheduleId = nextScheduleId;
        appointment.AppointmentDate = nextDate;
        appointment.SlotStartTime = nextStart;
        appointment.SlotEndTime = nextEnd;
        appointment.Status = nextStatus;
        appointment.AdminRemarks = Normalize(dto.AdminRemarks);
        appointment.UpdatedAt = DateTime.UtcNow;

        if (nextStatus == AppointmentStatus.Approved ||
            nextStatus == AppointmentStatus.Rescheduled ||
            nextStatus == AppointmentStatus.Completed)
        {
            appointment.TokenNumber = await GenerateTokenNumberAsync(nextDoctorId, nextDate, appointmentId);
        }

        await _db.SaveChangesAsync();

        var payload = (await BuildAppointmentListAsync()).First(x => x.AppointmentId == appointmentId);
        return Ok(ApiResponse<AppointmentAdminDto>.Ok(payload, "Appointment updated"));
    }

    [HttpGet("token-settings")]
    public async Task<ActionResult<ApiResponse<AppointmentTokenSettingsDto>>> GetTokenSettings()
    {
        var settings = await GetOrCreateTokenSettingsAsync();
        return Ok(ApiResponse<AppointmentTokenSettingsDto>.Ok(MapTokenSettings(settings)));
    }

    [HttpPut("token-settings")]
    public async Task<ActionResult<ApiResponse<AppointmentTokenSettingsDto>>> UpdateTokenSettings([FromBody] AppointmentTokenSettingsDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Prefix))
        {
            return BadRequest(ApiResponse<AppointmentTokenSettingsDto>.Fail("Token prefix is required"));
        }

        if (dto.StartingNumber < 1 || dto.NumberPadding < 2 || dto.NumberPadding > 6)
        {
            return BadRequest(ApiResponse<AppointmentTokenSettingsDto>.Fail("Provide a valid token start number and padding"));
        }

        var settings = await GetOrCreateTokenSettingsAsync();
        settings.Prefix = dto.Prefix.Trim().ToUpperInvariant();
        settings.StartingNumber = dto.StartingNumber;
        settings.NumberPadding = dto.NumberPadding;
        settings.ResetDaily = dto.ResetDaily;
        settings.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<AppointmentTokenSettingsDto>.Ok(MapTokenSettings(settings), "Token settings updated"));
    }

    private async Task<List<AppointmentAdminDto>> BuildAppointmentListAsync()
    {
        var appointments = await _db.Appointments
            .AsNoTracking()
            .OrderByDescending(x => x.AppointmentDate)
            .ThenByDescending(x => x.SlotStartTime)
            .ToListAsync();

        var patientIds = appointments.Select(x => x.PatientId).Distinct().ToList();
        var doctorIds = appointments.Select(x => x.DoctorId).Distinct().ToList();
        var serviceIds = appointments.Where(x => x.ServiceId.HasValue).Select(x => x.ServiceId!.Value).Distinct().ToList();

        var patients = await _db.Patients
            .AsNoTracking()
            .Where(x => patientIds.Contains(x.PatientId))
            .ToDictionaryAsync(x => x.PatientId);

        var patientUserIds = patients.Values.Select(x => x.UserId).Distinct().ToList();
        var users = await _db.Users
            .AsNoTracking()
            .Where(x => patientUserIds.Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId);

        var doctors = await _db.Doctors
            .AsNoTracking()
            .Where(x => doctorIds.Contains(x.DoctorId))
            .ToListAsync();

        var branchIds = doctors.Where(x => x.BranchId.HasValue).Select(x => x.BranchId!.Value).Distinct().ToList();
        var departmentIds = doctors.Where(x => x.DepartmentId.HasValue).Select(x => x.DepartmentId!.Value).Distinct().ToList();

        var branches = await _db.Branches
            .AsNoTracking()
            .Where(x => branchIds.Contains(x.BranchId))
            .ToDictionaryAsync(x => x.BranchId);

        var departments = await _db.Departments
            .AsNoTracking()
            .Where(x => departmentIds.Contains(x.DepartmentId))
            .ToDictionaryAsync(x => x.DepartmentId);

        var services = await _db.Services
            .AsNoTracking()
            .Where(x => serviceIds.Contains(x.Id))
            .ToDictionaryAsync(x => (long)x.Id, x => x.Name);

        return appointments.Select(appointment =>
        {
            patients.TryGetValue(appointment.PatientId, out var patient);
            users.TryGetValue(patient?.UserId ?? 0, out var user);
            var doctor = doctors.FirstOrDefault(x => x.DoctorId == appointment.DoctorId);

            return new AppointmentAdminDto
            {
                AppointmentId = appointment.AppointmentId,
                PatientId = appointment.PatientId,
                PatientName = user?.FullName ?? "Unknown patient",
                MedicalRecordNumber = patient?.MedicalRecordNumber ?? $"MRN-{appointment.PatientId:D5}",
                DoctorId = appointment.DoctorId,
                DoctorName = doctor?.FullName ?? "Unknown doctor",
                DepartmentName = doctor?.DepartmentId is long departmentId && departments.TryGetValue(departmentId, out var department)
                    ? department.Name
                    : string.Empty,
                BranchName = doctor?.BranchId is long branchId && branches.TryGetValue(branchId, out var branch)
                    ? branch.Name
                    : string.Empty,
                ScheduleId = appointment.ScheduleId,
                ServiceId = appointment.ServiceId,
                ServiceName = appointment.ServiceId.HasValue && services.TryGetValue(appointment.ServiceId.Value, out var serviceName)
                    ? serviceName
                    : "Walk-in consultation",
                AppointmentDate = appointment.AppointmentDate,
                SlotStartTime = appointment.SlotStartTime,
                SlotEndTime = appointment.SlotEndTime,
                Status = appointment.Status.ToString(),
                TokenNumber = appointment.TokenNumber,
                Reason = appointment.Reason,
                AdminRemarks = appointment.AdminRemarks
            };
        }).ToList();
    }

    private async Task<AppointmentTokenSetting> GetOrCreateTokenSettingsAsync()
    {
        var settings = await _db.AppointmentTokenSettings.OrderBy(x => x.AppointmentTokenSettingId).FirstOrDefaultAsync();
        if (settings is not null)
        {
            return settings;
        }

        settings = new AppointmentTokenSetting
        {
            Prefix = "OPD",
            StartingNumber = 1,
            NumberPadding = 3,
            ResetDaily = true,
            UpdatedAt = DateTime.UtcNow
        };

        _db.AppointmentTokenSettings.Add(settings);
        await _db.SaveChangesAsync();
        return settings;
    }

    private async Task<string> GenerateTokenNumberAsync(long doctorId, DateTime appointmentDate, long appointmentId)
    {
        var settings = await GetOrCreateTokenSettingsAsync();

        var query = _db.Appointments
            .AsNoTracking()
            .Where(x =>
                x.AppointmentId != appointmentId &&
                x.DoctorId == doctorId &&
                x.Status != AppointmentStatus.Cancelled &&
                x.TokenNumber != null);

        if (settings.ResetDaily)
        {
            query = query.Where(x => x.AppointmentDate.Date == appointmentDate.Date);
        }

        var existingTokens = await query.Select(x => x.TokenNumber!).ToListAsync();
        var maxNumber = existingTokens
            .Select(ExtractSequence)
            .DefaultIfEmpty(settings.StartingNumber - 1)
            .Max();

        var nextNumber = Math.Max(settings.StartingNumber, maxNumber + 1);
        return $"{settings.Prefix}-{appointmentDate:yyyyMMdd}-{nextNumber.ToString($"D{settings.NumberPadding}")}";
    }

    private static int ExtractSequence(string tokenNumber)
    {
        var parts = tokenNumber.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 || !int.TryParse(parts[^1], out var value) ? 0 : value;
    }

    private static AppointmentTokenSettingsDto MapTokenSettings(AppointmentTokenSetting settings)
    {
        return new AppointmentTokenSettingsDto
        {
            AppointmentTokenSettingId = settings.AppointmentTokenSettingId,
            Prefix = settings.Prefix,
            StartingNumber = settings.StartingNumber,
            NumberPadding = settings.NumberPadding,
            ResetDaily = settings.ResetDaily
        };
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
