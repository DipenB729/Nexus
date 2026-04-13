using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using AngularApp4.Services.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/doctors")]
public class DoctorsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _audit;

    public DoctorsController(AppDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<DoctorMasterDto>>>> GetAll([FromQuery] string? search = null, [FromQuery] bool? isActive = null)
    {
        var query = _db.Doctors
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Department)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.FullName.Contains(term) ||
                x.Specialization.Contains(term) ||
                (x.Department != null && x.Department.Name.Contains(term)) ||
                (x.Branch != null && x.Branch.Name.Contains(term)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        var doctors = await query
            .OrderBy(x => x.FullName)
            .Select(x => new DoctorMasterDto
            {
                DoctorId = x.DoctorId,
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                DepartmentId = x.DepartmentId,
                DepartmentName = x.Department != null ? x.Department.Name : string.Empty,
                FullName = x.FullName,
                Specialization = x.Specialization,
                Email = x.Email,
                Phone = x.Phone,
                ExperienceYears = x.ExperienceYears,
                Qualification = x.Qualification,
                OpdDays = x.OpdDays,
                OpdStartTime = x.OpdStartTime,
                OpdEndTime = x.OpdEndTime,
                ConsultationFee = x.ConsultationFee,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<DoctorMasterDto>>.Ok(doctors));
    }

    [HttpGet("{doctorId:long}")]
    public async Task<ActionResult<ApiResponse<DoctorMasterDto>>> GetById(long doctorId)
    {
        var doctor = await _db.Doctors
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Department)
            .Where(x => x.DoctorId == doctorId)
            .Select(x => new DoctorMasterDto
            {
                DoctorId = x.DoctorId,
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                DepartmentId = x.DepartmentId,
                DepartmentName = x.Department != null ? x.Department.Name : string.Empty,
                FullName = x.FullName,
                Specialization = x.Specialization,
                Email = x.Email,
                Phone = x.Phone,
                ExperienceYears = x.ExperienceYears,
                Qualification = x.Qualification,
                OpdDays = x.OpdDays,
                OpdStartTime = x.OpdStartTime,
                OpdEndTime = x.OpdEndTime,
                ConsultationFee = x.ConsultationFee,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync();

        return doctor is null
            ? NotFound(ApiResponse<DoctorMasterDto>.Fail("Doctor not found"))
            : Ok(ApiResponse<DoctorMasterDto>.Ok(doctor));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<DoctorMasterDto>>> Create([FromBody] SaveDoctorDto dto)
    {
        var validation = await ValidateMappingsAsync(dto.BranchId, dto.DepartmentId);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<DoctorMasterDto>.Fail(validation));
        }

        var doctor = new Doctor
        {
            BranchId = dto.BranchId,
            DepartmentId = dto.DepartmentId,
            FullName = dto.FullName.Trim(),
            Specialization = dto.Specialization.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            Phone = Normalize(dto.Phone),
            ExperienceYears = dto.ExperienceYears,
            Qualification = Normalize(dto.Qualification),
            OpdDays = Normalize(dto.OpdDays),
            OpdStartTime = dto.OpdStartTime,
            OpdEndTime = dto.OpdEndTime,
            ConsultationFee = dto.ConsultationFee,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync();
        await WriteAuditAsync("Created", doctor.DoctorId, doctor.FullName, $"Doctor {doctor.FullName} was created.");

        var payload = await GetDoctorDtoAsync(doctor.DoctorId);
        return Ok(ApiResponse<DoctorMasterDto>.Ok(payload, "Doctor created"));
    }

    [HttpPut("{doctorId:long}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<DoctorMasterDto>>> Update(long doctorId, [FromBody] SaveDoctorDto dto)
    {
        var doctor = await _db.Doctors.FirstOrDefaultAsync(x => x.DoctorId == doctorId);
        if (doctor is null)
        {
            return NotFound(ApiResponse<DoctorMasterDto>.Fail("Doctor not found"));
        }

        var validation = await ValidateMappingsAsync(dto.BranchId, dto.DepartmentId);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<DoctorMasterDto>.Fail(validation));
        }

        doctor.BranchId = dto.BranchId;
        doctor.DepartmentId = dto.DepartmentId;
        doctor.FullName = dto.FullName.Trim();
        doctor.Specialization = dto.Specialization.Trim();
        doctor.Email = dto.Email.Trim().ToLowerInvariant();
        doctor.Phone = Normalize(dto.Phone);
        doctor.ExperienceYears = dto.ExperienceYears;
        doctor.Qualification = Normalize(dto.Qualification);
        doctor.OpdDays = Normalize(dto.OpdDays);
        doctor.OpdStartTime = dto.OpdStartTime;
        doctor.OpdEndTime = dto.OpdEndTime;
        doctor.ConsultationFee = dto.ConsultationFee;
        doctor.IsActive = dto.IsActive;
        doctor.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await WriteAuditAsync("Updated", doctor.DoctorId, doctor.FullName, $"Doctor {doctor.FullName} was updated.");

        var payload = await GetDoctorDtoAsync(doctor.DoctorId);
        return Ok(ApiResponse<DoctorMasterDto>.Ok(payload, "Doctor updated"));
    }

    [HttpPut("{doctorId:long}/status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateStatus(long doctorId, [FromBody] StatusUpdateDto dto)
    {
        var doctor = await _db.Doctors.FirstOrDefaultAsync(x => x.DoctorId == doctorId);
        if (doctor is null)
        {
            return NotFound(ApiResponse<object>.Fail("Doctor not found"));
        }

        doctor.IsActive = dto.IsActive;
        doctor.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await WriteAuditAsync(dto.IsActive ? "Activated" : "Deactivated", doctor.DoctorId, doctor.FullName, $"Doctor {doctor.FullName} was {(dto.IsActive ? "activated" : "deactivated")}.");

        return Ok(ApiResponse<object>.Ok(null, dto.IsActive ? "Doctor activated" : "Doctor deactivated"));
    }

    [HttpDelete("{doctorId:long}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long doctorId)
    {
        var doctor = await _db.Doctors.FirstOrDefaultAsync(x => x.DoctorId == doctorId);
        if (doctor is null) return NotFound(ApiResponse<object>.Fail("Doctor not found"));

        doctor.IsActive = false;
        doctor.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await WriteAuditAsync("Deleted", doctor.DoctorId, doctor.FullName, $"Doctor {doctor.FullName} was deactivated from the master list.");
        return Ok(ApiResponse<object>.Ok(null, "Doctor deactivated"));
    }

    private Task WriteAuditAsync(string action, long entityId, string targetDisplayName, string summary)
    {
        return _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.MasterSetup,
            Action = action,
            EntityName = "Doctor",
            EntityId = entityId,
            TargetDisplayName = targetDisplayName,
            Summary = summary
        });
    }

    [HttpGet("{doctorId:long}/available-slots")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetAvailableSlots(long doctorId, [FromQuery] DateTime date)
    {
        var dayOfWeek = ((int)date.DayOfWeek + 6) % 7 + 1;
        var schedule = await _db.DoctorSchedules.FirstOrDefaultAsync(x => x.DoctorId == doctorId && x.DayOfWeek == dayOfWeek && x.IsActive);

        if (schedule is null)
        {
            var doctor = await _db.Doctors.AsNoTracking().FirstOrDefaultAsync(x => x.DoctorId == doctorId && x.IsActive);
            if (doctor is null)
            {
                return NotFound(ApiResponse<IEnumerable<object>>.Fail("Doctor not found"));
            }

            if (!IsDoctorAvailableOnDay(doctor.OpdDays, date.DayOfWeek) || !doctor.OpdStartTime.HasValue || !doctor.OpdEndTime.HasValue)
            {
                return Ok(ApiResponse<IEnumerable<object>>.Ok(Array.Empty<object>(), "No schedule"));
            }

            schedule = new DoctorSchedule
            {
                DoctorId = doctorId,
                DayOfWeek = (byte)dayOfWeek,
                StartTime = doctor.OpdStartTime.Value,
                EndTime = doctor.OpdEndTime.Value,
                SlotDurationMinutes = 30,
                MaxPatientsPerSlot = 1,
                IsActive = true
            };
        }

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

    private async Task<DoctorMasterDto> GetDoctorDtoAsync(long doctorId)
    {
        return await _db.Doctors
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Department)
            .Where(x => x.DoctorId == doctorId)
            .Select(x => new DoctorMasterDto
            {
                DoctorId = x.DoctorId,
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty,
                DepartmentId = x.DepartmentId,
                DepartmentName = x.Department != null ? x.Department.Name : string.Empty,
                FullName = x.FullName,
                Specialization = x.Specialization,
                Email = x.Email,
                Phone = x.Phone,
                ExperienceYears = x.ExperienceYears,
                Qualification = x.Qualification,
                OpdDays = x.OpdDays,
                OpdStartTime = x.OpdStartTime,
                OpdEndTime = x.OpdEndTime,
                ConsultationFee = x.ConsultationFee,
                IsActive = x.IsActive
            })
            .FirstAsync();
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsDoctorAvailableOnDay(string? opdDays, DayOfWeek dayOfWeek)
    {
        if (string.IsNullOrWhiteSpace(opdDays))
        {
            return false;
        }

        var normalized = opdDays.ToLowerInvariant();
        var token = dayOfWeek switch
        {
            DayOfWeek.Sunday => "sun",
            DayOfWeek.Monday => "mon",
            DayOfWeek.Tuesday => "tue",
            DayOfWeek.Wednesday => "wed",
            DayOfWeek.Thursday => "thu",
            DayOfWeek.Friday => "fri",
            _ => "sat"
        };

        return normalized.Contains(token);
    }
}
