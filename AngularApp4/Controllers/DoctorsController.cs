using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using AngularApp4.Services.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/doctors")]
public class DoctorsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _audit;
    private readonly IDoctorAvailabilityService _availability;
    private readonly IDoctorPortalEmailService _doctorPortalEmail;

    public DoctorsController(
        AppDbContext db,
        IAuditLogService audit,
        IDoctorAvailabilityService availability,
        IDoctorPortalEmailService doctorPortalEmail)
    {
        _db = db;
        _audit = audit;
        _availability = availability;
        _doctorPortalEmail = doctorPortalEmail;
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

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Doctors.AnyAsync(x => x.Email == normalizedEmail))
        {
            return BadRequest(ApiResponse<DoctorMasterDto>.Fail("Doctor email already exists"));
        }

        var portalValidation = await ValidatePortalEmailAvailabilityAsync(normalizedEmail, null);
        if (portalValidation is not null)
        {
            return BadRequest(ApiResponse<DoctorMasterDto>.Fail(portalValidation));
        }

        var doctor = new Doctor
        {
            BranchId = dto.BranchId,
            DepartmentId = dto.DepartmentId,
            FullName = dto.FullName.Trim(),
            Specialization = dto.Specialization.Trim(),
            Email = normalizedEmail,
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

        DoctorPortalProvisioningResult provisioning;
        try
        {
            provisioning = await EnsureDoctorPortalAccountAsync(doctor, null, sendCredentials: true);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<DoctorMasterDto>.Fail(ex.Message));
        }
        await WriteAuditAsync("Created", doctor.DoctorId, doctor.FullName, $"Doctor {doctor.FullName} was created.");

        var payload = await GetDoctorDtoAsync(doctor.DoctorId);
        ApplyProvisioning(payload, provisioning);
        return Ok(ApiResponse<DoctorMasterDto>.Ok(payload, provisioning.Message));
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

        var previousEmail = doctor.Email;
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Doctors.AnyAsync(x => x.DoctorId != doctorId && x.Email == normalizedEmail))
        {
            return BadRequest(ApiResponse<DoctorMasterDto>.Fail("Doctor email already exists"));
        }

        var portalValidation = await ValidatePortalEmailAvailabilityAsync(normalizedEmail, previousEmail);
        if (portalValidation is not null)
        {
            return BadRequest(ApiResponse<DoctorMasterDto>.Fail(portalValidation));
        }

        doctor.BranchId = dto.BranchId;
        doctor.DepartmentId = dto.DepartmentId;
        doctor.FullName = dto.FullName.Trim();
        doctor.Specialization = dto.Specialization.Trim();
        doctor.Email = normalizedEmail;
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

        var shouldSendCredentials = !string.Equals(previousEmail, normalizedEmail, StringComparison.OrdinalIgnoreCase);
        DoctorPortalProvisioningResult provisioning;
        try
        {
            provisioning = await EnsureDoctorPortalAccountAsync(doctor, previousEmail, shouldSendCredentials);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<DoctorMasterDto>.Fail(ex.Message));
        }
        await WriteAuditAsync("Updated", doctor.DoctorId, doctor.FullName, $"Doctor {doctor.FullName} was updated.");

        var payload = await GetDoctorDtoAsync(doctor.DoctorId);
        ApplyProvisioning(payload, provisioning);
        return Ok(ApiResponse<DoctorMasterDto>.Ok(payload, provisioning.Message));
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

        var linkedUser = await FindDoctorPortalUserAsync(doctor.Email);
        if (linkedUser is not null)
        {
            linkedUser.IsActive = dto.IsActive;
            linkedUser.UpdatedAt = DateTime.UtcNow;
        }

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

        var linkedUser = await FindDoctorPortalUserAsync(doctor.Email);
        if (linkedUser is not null)
        {
            linkedUser.IsActive = false;
            linkedUser.UpdatedAt = DateTime.UtcNow;
        }

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
    public async Task<ActionResult<ApiResponse<IEnumerable<DoctorAvailableSlotDto>>>> GetAvailableSlots(long doctorId, [FromQuery] DateTime date)
    {
        var doctorExists = await _db.Doctors.AsNoTracking().AnyAsync(x => x.DoctorId == doctorId && x.IsActive);
        if (!doctorExists)
        {
            return NotFound(ApiResponse<IEnumerable<DoctorAvailableSlotDto>>.Fail("Doctor not found"));
        }

        var slots = await _availability.GetAvailableSlotsAsync(doctorId, date.Date);
        if (slots.Count == 0)
        {
            return Ok(ApiResponse<IEnumerable<DoctorAvailableSlotDto>>.Ok(Array.Empty<DoctorAvailableSlotDto>(), "No schedule"));
        }

        return Ok(ApiResponse<IEnumerable<DoctorAvailableSlotDto>>.Ok(slots));
    }

    private async Task<string?> ValidateMappingsAsync(long? branchId, long? departmentId)
    {
        if (branchId.HasValue && branchId <= 0)
        {
            return "Branch not found";
        }

        if (departmentId.HasValue && departmentId <= 0)
        {
            return "Department not found";
        }

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

    private async Task<string?> ValidatePortalEmailAvailabilityAsync(string normalizedEmail, string? previousEmail)
    {
        var user = await _db.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail);

        if (user is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(previousEmail) &&
            string.Equals(previousEmail.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return user.Role?.Name == "Doctor" ? null : "Email is already used by another portal account.";
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

    private async Task<DoctorPortalProvisioningResult> EnsureDoctorPortalAccountAsync(Doctor doctor, string? previousEmail, bool sendCredentials)
    {
        var normalizedEmail = doctor.Email.Trim().ToLowerInvariant();
        var doctorRole = await _db.Roles.FirstAsync(x => x.Name == "Doctor");
        var existingUser = await FindDoctorPortalUserAsync(previousEmail ?? normalizedEmail);

        if (existingUser is null)
        {
            existingUser = await _db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail);
            if (existingUser is not null && existingUser.RoleId != doctorRole.RoleId)
            {
                throw new InvalidOperationException("Email is already used by another portal account.");
            }
        }

        var accountCreated = false;
        string? temporaryPassword = null;

        if (existingUser is null)
        {
            temporaryPassword = GenerateTemporaryPassword();
            CreatePasswordHash(temporaryPassword, out var hash, out var salt);

            existingUser = new User
            {
                RoleId = doctorRole.RoleId,
                FullName = doctor.FullName,
                Email = normalizedEmail,
                Phone = doctor.Phone,
                PasswordHash = hash,
                PasswordSalt = salt,
                IsActive = doctor.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(existingUser);
            await _db.SaveChangesAsync();
            accountCreated = true;
        }
        else
        {
            existingUser.RoleId = doctorRole.RoleId;
            existingUser.FullName = doctor.FullName;
            existingUser.Email = normalizedEmail;
            existingUser.Phone = doctor.Phone;
            existingUser.IsActive = doctor.IsActive;
            existingUser.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        var messageParts = new List<string>
        {
            accountCreated ? "Doctor created and portal account provisioned." : "Doctor updated and portal account synced."
        };

        var emailSent = false;
        if (sendCredentials && !string.IsNullOrWhiteSpace(temporaryPassword))
        {
            var portalUrl = await GetDoctorPortalUrlAsync();
            var emailResult = await _doctorPortalEmail.SendCredentialsAsync(
                normalizedEmail,
                doctor.FullName,
                normalizedEmail,
                temporaryPassword,
                portalUrl,
                HttpContext.RequestAborted);

            emailSent = emailResult.Sent;
            messageParts.Add(emailResult.Message);
            if (!emailResult.Sent)
            {
                messageParts.Add($"Temporary password: {temporaryPassword}");
            }
        }
        else if (accountCreated && !sendCredentials && !string.IsNullOrWhiteSpace(temporaryPassword))
        {
            messageParts.Add($"Temporary password: {temporaryPassword}");
        }
        else if (!accountCreated && sendCredentials)
        {
            messageParts.Add("Existing doctor portal account kept its current password.");
        }

        return new DoctorPortalProvisioningResult
        {
            AccountCreated = accountCreated,
            EmailSent = sendCredentials ? emailSent : null,
            Message = string.Join(" ", messageParts.Where(x => !string.IsNullOrWhiteSpace(x)))
        };
    }

    private async Task<string> GetDoctorPortalUrlAsync()
    {
        var configuredUrl = await _db.SystemControlSettings
            .AsNoTracking()
            .OrderBy(x => x.SystemControlSettingId)
            .Select(x => x.DoctorPortalBaseUrl)
            .FirstOrDefaultAsync();

        if (!string.IsNullOrWhiteSpace(configuredUrl))
        {
            return configuredUrl.Trim();
        }

        return $"{Request.Scheme}://{Request.Host}/auth/login";
    }

    private async Task<User?> FindDoctorPortalUserAsync(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await _db.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail && x.Role != null && x.Role.Name == "Doctor");
    }

    private static void ApplyProvisioning(DoctorMasterDto dto, DoctorPortalProvisioningResult provisioning)
    {
        dto.PortalAccountCreated = provisioning.AccountCreated;
        dto.PortalEmailSent = provisioning.EmailSent;
        dto.PortalProvisioningNote = provisioning.Message;
    }

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
        var buffer = new byte[12];
        RandomNumberGenerator.Fill(buffer);
        var password = new char[12];
        for (var index = 0; index < password.Length; index++)
        {
            password[index] = chars[buffer[index] % chars.Length];
        }

        return new string(password);
    }

    private static void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
    {
        using var hmac = new HMACSHA512();
        salt = hmac.Key;
        hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class DoctorPortalProvisioningResult
    {
        public bool AccountCreated { get; init; }
        public bool? EmailSent { get; init; }
        public string Message { get; init; } = string.Empty;
    }
}
