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
[Route("api/admin/patients")]
[Authorize(Policy = "AdminOnly")]
public class AdminPatientsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordPolicyService _passwordPolicy;

    public AdminPatientsController(AppDbContext db, IPasswordPolicyService passwordPolicy)
    {
        _db = db;
        _passwordPolicy = passwordPolicy;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PatientOverviewDto>>> Create([FromBody] CreatePatientDto dto)
    {
        var nextEmail = dto.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(nextEmail))
        {
            return BadRequest(ApiResponse<PatientOverviewDto>.Fail("Full name and email are required"));
        }

        var policyValidation = await _passwordPolicy.ValidateAsync(dto.Password);
        if (!policyValidation.IsValid)
        {
            return BadRequest(ApiResponse<PatientOverviewDto>.Fail(policyValidation.Errors.First()));
        }

        if (await _db.Users.AnyAsync(x => x.Email == nextEmail))
        {
            return BadRequest(ApiResponse<PatientOverviewDto>.Fail("Email address is already in use"));
        }

        if (dto.PatientCategoryId.HasValue &&
            !await _db.PatientCategories.AnyAsync(x => x.PatientCategoryId == dto.PatientCategoryId.Value))
        {
            return BadRequest(ApiResponse<PatientOverviewDto>.Fail("Selected patient category was not found"));
        }

        var userRole = await _db.Roles.FirstOrDefaultAsync(x => x.Name == "User");
        if (userRole is null)
        {
            return BadRequest(ApiResponse<PatientOverviewDto>.Fail("User role is not configured"));
        }

        CreatePasswordHash(dto.Password, out var hash, out var salt);

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = nextEmail,
            Phone = Normalize(dto.Phone),
            RoleId = userRole.RoleId,
            PasswordHash = hash,
            PasswordSalt = salt,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var patient = new Patient
        {
            UserId = user.UserId,
            PatientCategoryId = dto.PatientCategoryId,
            Gender = Normalize(dto.Gender),
            DateOfBirth = dto.DateOfBirth?.Date,
            Address = Normalize(dto.Address),
            BloodGroup = Normalize(dto.BloodGroup),
            EmergencyContact = Normalize(dto.EmergencyContact),
            Notes = Normalize(dto.Notes),
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Patients.Add(patient);
        patient.MedicalRecordNumber = GenerateMedicalRecordNumber(patient.PatientId);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        var payload = (await BuildPatientListAsync(includeMerged: true)).First(x => x.PatientId == patient.PatientId);
        return Ok(ApiResponse<PatientOverviewDto>.Ok(payload, "Patient created"));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<PatientOverviewDto>>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] long? patientCategoryId = null,
        [FromQuery] bool includeMerged = false)
    {
        var rows = await BuildPatientListAsync(includeMerged);

        if (patientCategoryId.HasValue)
        {
            rows = rows.Where(x => x.PatientCategoryId == patientCategoryId.Value).ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            rows = rows.Where(x =>
                    x.FullName.ToLowerInvariant().Contains(term) ||
                    x.Email.ToLowerInvariant().Contains(term) ||
                    (x.Phone ?? string.Empty).ToLowerInvariant().Contains(term) ||
                    x.MedicalRecordNumber.ToLowerInvariant().Contains(term) ||
                    (x.PatientCategoryName ?? string.Empty).ToLowerInvariant().Contains(term) ||
                    (x.Address ?? string.Empty).ToLowerInvariant().Contains(term))
                .ToList();
        }

        return Ok(ApiResponse<IEnumerable<PatientOverviewDto>>.Ok(rows));
    }

    [HttpPut("{patientId:long}")]
    public async Task<ActionResult<ApiResponse<PatientOverviewDto>>> Update(long patientId, [FromBody] SavePatientDetailsDto dto)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(x => x.PatientId == patientId);
        if (patient is null)
        {
            return NotFound(ApiResponse<PatientOverviewDto>.Fail("Patient record not found"));
        }

        if (patient.MergedIntoPatientId.HasValue)
        {
            return BadRequest(ApiResponse<PatientOverviewDto>.Fail("Merged records cannot be edited"));
        }

        var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == patient.UserId);
        if (user is null)
        {
            return BadRequest(ApiResponse<PatientOverviewDto>.Fail("Linked user account not found"));
        }

        var nextEmail = dto.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(nextEmail))
        {
            return BadRequest(ApiResponse<PatientOverviewDto>.Fail("Full name and email are required"));
        }

        var emailTaken = await _db.Users.AnyAsync(x => x.UserId != user.UserId && x.Email == nextEmail);
        if (emailTaken)
        {
            return BadRequest(ApiResponse<PatientOverviewDto>.Fail("Email address is already in use"));
        }

        if (dto.PatientCategoryId.HasValue &&
            !await _db.PatientCategories.AnyAsync(x => x.PatientCategoryId == dto.PatientCategoryId.Value))
        {
            return BadRequest(ApiResponse<PatientOverviewDto>.Fail("Selected patient category was not found"));
        }

        user.FullName = dto.FullName.Trim();
        user.Email = nextEmail;
        user.Phone = Normalize(dto.Phone);
        user.IsActive = dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        patient.PatientCategoryId = dto.PatientCategoryId;
        patient.Gender = Normalize(dto.Gender);
        patient.DateOfBirth = dto.DateOfBirth?.Date;
        patient.Address = Normalize(dto.Address);
        patient.BloodGroup = Normalize(dto.BloodGroup);
        patient.EmergencyContact = Normalize(dto.EmergencyContact);
        patient.Notes = Normalize(dto.Notes);
        patient.IsActive = dto.IsActive;
        patient.UpdatedAt = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(patient.MedicalRecordNumber))
        {
            patient.MedicalRecordNumber = GenerateMedicalRecordNumber(patient.PatientId);
        }

        await _db.SaveChangesAsync();

        var payload = (await BuildPatientListAsync(includeMerged: true)).First(x => x.PatientId == patientId);
        return Ok(ApiResponse<PatientOverviewDto>.Ok(payload, "Patient details updated"));
    }

    [HttpPost("merge")]
    public async Task<ActionResult<ApiResponse<PatientOverviewDto>>> Merge([FromBody] MergePatientRecordsDto dto)
    {
        if (dto.SourcePatientId == dto.TargetPatientId)
        {
            return BadRequest(ApiResponse<PatientOverviewDto>.Fail("Source and target patients must be different"));
        }

        var source = await _db.Patients.FirstOrDefaultAsync(x => x.PatientId == dto.SourcePatientId);
        var target = await _db.Patients.FirstOrDefaultAsync(x => x.PatientId == dto.TargetPatientId);
        if (source is null || target is null)
        {
            return NotFound(ApiResponse<PatientOverviewDto>.Fail("Patient record not found"));
        }

        if (source.MergedIntoPatientId.HasValue)
        {
            return BadRequest(ApiResponse<PatientOverviewDto>.Fail("Source patient is already merged"));
        }

        var sourceUser = await _db.Users.FirstOrDefaultAsync(x => x.UserId == source.UserId);
        var targetUser = await _db.Users.FirstOrDefaultAsync(x => x.UserId == target.UserId);
        if (sourceUser is null || targetUser is null)
        {
            return BadRequest(ApiResponse<PatientOverviewDto>.Fail("Linked user account not found"));
        }

        await ReassignPatientReferencesAsync(source.PatientId, target.PatientId);

        target.PatientCategoryId ??= source.PatientCategoryId;
        target.Gender ??= source.Gender;
        target.DateOfBirth ??= source.DateOfBirth;
        target.Address ??= source.Address;
        target.BloodGroup ??= source.BloodGroup;
        target.EmergencyContact ??= source.EmergencyContact;
        target.Notes = CombineNotes(target.Notes, source.Notes, dto.Notes);
        target.IsActive = true;
        target.UpdatedAt = DateTime.UtcNow;

        targetUser.Phone = string.IsNullOrWhiteSpace(targetUser.Phone) ? sourceUser.Phone : targetUser.Phone;
        targetUser.UpdatedAt = DateTime.UtcNow;

        source.IsActive = false;
        source.MergedIntoPatientId = target.PatientId;
        source.Notes = CombineNotes(source.Notes, $"Merged into {target.MedicalRecordNumber ?? GenerateMedicalRecordNumber(target.PatientId)}", dto.Notes);
        source.UpdatedAt = DateTime.UtcNow;

        sourceUser.IsActive = false;
        sourceUser.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var payload = (await BuildPatientListAsync(includeMerged: true)).First(x => x.PatientId == target.PatientId);
        return Ok(ApiResponse<PatientOverviewDto>.Ok(payload, "Duplicate patient record merged"));
    }

    private async Task ReassignPatientReferencesAsync(long sourcePatientId, long targetPatientId)
    {
        var appointments = await _db.Appointments.Where(x => x.PatientId == sourcePatientId).ToListAsync();
        foreach (var appointment in appointments)
        {
            appointment.PatientId = targetPatientId;
            appointment.UpdatedAt = DateTime.UtcNow;
        }

        var invoices = await _db.BillingInvoices.Where(x => x.PatientId == sourcePatientId).ToListAsync();
        foreach (var invoice in invoices)
        {
            invoice.PatientId = targetPatientId;
            invoice.UpdatedAt = DateTime.UtcNow;
        }

        var admissions = await _db.PatientAdmissions.Where(x => x.PatientId == sourcePatientId).ToListAsync();
        foreach (var admission in admissions)
        {
            admission.PatientId = targetPatientId;
            admission.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task<List<PatientOverviewDto>> BuildPatientListAsync(bool includeMerged)
    {
        var patients = await _db.Patients
            .AsNoTracking()
            .ToListAsync();
        var patientCategories = await _db.PatientCategories
            .AsNoTracking()
            .ToDictionaryAsync(x => x.PatientCategoryId);

        var patientIds = patients.Select(x => x.PatientId).ToList();
        var userIds = patients.Select(x => x.UserId).Distinct().ToList();

        var users = await _db.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId);

        var appointmentRows = await _db.Appointments
            .AsNoTracking()
            .Where(x => patientIds.Contains(x.PatientId))
            .ToListAsync();
        var appointmentCounts = appointmentRows
            .GroupBy(x => x.PatientId)
            .ToDictionary(x => x.Key, x => x.Count());

        var activeAdmissionRows = await _db.PatientAdmissions
            .AsNoTracking()
            .Where(x => patientIds.Contains(x.PatientId) && x.Status != AdmissionStatus.Discharged)
            .ToListAsync();
        var activeAdmissionCounts = activeAdmissionRows
            .GroupBy(x => x.PatientId)
            .ToDictionary(x => x.Key, x => x.Count());

        var rows = patients
            .Where(x => includeMerged || !x.MergedIntoPatientId.HasValue)
            .Select(patient =>
            {
                users.TryGetValue(patient.UserId, out var user);
                return new PatientOverviewDto
                {
                    PatientId = patient.PatientId,
                    UserId = patient.UserId,
                    MedicalRecordNumber = patient.MedicalRecordNumber ?? GenerateMedicalRecordNumber(patient.PatientId),
                    FullName = user?.FullName ?? "Unknown patient",
                    Email = user?.Email ?? string.Empty,
                    Phone = user?.Phone,
                    PatientCategoryId = patient.PatientCategoryId,
                    PatientCategoryName = patient.PatientCategoryId.HasValue && patientCategories.TryGetValue(patient.PatientCategoryId.Value, out var category) ? category.Name : "Unassigned",
                    Gender = patient.Gender,
                    DateOfBirth = patient.DateOfBirth,
                    Address = patient.Address,
                    BloodGroup = patient.BloodGroup,
                    EmergencyContact = patient.EmergencyContact,
                    Notes = patient.Notes,
                    IsActive = patient.IsActive && (user?.IsActive ?? true),
                    MergedIntoPatientId = patient.MergedIntoPatientId,
                    IsMerged = patient.MergedIntoPatientId.HasValue,
                    AppointmentCount = appointmentCounts.GetValueOrDefault(patient.PatientId),
                    ActiveAdmissions = activeAdmissionCounts.GetValueOrDefault(patient.PatientId)
                };
            })
            .ToList();

        var duplicateMap = rows
            .GroupBy(BuildDuplicateKey)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
            .SelectMany(group => group.Select(item => new { item.PatientId, Count = group.Count() }))
            .ToDictionary(x => x.PatientId, x => x.Count);

        foreach (var row in rows)
        {
            row.DuplicateGroupSize = duplicateMap.GetValueOrDefault(row.PatientId, 1);
        }

        return rows
            .OrderByDescending(x => x.DuplicateGroupSize)
            .ThenByDescending(x => x.IsActive)
            .ThenBy(x => x.FullName)
            .ToList();
    }

    private static string BuildDuplicateKey(PatientOverviewDto patient)
    {
        var fullName = patient.FullName.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(fullName) && patient.DateOfBirth.HasValue)
        {
            return $"{fullName}|{patient.DateOfBirth.Value:yyyyMMdd}";
        }

        var contact = NormalizeDigits(patient.Phone) ?? NormalizeDigits(patient.EmergencyContact);
        return contact is null || !patient.DateOfBirth.HasValue
            ? string.Empty
            : $"{contact}|{patient.DateOfBirth.Value:yyyyMMdd}";
    }

    private static string GenerateMedicalRecordNumber(long patientId) => $"MRN-{patientId:D5}";

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
    {
        using var hmac = new HMACSHA512();
        salt = hmac.Key;
        hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }

    private static string? NormalizeDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length == 0 ? null : digits;
    }

    private static string? CombineNotes(params string?[] notes)
    {
        var lines = notes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return lines.Count == 0 ? null : string.Join(Environment.NewLine, lines);
    }
}
