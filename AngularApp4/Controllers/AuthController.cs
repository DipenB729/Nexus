using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using AngularApp4.Services.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly IAuditLogService _audit;
    private readonly IPasswordPolicyService _passwordPolicy;
    private readonly IPasswordResetService _passwordReset;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        AppDbContext db,
        IJwtTokenService jwt,
        IAuditLogService audit,
        IPasswordPolicyService passwordPolicy,
        IPasswordResetService passwordReset,
        IWebHostEnvironment environment)
    {
        _db = db;
        _jwt = jwt;
        _audit = audit;
        _passwordPolicy = passwordPolicy;
        _passwordReset = passwordReset;
        _environment = environment;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register(RegisterRequestDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("Full name and email are required"));
        }

        if (await _db.Users.AnyAsync(x => x.Email == email))
        {
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("Email already exists"));
        }

        var policyValidation = await _passwordPolicy.ValidateAsync(dto.Password);
        if (!policyValidation.IsValid)
        {
            return BadRequest(ApiResponse<AuthResponseDto>.Fail(policyValidation.Errors.First()));
        }

        const string roleName = "User";
        var role = await _db.Roles.FirstAsync(x => x.Name == roleName);

        CreatePasswordHash(dto.Password, out var hash, out var salt);

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            Phone = Normalize(dto.Phone),
            RoleId = role.RoleId,
            PasswordHash = hash,
            PasswordSalt = salt,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        if (roleName == "User")
        {
            _db.Patients.Add(new Patient
            {
                UserId = user.UserId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        await _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.Authentication,
            Action = "Registered",
            EntityName = "User",
            EntityId = user.UserId,
            TargetDisplayName = user.FullName,
            Summary = $"User {user.FullName} registered a new portal account.",
            Metadata = new { user.Email, Role = roleName },
            PerformedByUserId = user.UserId,
            PerformedByName = user.FullName,
            PerformedByRole = roleName,
            ActorEmail = user.Email
        });

        var token = await _jwt.GenerateTokenAsync(user, roleName);
        return Ok(ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Token = token,
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = roleName
        }, "Registration successful"));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(LoginRequestDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Email == email && x.IsActive);
        if (user is null || !VerifyPassword(dto.Password, user.PasswordHash, user.PasswordSalt))
        {
            await _audit.WriteAsync(new AuditLogRequest
            {
                Category = AuditLogCategories.Authentication,
                Action = "LoginFailed",
                EntityName = "UserSession",
                TargetDisplayName = email,
                Summary = $"Failed login attempt for {email}.",
                ActorEmail = email
            });
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail("Invalid credentials"));
        }

        var role = user.Role?.Name ?? "User";
        await _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.Authentication,
            Action = "LoginSucceeded",
            EntityName = "UserSession",
            EntityId = user.UserId,
            TargetDisplayName = user.FullName,
            Summary = $"{user.FullName} signed in successfully.",
            Metadata = new { user.Email, Role = role },
            PerformedByUserId = user.UserId,
            PerformedByName = user.FullName,
            PerformedByRole = role,
            ActorEmail = user.Email
        });

        var token = await _jwt.GenerateTokenAsync(user, role);
        return Ok(ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Token = token,
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = role
        }, "Login successful"));
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<ForgotPasswordResponseDto>>> ForgotPassword(ForgotPasswordRequestDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var payload = new ForgotPasswordResponseDto();

        if (!string.IsNullOrWhiteSpace(email))
        {
            var user = await _db.Users
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Email == email && x.IsActive);

            if (user?.Role?.Name is "User" or "Doctor")
            {
                var ticket = _passwordReset.CreateTicket(email);
                if (_environment.IsDevelopment())
                {
                    payload.ResetCodePreview = ticket.Code;
                    payload.ExpiresAt = ticket.ExpiresAtUtc;
                }

                await _audit.WriteAsync(new AuditLogRequest
                {
                    Category = AuditLogCategories.Authentication,
                    Action = "PasswordResetRequested",
                    EntityName = "UserSession",
                    EntityId = user.UserId,
                    TargetDisplayName = user.FullName,
                    Summary = $"{user.FullName} requested a password reset code.",
                    PerformedByUserId = user.UserId,
                    PerformedByName = user.FullName,
                    PerformedByRole = user.Role.Name,
                    ActorEmail = user.Email
                });
            }
        }

        return Ok(ApiResponse<ForgotPasswordResponseDto>.Ok(payload, "If the account exists, reset instructions are ready."));
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(ResetPasswordRequestDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(dto.ResetCode))
        {
            return BadRequest(ApiResponse<object>.Fail("Email and reset code are required"));
        }

        var user = await _db.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email == email && x.IsActive);

        if (user?.Role?.Name is not ("User" or "Doctor") || !_passwordReset.TryConsume(email, dto.ResetCode))
        {
            return BadRequest(ApiResponse<object>.Fail("Invalid or expired reset code"));
        }

        var policyValidation = await _passwordPolicy.ValidateAsync(dto.NewPassword);
        if (!policyValidation.IsValid)
        {
            return BadRequest(ApiResponse<object>.Fail(policyValidation.Errors.First()));
        }

        CreatePasswordHash(dto.NewPassword, out var hash, out var salt);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.Authentication,
            Action = "PasswordResetCompleted",
            EntityName = "User",
            EntityId = user.UserId,
            TargetDisplayName = user.FullName,
            Summary = $"{user.FullName} completed a password reset.",
            PerformedByUserId = user.UserId,
            PerformedByName = user.FullName,
            PerformedByRole = user.Role.Name,
            ActorEmail = user.Email
        });

        return Ok(ApiResponse<object>.Ok(null, "Password reset successful"));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(ChangePasswordRequestDto dto)
    {
        var userId = GetUserId();
        var user = await _db.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive);
        if (user is null)
        {
            return NotFound(ApiResponse<object>.Fail("User account not found"));
        }

        if (!VerifyPassword(dto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
        {
            return BadRequest(ApiResponse<object>.Fail("Current password is incorrect"));
        }

        var policyValidation = await _passwordPolicy.ValidateAsync(dto.NewPassword);
        if (!policyValidation.IsValid)
        {
            return BadRequest(ApiResponse<object>.Fail(policyValidation.Errors.First()));
        }

        CreatePasswordHash(dto.NewPassword, out var hash, out var salt);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.Authentication,
            Action = "PasswordChanged",
            EntityName = "User",
            EntityId = user.UserId,
            TargetDisplayName = user.FullName,
            Summary = $"{user.FullName} changed their password.",
            PerformedByUserId = user.UserId,
            PerformedByName = user.FullName,
            PerformedByRole = user.Role?.Name,
            ActorEmail = user.Email
        });

        return Ok(ApiResponse<object>.Ok(null, "Password changed successfully"));
    }

    [HttpGet("profile")]
    [Authorize(Policy = "UserOnly")]
    public async Task<ActionResult<ApiResponse<PatientProfileDto>>> GetProfile()
    {
        var profile = await BuildProfileAsync(GetUserId());
        return profile is null
            ? NotFound(ApiResponse<PatientProfileDto>.Fail("Patient profile not found"))
            : Ok(ApiResponse<PatientProfileDto>.Ok(profile));
    }

    [HttpPut("profile")]
    [Authorize(Policy = "UserOnly")]
    public async Task<ActionResult<ApiResponse<PatientProfileDto>>> UpdateProfile(UpdatePatientProfileDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
        {
            return BadRequest(ApiResponse<PatientProfileDto>.Fail("Full name is required"));
        }

        var userId = GetUserId();
        var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive);
        var patient = await _db.Patients.FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive);
        if (user is null || patient is null)
        {
            return NotFound(ApiResponse<PatientProfileDto>.Fail("Patient profile not found"));
        }

        user.FullName = dto.FullName.Trim();
        user.Phone = Normalize(dto.Phone);
        user.UpdatedAt = DateTime.UtcNow;

        patient.Gender = Normalize(dto.Gender);
        patient.DateOfBirth = dto.DateOfBirth?.Date;
        patient.Address = Normalize(dto.Address);
        patient.BloodGroup = Normalize(dto.BloodGroup);
        patient.EmergencyContact = Normalize(dto.EmergencyContact);
        patient.MedicalRecordNumber ??= GenerateMedicalRecordNumber(patient.PatientId);
        patient.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.Authentication,
            Action = "ProfileUpdated",
            EntityName = "PatientProfile",
            EntityId = patient.PatientId,
            TargetDisplayName = user.FullName,
            Summary = $"{user.FullName} updated their patient portal profile.",
            PerformedByUserId = user.UserId,
            PerformedByName = user.FullName,
            PerformedByRole = "User",
            ActorEmail = user.Email
        });

        var profile = await BuildProfileAsync(userId);
        return Ok(ApiResponse<PatientProfileDto>.Ok(profile!, "Profile updated"));
    }

    [HttpGet("doctor-profile")]
    [Authorize(Policy = "DoctorOnly")]
    public async Task<ActionResult<ApiResponse<DoctorProfileDto>>> GetDoctorProfile()
    {
        var profile = await BuildDoctorProfileAsync(GetUserId());
        return profile is null
            ? NotFound(ApiResponse<DoctorProfileDto>.Fail("Doctor profile not found"))
            : Ok(ApiResponse<DoctorProfileDto>.Ok(profile));
    }

    [HttpPut("doctor-profile")]
    [Authorize(Policy = "DoctorOnly")]
    public async Task<ActionResult<ApiResponse<DoctorProfileDto>>> UpdateDoctorProfile(UpdateDoctorProfileDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Email))
        {
            return BadRequest(ApiResponse<DoctorProfileDto>.Fail("Full name and email are required"));
        }

        var userId = GetUserId();
        var user = await _db.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive);
        if (user is null)
        {
            return NotFound(ApiResponse<DoctorProfileDto>.Fail("Doctor account not found"));
        }

        var doctor = await _db.Doctors.FirstOrDefaultAsync(x => x.Email == user.Email && x.IsActive);
        if (doctor is null)
        {
            return NotFound(ApiResponse<DoctorProfileDto>.Fail("Doctor profile not found"));
        }

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var emailInUse = await _db.Users.AnyAsync(x => x.UserId != userId && x.Email == normalizedEmail);
        if (emailInUse)
        {
            return BadRequest(ApiResponse<DoctorProfileDto>.Fail("Email is already used by another account"));
        }

        if (dto.DepartmentId.HasValue && !await _db.Departments.AnyAsync(x => x.DepartmentId == dto.DepartmentId.Value && x.IsActive))
        {
            return BadRequest(ApiResponse<DoctorProfileDto>.Fail("Selected department was not found"));
        }

        user.FullName = dto.FullName.Trim();
        user.Email = normalizedEmail;
        user.Phone = Normalize(dto.Phone);
        user.UpdatedAt = DateTime.UtcNow;

        doctor.FullName = dto.FullName.Trim();
        doctor.Email = normalizedEmail;
        doctor.Phone = Normalize(dto.Phone);
        doctor.DepartmentId = dto.DepartmentId;
        doctor.Specialization = string.IsNullOrWhiteSpace(dto.Specialization) ? doctor.Specialization : dto.Specialization.Trim();
        doctor.ExperienceYears = Math.Max(dto.ExperienceYears, 0);
        doctor.Qualification = Normalize(dto.Qualification);
        doctor.LicenseNumber = Normalize(dto.LicenseNumber);
        doctor.ConsultationFee = Math.Max(dto.ConsultationFee, 0);
        doctor.Bio = Normalize(dto.Bio);
        doctor.Address = Normalize(dto.Address);
        doctor.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.Authentication,
            Action = "DoctorProfileUpdated",
            EntityName = "DoctorProfile",
            EntityId = doctor.DoctorId,
            TargetDisplayName = doctor.FullName,
            Summary = $"{doctor.FullName} updated their doctor portal profile.",
            PerformedByUserId = user.UserId,
            PerformedByName = user.FullName,
            PerformedByRole = user.Role?.Name,
            ActorEmail = user.Email
        });

        var profile = await BuildDoctorProfileAsync(userId);
        return Ok(ApiResponse<DoctorProfileDto>.Ok(profile!, "Doctor profile updated"));
    }

    [HttpGet("doctor-profile/options")]
    [Authorize(Policy = "DoctorOnly")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DoctorProfileDepartmentOptionDto>>>> GetDoctorProfileOptions()
    {
        var departments = await _db.Departments
            .AsNoTracking()
            .Include(x => x.Branch)
            .Where(x => x.IsActive)
            .OrderBy(x => x.Branch!.Name)
            .ThenBy(x => x.Name)
            .Select(x => new DoctorProfileDepartmentOptionDto
            {
                DepartmentId = x.DepartmentId,
                Name = x.Name,
                BranchName = x.Branch != null ? x.Branch.Name : string.Empty
            })
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<DoctorProfileDepartmentOptionDto>>.Ok(departments));
    }

    [HttpPost("doctor-profile/photo")]
    [Authorize(Policy = "DoctorOnly")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<ApiResponse<DoctorProfilePhotoDto>>> UploadDoctorPhoto(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<DoctorProfilePhotoDto>.Fail("Select a photo to upload"));
        }

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(extension))
        {
            return BadRequest(ApiResponse<DoctorProfilePhotoDto>.Fail("Only JPG, PNG, or WebP images are supported"));
        }

        var userId = GetUserId();
        var user = await _db.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive);
        if (user is null)
        {
            return NotFound(ApiResponse<DoctorProfilePhotoDto>.Fail("Doctor account not found"));
        }

        var doctor = await _db.Doctors.FirstOrDefaultAsync(x => x.Email == user.Email && x.IsActive);
        if (doctor is null)
        {
            return NotFound(ApiResponse<DoctorProfilePhotoDto>.Fail("Doctor profile not found"));
        }

        var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", "doctor-profiles");
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"doctor-{doctor.DoctorId}-{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = System.IO.File.Create(absolutePath))
        {
            await file.CopyToAsync(stream);
        }

        var previousPhotoPath = doctor.PhotoUrl;
        doctor.PhotoUrl = $"/uploads/doctor-profiles/{fileName}";
        doctor.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        DeleteDoctorPhotoFile(previousPhotoPath);

        await _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.Authentication,
            Action = "DoctorPhotoUpdated",
            EntityName = "DoctorProfile",
            EntityId = doctor.DoctorId,
            TargetDisplayName = doctor.FullName,
            Summary = $"{doctor.FullName} updated their profile photo.",
            PerformedByUserId = user.UserId,
            PerformedByName = user.FullName,
            PerformedByRole = user.Role?.Name,
            ActorEmail = user.Email
        });

        return Ok(ApiResponse<DoctorProfilePhotoDto>.Ok(new DoctorProfilePhotoDto
        {
            PhotoUrl = doctor.PhotoUrl
        }, "Doctor photo uploaded"));
    }

    private static void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
    {
        using var hmac = new HMACSHA512();
        salt = hmac.Key;
        hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }

    private static bool VerifyPassword(string password, byte[] storedHash, byte[] storedSalt)
    {
        using var hmac = new HMACSHA512(storedSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return computedHash.SequenceEqual(storedHash);
    }

    private async Task<PatientProfileDto?> BuildProfileAsync(long userId)
    {
        return await _db.Patients
            .AsNoTracking()
            .Include(x => x.PatientCategory)
            .Where(x => x.UserId == userId)
            .Join(
                _db.Users.AsNoTracking(),
                patient => patient.UserId,
                user => user.UserId,
                (patient, user) => new PatientProfileDto
                {
                    PatientId = patient.PatientId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    MedicalRecordNumber = patient.MedicalRecordNumber ?? GenerateMedicalRecordNumber(patient.PatientId),
                    PatientCategoryName = patient.PatientCategory != null ? patient.PatientCategory.Name : "Unassigned",
                    Gender = patient.Gender,
                    DateOfBirth = patient.DateOfBirth,
                    Address = patient.Address,
                    BloodGroup = patient.BloodGroup,
                    EmergencyContact = patient.EmergencyContact
                })
            .FirstOrDefaultAsync();
    }

    private async Task<DoctorProfileDto?> BuildDoctorProfileAsync(long userId)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive);
        if (user is null)
        {
            return null;
        }

        var normalizedEmail = user.Email.Trim().ToLowerInvariant();

        return await _db.Doctors
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Department)
            .Where(x => x.Email == normalizedEmail && x.IsActive)
            .Select(x => new DoctorProfileDto
            {
                DoctorId = x.DoctorId,
                DepartmentId = x.DepartmentId,
                FullName = x.FullName,
                PhotoUrl = x.PhotoUrl,
                Email = x.Email,
                Phone = x.Phone,
                Specialization = x.Specialization,
                LicenseNumber = x.LicenseNumber,
                ExperienceYears = x.ExperienceYears,
                Qualification = x.Qualification,
                ConsultationFee = x.ConsultationFee,
                BranchName = x.Branch != null ? x.Branch.Name : null,
                DepartmentName = x.Department != null ? x.Department.Name : null,
                Bio = x.Bio,
                Address = x.Address,
                OpdDays = x.OpdDays,
                OpdStartTime = x.OpdStartTime,
                OpdEndTime = x.OpdEndTime
            })
            .FirstOrDefaultAsync();
    }

    private long GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return long.Parse(raw!);
    }

    private static string GenerateMedicalRecordNumber(long patientId) => $"MRN-{patientId:D5}";

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void DeleteDoctorPhotoFile(string? currentPhotoUrl)
    {
        if (string.IsNullOrWhiteSpace(currentPhotoUrl) || !currentPhotoUrl.StartsWith("/uploads/doctor-profiles/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var relativePath = currentPhotoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var absolutePath = Path.Combine(_environment.WebRootPath, relativePath);
        if (System.IO.File.Exists(absolutePath))
        {
            System.IO.File.Delete(absolutePath);
        }
    }
}
