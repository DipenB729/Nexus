using System.Security.Claims;
using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using AngularApp4.Services.Hms;
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
    private readonly IDoctorAvailabilityService _availability;
    private readonly IAppNotificationService _notifications;

    public AppointmentsController(AppDbContext db, IDoctorAvailabilityService availability, IAppNotificationService notifications)
    {
        _db = db;
        _availability = availability;
        _notifications = notifications;
    }

    [HttpPost]
    [Authorize(Policy = "UserOnly")]
    public async Task<ActionResult<ApiResponse<Appointment>>> Create([FromBody] AppointmentCreateDto dto)
    {
        var userId = GetUserId();
        var patient = await GetPatientAsync(userId);
        if (patient is null) return BadRequest(ApiResponse<Appointment>.Fail("Patient profile not found"));

        var availabilityError = await ValidateAppointmentSlotAsync(
            dto.DoctorId,
            dto.ScheduleId,
            dto.AppointmentDate.Date,
            dto.SlotStartTime,
            dto.SlotEndTime);
        if (availabilityError is not null)
        {
            return BadRequest(ApiResponse<Appointment>.Fail(availabilityError));
        }

        var schedule = await _availability.ResolveScheduleForSlotAsync(dto.DoctorId, dto.AppointmentDate.Date, dto.SlotStartTime, dto.SlotEndTime);
        if (schedule is null)
        {
            return BadRequest(ApiResponse<Appointment>.Fail("Doctor is unavailable for the selected date and time"));
        }

        var appointment = new Appointment
        {
            PatientId = patient.PatientId,
            DoctorId = dto.DoctorId,
            ScheduleId = schedule.ScheduleId,
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
        await _notifications.QueueAppointmentBookedAsync(appointment);
        return Ok(ApiResponse<Appointment>.Ok(appointment, "Appointment created"));
    }

    [HttpPut("{appointmentId:long}/cancel")]
    [Authorize(Policy = "UserOnly")]
    public async Task<ActionResult<ApiResponse<Appointment>>> Cancel(long appointmentId)
    {
        var appointment = await GetOwnedAppointmentAsync(appointmentId);
        if (appointment is null)
        {
            return NotFound(ApiResponse<Appointment>.Fail("Appointment not found"));
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            return BadRequest(ApiResponse<Appointment>.Fail("Appointment is already cancelled"));
        }

        if (appointment.Status == AppointmentStatus.Completed)
        {
            return BadRequest(ApiResponse<Appointment>.Fail("Completed appointments cannot be cancelled"));
        }

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _notifications.QueueAppointmentCancelledAsync(appointment);
        return Ok(ApiResponse<Appointment>.Ok(appointment, "Appointment cancelled"));
    }

    [HttpPut("{appointmentId:long}/reschedule")]
    [Authorize(Policy = "UserOnly")]
    public async Task<ActionResult<ApiResponse<Appointment>>> Reschedule(long appointmentId, [FromBody] AppointmentRescheduleDto dto)
    {
        var appointment = await GetOwnedAppointmentAsync(appointmentId);
        if (appointment is null)
        {
            return NotFound(ApiResponse<Appointment>.Fail("Appointment not found"));
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            return BadRequest(ApiResponse<Appointment>.Fail("Cancelled appointments cannot be rescheduled"));
        }

        if (appointment.Status == AppointmentStatus.Completed)
        {
            return BadRequest(ApiResponse<Appointment>.Fail("Completed appointments cannot be rescheduled"));
        }

        var availabilityError = await ValidateAppointmentSlotAsync(
            dto.DoctorId,
            dto.ScheduleId,
            dto.AppointmentDate.Date,
            dto.SlotStartTime,
            dto.SlotEndTime,
            appointmentId);
        if (availabilityError is not null)
        {
            return BadRequest(ApiResponse<Appointment>.Fail(availabilityError));
        }

        var schedule = await _availability.ResolveScheduleForSlotAsync(dto.DoctorId, dto.AppointmentDate.Date, dto.SlotStartTime, dto.SlotEndTime);

        appointment.DoctorId = dto.DoctorId;
        appointment.ScheduleId = schedule?.ScheduleId;
        appointment.AppointmentDate = dto.AppointmentDate.Date;
        appointment.SlotStartTime = dto.SlotStartTime;
        appointment.SlotEndTime = dto.SlotEndTime;
        appointment.Reason = dto.Reason;
        appointment.Status = AppointmentStatus.Rescheduled;
        appointment.TokenNumber = null;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _notifications.QueueAppointmentRescheduledAsync(appointment);
        return Ok(ApiResponse<Appointment>.Ok(appointment, "Appointment rescheduled"));
    }

    [HttpGet("my")]
    [Authorize(Policy = "UserOnly")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PatientAppointmentSummaryDto>>>> My([FromQuery] AppointmentStatus? status = null)
    {
        var userId = GetUserId();
        var patient = await _db.Patients.FirstOrDefaultAsync(x => x.UserId == userId);
        if (patient is null)
        {
            return Ok(ApiResponse<IEnumerable<PatientAppointmentSummaryDto>>.Ok(Array.Empty<PatientAppointmentSummaryDto>()));
        }

        var query = BuildPatientAppointmentQuery().Where(x => x.PatientId == patient.PatientId);
        if (status.HasValue)
        {
            var expectedStatus = status.Value.ToString();
            query = query.Where(x => x.Status == expectedStatus);
        }

        var items = await query
            .OrderByDescending(x => x.AppointmentDate)
            .ThenByDescending(x => x.SlotStartTime)
            .Select(x => new PatientAppointmentSummaryDto
            {
                AppointmentId = x.AppointmentId,
                AppointmentDate = x.AppointmentDate,
                SlotStartTime = x.SlotStartTime,
                SlotEndTime = x.SlotEndTime,
                Status = x.Status,
                TokenNumber = x.TokenNumber,
                Reason = x.Reason,
                AdminRemarks = x.AdminRemarks,
                DoctorId = x.DoctorId,
                DoctorName = x.DoctorName,
                DoctorSpecialization = x.DoctorSpecialization,
                ServiceId = x.ServiceId,
                ServiceName = x.ServiceName,
                ServicePrice = x.ServicePrice
            })
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<PatientAppointmentSummaryDto>>.Ok(items));
    }

    [HttpGet("doctor/my")]
    [Authorize(Policy = "DoctorOnly")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DoctorAppointmentSummaryDto>>>> DoctorMy([FromQuery] AppointmentStatus? status = null)
    {
        var userId = GetUserId();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive);
        if (user is null)
        {
            return Unauthorized(ApiResponse<IEnumerable<DoctorAppointmentSummaryDto>>.Fail("Doctor session not found"));
        }

        var normalizedEmail = user.Email.Trim().ToLowerInvariant();
        var doctor = await _db.Doctors.AsNoTracking().FirstOrDefaultAsync(x => x.Email == normalizedEmail && x.IsActive);
        if (doctor is null)
        {
            return Ok(ApiResponse<IEnumerable<DoctorAppointmentSummaryDto>>.Ok(Array.Empty<DoctorAppointmentSummaryDto>()));
        }

        var query = BuildDoctorAppointmentQuery().Where(x => x.DoctorId == doctor.DoctorId);
        if (status.HasValue)
        {
            var expectedStatus = status.Value.ToString();
            query = query.Where(x => x.Status == expectedStatus);
        }

        var items = await query
            .OrderBy(x => x.AppointmentDate)
            .ThenBy(x => x.SlotStartTime)
            .Select(x => new DoctorAppointmentSummaryDto
            {
                AppointmentId = x.AppointmentId,
                PatientId = x.PatientId,
                ScheduleId = x.ScheduleId,
                PatientName = x.PatientName,
                MedicalRecordNumber = x.MedicalRecordNumber,
                PatientGender = x.PatientGender,
                PatientDateOfBirth = x.PatientDateOfBirth,
                AppointmentDate = x.AppointmentDate,
                SlotStartTime = x.SlotStartTime,
                SlotEndTime = x.SlotEndTime,
                Status = x.Status,
                TokenNumber = x.TokenNumber,
                Reason = x.Reason,
                AdminRemarks = x.AdminRemarks,
                DoctorId = x.DoctorId,
                DoctorName = x.DoctorName,
                DoctorSpecialization = x.DoctorSpecialization,
                ServiceId = x.ServiceId,
                ServiceName = x.ServiceName,
                ServicePrice = x.ServicePrice
            })
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<DoctorAppointmentSummaryDto>>.Ok(items));
    }

    [HttpPut("doctor/{appointmentId:long}/status")]
    [Authorize(Policy = "DoctorOnly")]
    public async Task<ActionResult<ApiResponse<DoctorAppointmentSummaryDto>>> UpdateDoctorAppointmentStatus(long appointmentId, [FromBody] DoctorAppointmentStatusUpdateDto dto)
    {
        if (dto.Status is not (AppointmentStatus.Approved or AppointmentStatus.Cancelled or AppointmentStatus.Completed or AppointmentStatus.NoShow))
        {
            return BadRequest(ApiResponse<DoctorAppointmentSummaryDto>.Fail("Doctors can only approve, cancel, complete, or mark no-show"));
        }

        var userId = GetUserId();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive);
        if (user is null)
        {
            return Unauthorized(ApiResponse<DoctorAppointmentSummaryDto>.Fail("Doctor session not found"));
        }

        var normalizedEmail = user.Email.Trim().ToLowerInvariant();
        var doctor = await _db.Doctors.AsNoTracking().FirstOrDefaultAsync(x => x.Email == normalizedEmail && x.IsActive);
        if (doctor is null)
        {
            return NotFound(ApiResponse<DoctorAppointmentSummaryDto>.Fail("Doctor profile not found"));
        }

        var appointment = await _db.Appointments.FirstOrDefaultAsync(x => x.AppointmentId == appointmentId && x.DoctorId == doctor.DoctorId);
        if (appointment is null)
        {
            return NotFound(ApiResponse<DoctorAppointmentSummaryDto>.Fail("Appointment not found"));
        }

        if (appointment.Status == AppointmentStatus.Cancelled && dto.Status != AppointmentStatus.Cancelled)
        {
            return BadRequest(ApiResponse<DoctorAppointmentSummaryDto>.Fail("Cancelled appointments cannot be updated"));
        }

        if (appointment.Status == AppointmentStatus.Completed && dto.Status != AppointmentStatus.Completed)
        {
            return BadRequest(ApiResponse<DoctorAppointmentSummaryDto>.Fail("Completed appointments cannot be updated"));
        }

        var previousStatus = appointment.Status;
        appointment.Status = dto.Status;
        appointment.AdminRemarks = Normalize(dto.AdminRemarks) ?? appointment.AdminRemarks;
        if (dto.Status is AppointmentStatus.Approved or AppointmentStatus.Completed)
        {
            appointment.TokenNumber = await GenerateTokenNumberAsync(doctor.DoctorId, appointment.AppointmentDate, appointmentId);
        }

        appointment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (dto.Status == AppointmentStatus.Approved && previousStatus != AppointmentStatus.Approved)
        {
            await _notifications.QueueAppointmentApprovedAsync(appointment);
        }
        else if (dto.Status == AppointmentStatus.Cancelled && previousStatus != AppointmentStatus.Cancelled)
        {
            await _notifications.QueueAppointmentCancelledAsync(appointment);
        }

        var payload = await BuildDoctorAppointmentQuery()
            .Where(x => x.AppointmentId == appointmentId)
            .Select(x => new DoctorAppointmentSummaryDto
            {
                AppointmentId = x.AppointmentId,
                PatientId = x.PatientId,
                ScheduleId = x.ScheduleId,
                PatientName = x.PatientName,
                MedicalRecordNumber = x.MedicalRecordNumber,
                PatientGender = x.PatientGender,
                PatientDateOfBirth = x.PatientDateOfBirth,
                AppointmentDate = x.AppointmentDate,
                SlotStartTime = x.SlotStartTime,
                SlotEndTime = x.SlotEndTime,
                Status = x.Status,
                TokenNumber = x.TokenNumber,
                Reason = x.Reason,
                AdminRemarks = x.AdminRemarks,
                DoctorId = x.DoctorId,
                DoctorName = x.DoctorName,
                DoctorSpecialization = x.DoctorSpecialization,
                ServiceId = x.ServiceId,
                ServiceName = x.ServiceName,
                ServicePrice = x.ServicePrice
            })
            .FirstAsync();

        return Ok(ApiResponse<DoctorAppointmentSummaryDto>.Ok(payload, "Appointment status updated"));
    }

    [HttpPut("doctor/{appointmentId:long}/manage")]
    [Authorize(Policy = "DoctorOnly")]
    public async Task<ActionResult<ApiResponse<DoctorAppointmentSummaryDto>>> ManageDoctorAppointment(long appointmentId, [FromBody] DoctorManageAppointmentDto dto)
    {
        if (dto.Status is not (AppointmentStatus.Approved or AppointmentStatus.Rescheduled or AppointmentStatus.Cancelled or AppointmentStatus.Completed or AppointmentStatus.NoShow))
        {
            return BadRequest(ApiResponse<DoctorAppointmentSummaryDto>.Fail("Unsupported doctor appointment action"));
        }

        var userId = GetUserId();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive);
        if (user is null)
        {
            return Unauthorized(ApiResponse<DoctorAppointmentSummaryDto>.Fail("Doctor session not found"));
        }

        var normalizedEmail = user.Email.Trim().ToLowerInvariant();
        var doctor = await _db.Doctors.AsNoTracking().FirstOrDefaultAsync(x => x.Email == normalizedEmail && x.IsActive);
        if (doctor is null)
        {
            return NotFound(ApiResponse<DoctorAppointmentSummaryDto>.Fail("Doctor profile not found"));
        }

        var appointment = await _db.Appointments.FirstOrDefaultAsync(x => x.AppointmentId == appointmentId && x.DoctorId == doctor.DoctorId);
        if (appointment is null)
        {
            return NotFound(ApiResponse<DoctorAppointmentSummaryDto>.Fail("Appointment not found"));
        }

        if (appointment.Status == AppointmentStatus.Completed && dto.Status is not AppointmentStatus.Completed)
        {
            return BadRequest(ApiResponse<DoctorAppointmentSummaryDto>.Fail("Completed appointments cannot be changed"));
        }

        var nextDate = dto.AppointmentDate?.Date ?? appointment.AppointmentDate.Date;
        var nextStart = dto.SlotStartTime ?? appointment.SlotStartTime;
        var nextEnd = dto.SlotEndTime ?? appointment.SlotEndTime;
        var nextScheduleId = dto.ScheduleId ?? appointment.ScheduleId;
        var timingChanged = nextDate != appointment.AppointmentDate || nextStart != appointment.SlotStartTime || nextEnd != appointment.SlotEndTime;

        ResolvedDoctorSchedule? resolvedSchedule = null;
        if (dto.Status != AppointmentStatus.Cancelled)
        {
            if (timingChanged && nextEnd <= nextStart)
            {
                return BadRequest(ApiResponse<DoctorAppointmentSummaryDto>.Fail("End time must be after start time"));
            }

            resolvedSchedule = await _availability.ResolveScheduleForSlotAsync(doctor.DoctorId, nextDate, nextStart, nextEnd);
            if (resolvedSchedule is null)
            {
                return BadRequest(ApiResponse<DoctorAppointmentSummaryDto>.Fail("Doctor is unavailable for the selected date and time"));
            }

            if (nextScheduleId.HasValue && resolvedSchedule.ScheduleId != nextScheduleId.Value)
            {
                return BadRequest(ApiResponse<DoctorAppointmentSummaryDto>.Fail("Selected schedule does not match the requested time slot"));
            }

            var bookedPatients = await _availability.GetBookedPatientCountAsync(
                doctor.DoctorId,
                nextDate,
                nextStart,
                nextEnd,
                appointmentId);

            if (bookedPatients >= resolvedSchedule.MaxPatientsPerSlot)
            {
                return BadRequest(ApiResponse<DoctorAppointmentSummaryDto>.Fail("Selected slot has reached its booking limit"));
            }
        }

        var previousStatus = appointment.Status;

        appointment.ScheduleId = resolvedSchedule?.ScheduleId ?? nextScheduleId ?? appointment.ScheduleId;
        appointment.AppointmentDate = nextDate;
        appointment.SlotStartTime = nextStart;
        appointment.SlotEndTime = nextEnd;
        appointment.Reason = string.IsNullOrWhiteSpace(dto.Reason) ? appointment.Reason : dto.Reason.Trim();
        appointment.AdminRemarks = Normalize(dto.AdminRemarks);
        appointment.Status = dto.Status;
        appointment.UpdatedAt = DateTime.UtcNow;

        if (dto.Status is AppointmentStatus.Approved or AppointmentStatus.Rescheduled or AppointmentStatus.Completed)
        {
            appointment.TokenNumber = await GenerateTokenNumberAsync(doctor.DoctorId, nextDate, appointmentId);
        }

        await _db.SaveChangesAsync();

        if (dto.Status == AppointmentStatus.Cancelled && previousStatus != AppointmentStatus.Cancelled)
        {
            await _notifications.QueueAppointmentCancelledAsync(appointment);
        }
        else if (dto.Status == AppointmentStatus.Rescheduled || timingChanged)
        {
            await _notifications.QueueAppointmentRescheduledAsync(appointment);
        }
        else if (dto.Status == AppointmentStatus.Approved && previousStatus != AppointmentStatus.Approved)
        {
            await _notifications.QueueAppointmentApprovedAsync(appointment);
        }

        var payload = await BuildDoctorAppointmentQuery()
            .Where(x => x.AppointmentId == appointmentId)
            .Select(x => new DoctorAppointmentSummaryDto
            {
                AppointmentId = x.AppointmentId,
                PatientId = x.PatientId,
                PatientName = x.PatientName,
                MedicalRecordNumber = x.MedicalRecordNumber,
                PatientGender = x.PatientGender,
                PatientDateOfBirth = x.PatientDateOfBirth,
                AppointmentDate = x.AppointmentDate,
                SlotStartTime = x.SlotStartTime,
                SlotEndTime = x.SlotEndTime,
                Status = x.Status,
                TokenNumber = x.TokenNumber,
                Reason = x.Reason,
                AdminRemarks = x.AdminRemarks,
                DoctorId = x.DoctorId,
                DoctorName = x.DoctorName,
                DoctorSpecialization = x.DoctorSpecialization,
                ServiceId = x.ServiceId,
                ServiceName = x.ServiceName,
                ServicePrice = x.ServicePrice
            })
            .FirstAsync();

        return Ok(ApiResponse<DoctorAppointmentSummaryDto>.Ok(payload, "Appointment updated"));
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

    private Task<Patient?> GetPatientAsync(long userId)
    {
        return _db.Patients.FirstOrDefaultAsync(x => x.UserId == userId);
    }

    private async Task<Appointment?> GetOwnedAppointmentAsync(long appointmentId)
    {
        var userId = GetUserId();
        var patient = await GetPatientAsync(userId);
        if (patient is null)
        {
            return null;
        }

        return await _db.Appointments.FirstOrDefaultAsync(x => x.AppointmentId == appointmentId && x.PatientId == patient.PatientId);
    }

    private async Task<string?> ValidateAppointmentSlotAsync(
        long doctorId,
        long? scheduleId,
        DateTime appointmentDate,
        TimeSpan slotStartTime,
        TimeSpan slotEndTime,
        long? excludeAppointmentId = null)
    {
        if (slotEndTime <= slotStartTime)
        {
            return "End time must be after start time";
        }

        var doctorExists = await _db.Doctors.AnyAsync(x => x.DoctorId == doctorId && x.IsActive);
        if (!doctorExists)
        {
            return "Doctor not found/inactive";
        }

        var schedule = await _availability.ResolveScheduleForSlotAsync(doctorId, appointmentDate, slotStartTime, slotEndTime);
        if (schedule is null)
        {
            return "Doctor is unavailable for the selected date and time";
        }

        if (scheduleId.HasValue && schedule.ScheduleId != scheduleId.Value)
        {
            return "Selected schedule does not match the requested time slot";
        }

        var bookedPatients = await _availability.GetBookedPatientCountAsync(
            doctorId,
            appointmentDate,
            slotStartTime,
            slotEndTime,
            excludeAppointmentId);

        return bookedPatients >= schedule.MaxPatientsPerSlot
            ? "Selected slot has reached its booking limit"
            : null;
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

    private static int ExtractSequence(string tokenNumber)
    {
        var parts = tokenNumber.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 || !int.TryParse(parts[^1], out var value) ? 0 : value;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private IQueryable<PatientAppointmentProjection> BuildPatientAppointmentQuery()
    {
        return from appointment in _db.Appointments.AsNoTracking()
               join doctor in _db.Doctors.AsNoTracking() on appointment.DoctorId equals doctor.DoctorId
               join service in _db.Services.AsNoTracking() on appointment.ServiceId equals service.Id into serviceGroup
               from service in serviceGroup.DefaultIfEmpty()
               select new PatientAppointmentProjection
               {
                   AppointmentId = appointment.AppointmentId,
                   PatientId = appointment.PatientId,
                   AppointmentDate = appointment.AppointmentDate,
                   SlotStartTime = appointment.SlotStartTime,
                   SlotEndTime = appointment.SlotEndTime,
                   Status = appointment.Status.ToString(),
                   TokenNumber = appointment.TokenNumber,
                   Reason = appointment.Reason,
                   AdminRemarks = appointment.AdminRemarks,
                   DoctorId = doctor.DoctorId,
                   DoctorName = doctor.FullName,
                   DoctorSpecialization = doctor.Specialization,
                   ServiceId = appointment.ServiceId,
                   ServiceName = service != null ? service.Name : null,
                   ServicePrice = service != null ? service.Price : null
               };
    }

    private IQueryable<DoctorAppointmentProjection> BuildDoctorAppointmentQuery()
    {
        return from appointment in _db.Appointments.AsNoTracking()
               join patient in _db.Patients.AsNoTracking() on appointment.PatientId equals patient.PatientId
               join patientUser in _db.Users.AsNoTracking() on patient.UserId equals patientUser.UserId
               join doctor in _db.Doctors.AsNoTracking() on appointment.DoctorId equals doctor.DoctorId
               join service in _db.Services.AsNoTracking() on appointment.ServiceId equals service.Id into serviceGroup
               from service in serviceGroup.DefaultIfEmpty()
               select new DoctorAppointmentProjection
               {
                   AppointmentId = appointment.AppointmentId,
                   PatientId = appointment.PatientId,
                   ScheduleId = appointment.ScheduleId,
                   PatientName = patientUser.FullName,
                   MedicalRecordNumber = patient.MedicalRecordNumber ?? $"MRN-{appointment.PatientId:D5}",
                   PatientGender = patient.Gender,
                   PatientDateOfBirth = patient.DateOfBirth,
                   AppointmentDate = appointment.AppointmentDate,
                   SlotStartTime = appointment.SlotStartTime,
                   SlotEndTime = appointment.SlotEndTime,
                   Status = appointment.Status.ToString(),
                   TokenNumber = appointment.TokenNumber,
                   Reason = appointment.Reason,
                   AdminRemarks = appointment.AdminRemarks,
                   DoctorId = doctor.DoctorId,
                   DoctorName = doctor.FullName,
                   DoctorSpecialization = doctor.Specialization,
                   ServiceId = appointment.ServiceId,
                   ServiceName = service != null ? service.Name : null,
                   ServicePrice = service != null ? service.Price : null
               };
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

    private static int ExtractSequence(string tokenNumber)
    {
        var parts = tokenNumber.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 || !int.TryParse(parts[^1], out var value) ? 0 : value;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class PatientAppointmentProjection
    {
        public long AppointmentId { get; init; }
        public long PatientId { get; init; }
        public DateTime AppointmentDate { get; init; }
        public TimeSpan SlotStartTime { get; init; }
        public TimeSpan SlotEndTime { get; init; }
        public string Status { get; init; } = string.Empty;
        public string? TokenNumber { get; init; }
        public string? Reason { get; init; }
        public string? AdminRemarks { get; init; }
        public long DoctorId { get; init; }
        public string DoctorName { get; init; } = string.Empty;
        public string? DoctorSpecialization { get; init; }
        public long? ServiceId { get; init; }
        public string? ServiceName { get; init; }
        public decimal? ServicePrice { get; init; }
    }

    private sealed class DoctorAppointmentProjection
    {
        public long AppointmentId { get; init; }
        public long PatientId { get; init; }
        public long? ScheduleId { get; init; }
        public string PatientName { get; init; } = string.Empty;
        public string MedicalRecordNumber { get; init; } = string.Empty;
        public string? PatientGender { get; init; }
        public DateTime? PatientDateOfBirth { get; init; }
        public DateTime AppointmentDate { get; init; }
        public TimeSpan SlotStartTime { get; init; }
        public TimeSpan SlotEndTime { get; init; }
        public string Status { get; init; } = string.Empty;
        public string? TokenNumber { get; init; }
        public string? Reason { get; init; }
        public string? AdminRemarks { get; init; }
        public long DoctorId { get; init; }
        public string DoctorName { get; init; } = string.Empty;
        public string? DoctorSpecialization { get; init; }
        public long? ServiceId { get; init; }
        public string? ServiceName { get; init; }
        public decimal? ServicePrice { get; init; }
    }
}
