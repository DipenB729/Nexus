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
[Route("api/superadmin")]
[Authorize(Policy = "SuperAdminOnly")]
public sealed class SuperAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _audit;
    private readonly IPasswordPolicyService _passwordPolicy;
    private readonly IPasswordResetService _passwordReset;
    private readonly IDoctorPortalEmailService _email;
    private readonly IWebHostEnvironment _environment;

    public SuperAdminController(
        AppDbContext db,
        IAuditLogService audit,
        IPasswordPolicyService passwordPolicy,
        IPasswordResetService passwordReset,
        IDoctorPortalEmailService email,
        IWebHostEnvironment environment)
    {
        _db = db;
        _audit = audit;
        _passwordPolicy = passwordPolicy;
        _passwordReset = passwordReset;
        _email = email;
        _environment = environment;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<SuperAdminSummaryDto>>> GetSummary(CancellationToken cancellationToken)
    {
        var adminRoleId = await _db.Roles
            .Where(x => x.Name == "Admin")
            .Select(x => (long?)x.RoleId)
            .FirstOrDefaultAsync(cancellationToken);

        var summary = new SuperAdminSummaryDto
        {
            TotalHospitals = await _db.HospitalProfiles.CountAsync(cancellationToken),
            ActiveBranches = await _db.Branches.CountAsync(x => x.IsActive, cancellationToken),
            TotalAdmins = adminRoleId.HasValue ? await _db.Users.CountAsync(x => x.RoleId == adminRoleId.Value, cancellationToken) : 0,
            ActiveAdmins = adminRoleId.HasValue ? await _db.Users.CountAsync(x => x.RoleId == adminRoleId.Value && x.IsActive, cancellationToken) : 0,
            TotalUsers = await _db.Users.CountAsync(cancellationToken),
            LastUpdatedAt = DateTime.UtcNow
        };

        return Ok(ApiResponse<SuperAdminSummaryDto>.Ok(summary));
    }

    [HttpGet("hospital")]
    public async Task<ActionResult<ApiResponse<SuperAdminHospitalDto>>> GetHospital(CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<SuperAdminHospitalDto>.Ok(new SuperAdminHospitalDto
        {
            Profile = await BuildHospitalProfileDtoAsync(cancellationToken),
            Branches = await BuildBranchDtos().ToListAsync(cancellationToken)
        }));
    }

    [HttpGet("hospitals")]
    public async Task<ActionResult<ApiResponse<IEnumerable<HospitalProfileDto>>>> GetHospitals(CancellationToken cancellationToken)
    {
        var hospitals = await _db.HospitalProfiles
            .AsNoTracking()
            .OrderBy(x => x.HospitalName)
            .Select(x => new HospitalProfileDto
            {
                HospitalProfileId = x.HospitalProfileId,
                HospitalName = x.HospitalName,
                LogoUrl = x.LogoUrl,
                AddressLine1 = x.AddressLine1,
                City = x.City,
                StateOrProvince = x.StateOrProvince,
                PostalCode = x.PostalCode,
                Country = x.Country,
                ContactEmail = x.ContactEmail,
                ContactPhone = x.ContactPhone,
                TaxLabel = x.TaxLabel,
                TaxRegistrationNumber = x.TaxRegistrationNumber,
                TaxPercentage = x.TaxPercentage,
                CurrencyCode = x.CurrencyCode,
                InvoicePrefix = x.InvoicePrefix,
                InvoiceStartingNumber = x.InvoiceStartingNumber,
                InvoiceFooterNote = x.InvoiceFooterNote,
                MultiBranchEnabled = x.MultiBranchEnabled
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IEnumerable<HospitalProfileDto>>.Ok(hospitals));
    }

    [HttpPut("hospital")]
    public async Task<ActionResult<ApiResponse<HospitalProfileDto>>> UpdateHospital(HospitalProfileDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.HospitalName))
        {
            return BadRequest(ApiResponse<HospitalProfileDto>.Fail("Hospital name is required"));
        }

        var profile = await _db.HospitalProfiles.OrderBy(x => x.HospitalProfileId).FirstOrDefaultAsync(cancellationToken);
        if (profile is null)
        {
            profile = new HospitalProfile();
            _db.HospitalProfiles.Add(profile);
        }

        profile.HospitalName = NormalizeRequired(dto.HospitalName);
        profile.LogoUrl = Normalize(dto.LogoUrl);
        profile.AddressLine1 = NormalizeRequired(dto.AddressLine1);
        profile.City = NormalizeRequired(dto.City);
        profile.StateOrProvince = NormalizeRequired(dto.StateOrProvince);
        profile.PostalCode = NormalizeRequired(dto.PostalCode);
        profile.Country = NormalizeRequired(dto.Country);
        profile.ContactEmail = NormalizeRequired(dto.ContactEmail);
        profile.ContactPhone = NormalizeRequired(dto.ContactPhone);
        profile.TaxLabel = NormalizeRequired(dto.TaxLabel);
        profile.TaxRegistrationNumber = NormalizeRequired(dto.TaxRegistrationNumber);
        profile.TaxPercentage = Math.Max(dto.TaxPercentage, 0);
        profile.CurrencyCode = NormalizeRequired(dto.CurrencyCode);
        profile.InvoicePrefix = NormalizeRequired(dto.InvoicePrefix);
        profile.InvoiceStartingNumber = Math.Max(dto.InvoiceStartingNumber, 1);
        profile.InvoiceFooterNote = NormalizeRequired(dto.InvoiceFooterNote);
        profile.MultiBranchEnabled = dto.MultiBranchEnabled;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync("HospitalUpdated", "HospitalProfile", profile.HospitalProfileId, profile.HospitalName, "Superadmin updated hospital profile.");

        return Ok(ApiResponse<HospitalProfileDto>.Ok(await BuildHospitalProfileDtoAsync(cancellationToken), "Hospital profile updated"));
    }

    [HttpPost("branches")]
    public async Task<ActionResult<ApiResponse<BranchDto>>> CreateBranch(UpsertBranchDto dto, CancellationToken cancellationToken)
    {
        var validation = await ValidateBranchAsync(dto, null, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<BranchDto>.Fail(validation));
        }

        var branch = new Branch { CreatedAt = DateTime.UtcNow };
        ApplyBranch(branch, dto);
        _db.Branches.Add(branch);
        await _db.SaveChangesAsync(cancellationToken);
        await EnforceSinglePrimaryBranchAsync(branch, cancellationToken);
        await WriteAuditAsync("BranchCreated", "Branch", branch.BranchId, branch.Name, $"Superadmin created branch {branch.Name}.");

        return Ok(ApiResponse<BranchDto>.Ok(MapBranch(branch), "Branch created"));
    }

    [HttpPut("branches/{branchId:long}")]
    public async Task<ActionResult<ApiResponse<BranchDto>>> UpdateBranch(long branchId, UpsertBranchDto dto, CancellationToken cancellationToken)
    {
        var branch = await _db.Branches.FirstOrDefaultAsync(x => x.BranchId == branchId, cancellationToken);
        if (branch is null)
        {
            return NotFound(ApiResponse<BranchDto>.Fail("Branch not found"));
        }

        var validation = await ValidateBranchAsync(dto, branchId, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<BranchDto>.Fail(validation));
        }

        ApplyBranch(branch, dto);
        await _db.SaveChangesAsync(cancellationToken);
        await EnforceSinglePrimaryBranchAsync(branch, cancellationToken);
        await WriteAuditAsync("BranchUpdated", "Branch", branch.BranchId, branch.Name, $"Superadmin updated branch {branch.Name}.");

        return Ok(ApiResponse<BranchDto>.Ok(MapBranch(branch), "Branch updated"));
    }

    [HttpGet("admins")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SuperAdminUserDto>>>> GetAdmins(CancellationToken cancellationToken)
    {
        var adminRoleId = await _db.Roles
            .Where(x => x.Name == "Admin")
            .Select(x => (long?)x.RoleId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!adminRoleId.HasValue)
        {
            return Ok(ApiResponse<IEnumerable<SuperAdminUserDto>>.Ok(Array.Empty<SuperAdminUserDto>()));
        }

        var adminUsers = await _db.Users
            .AsNoTracking()
            .Where(x => x.RoleId == adminRoleId.Value)
            .ToListAsync(cancellationToken);
        var hospitals = await _db.HospitalProfiles
            .AsNoTracking()
            .ToDictionaryAsync(x => x.HospitalProfileId, x => x.HospitalName, cancellationToken);
        var admins = adminUsers
            .Select(x => MapAdmin(x, "Admin", hospitals.GetValueOrDefault(x.HospitalProfileId ?? 0, "Unassigned")))
            .OrderBy(x => x.HospitalName)
            .ThenBy(x => x.FullName);

        return Ok(ApiResponse<IEnumerable<SuperAdminUserDto>>.Ok(admins));
    }

    [HttpPost("admins")]
    public async Task<ActionResult<ApiResponse<SuperAdminUserDto>>> CreateAdmin(CreateAdminUserDto dto, CancellationToken cancellationToken)
    {
        var email = NormalizeRequired(dto.Email).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(ApiResponse<SuperAdminUserDto>.Fail("Full name and email are required"));
        }

        var hospital = await _db.HospitalProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.HospitalProfileId == dto.HospitalProfileId, cancellationToken);
        if (hospital is null)
        {
            return BadRequest(ApiResponse<SuperAdminUserDto>.Fail("Select a valid hospital for this admin account"));
        }

        if (await _db.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            return BadRequest(ApiResponse<SuperAdminUserDto>.Fail("Email already exists"));
        }

        var policyValidation = await _passwordPolicy.ValidateAsync(dto.Password);
        if (!policyValidation.IsValid)
        {
            return BadRequest(ApiResponse<SuperAdminUserDto>.Fail(policyValidation.Errors.First()));
        }

        var role = await _db.Roles.FirstAsync(x => x.Name == "Admin", cancellationToken);
        CreatePasswordHash(dto.Password, out var hash, out var salt);

        var user = new User
        {
            RoleId = role.RoleId,
            HospitalProfileId = hospital.HospitalProfileId,
            FullName = NormalizeRequired(dto.FullName),
            Email = email,
            Phone = Normalize(dto.Phone),
            PasswordHash = hash,
            PasswordSalt = salt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync("AdminCreated", "User", user.UserId, user.FullName, $"Superadmin created admin account {user.Email} for {hospital.HospitalName}.");

        return Ok(ApiResponse<SuperAdminUserDto>.Ok(await BuildAdminDtoAsync(user.UserId, cancellationToken), "Admin created"));
    }

    [HttpPut("admins/{userId:long}")]
    public async Task<ActionResult<ApiResponse<SuperAdminUserDto>>> UpdateAdmin(long userId, UpdateAdminUserDto dto, CancellationToken cancellationToken)
    {
        var email = NormalizeRequired(dto.Email).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(ApiResponse<SuperAdminUserDto>.Fail("Full name and email are required"));
        }

        var adminRoleId = await _db.Roles
            .Where(x => x.Name == "Admin")
            .Select(x => (long?)x.RoleId)
            .FirstOrDefaultAsync(cancellationToken);
        var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (user is null || !adminRoleId.HasValue || user.RoleId != adminRoleId.Value)
        {
            return NotFound(ApiResponse<SuperAdminUserDto>.Fail("Admin account not found"));
        }

        var hospital = await _db.HospitalProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.HospitalProfileId == dto.HospitalProfileId, cancellationToken);
        if (hospital is null)
        {
            return BadRequest(ApiResponse<SuperAdminUserDto>.Fail("Select a valid hospital for this admin account"));
        }

        if (await _db.Users.AnyAsync(x => x.Email == email && x.UserId != userId, cancellationToken))
        {
            return BadRequest(ApiResponse<SuperAdminUserDto>.Fail("Email already exists"));
        }

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var policyValidation = await _passwordPolicy.ValidateAsync(dto.Password);
            if (!policyValidation.IsValid)
            {
                return BadRequest(ApiResponse<SuperAdminUserDto>.Fail(policyValidation.Errors.First()));
            }

            CreatePasswordHash(dto.Password, out var hash, out var salt);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
        }

        user.HospitalProfileId = hospital.HospitalProfileId;
        user.FullName = NormalizeRequired(dto.FullName);
        user.Email = email;
        user.Phone = Normalize(dto.Phone);
        user.IsActive = dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync("AdminUpdated", "User", user.UserId, user.FullName, $"Superadmin updated admin account {user.Email} for {hospital.HospitalName}.");

        return Ok(ApiResponse<SuperAdminUserDto>.Ok(await BuildAdminDtoAsync(user.UserId, cancellationToken), "Admin updated"));
    }

    [HttpPut("admins/{userId:long}/status")]
    public async Task<ActionResult<ApiResponse<SuperAdminUserDto>>> UpdateAdminStatus(long userId, UpdateAdminStatusDto dto, CancellationToken cancellationToken)
    {
        var user = await _db.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        var adminRoleId = await _db.Roles
            .Where(x => x.Name == "Admin")
            .Select(x => (long?)x.RoleId)
            .FirstOrDefaultAsync(cancellationToken);
        if (user is null || !adminRoleId.HasValue || user.RoleId != adminRoleId.Value)
        {
            return NotFound(ApiResponse<SuperAdminUserDto>.Fail("Admin account not found"));
        }

        user.IsActive = dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync("AdminStatusUpdated", "User", user.UserId, user.FullName, $"Superadmin {(dto.IsActive ? "activated" : "deactivated")} admin account {user.Email}.");

        return Ok(ApiResponse<SuperAdminUserDto>.Ok(await BuildAdminDtoAsync(user.UserId, cancellationToken), "Admin status updated"));
    }

    [HttpDelete("admins/{userId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAdmin(long userId, CancellationToken cancellationToken)
    {
        var adminRoleId = await _db.Roles
            .Where(x => x.Name == "Admin")
            .Select(x => (long?)x.RoleId)
            .FirstOrDefaultAsync(cancellationToken);
        var user = await _db.Users.Include(x => x.HospitalProfile).FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (user is null || !adminRoleId.HasValue || user.RoleId != adminRoleId.Value)
        {
            return NotFound(ApiResponse<object>.Fail("Admin account not found"));
        }

        var displayName = user.FullName;
        var email = user.Email;
        var hospitalName = user.HospitalProfile?.HospitalName ?? "Unassigned";
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync("AdminDeleted", "User", userId, displayName, $"Superadmin deleted admin account {email} for {hospitalName}.");

        return Ok(ApiResponse<object>.Ok(new { userId }, "Admin deleted"));
    }

    [HttpPost("admins/{userId:long}/password-reset")]
    public async Task<ActionResult<ApiResponse<AdminPasswordResetEmailDto>>> SendAdminPasswordReset(long userId, CancellationToken cancellationToken)
    {
        var adminRoleId = await _db.Roles
            .Where(x => x.Name == "Admin")
            .Select(x => (long?)x.RoleId)
            .FirstOrDefaultAsync(cancellationToken);
        var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (user is null || !adminRoleId.HasValue || user.RoleId != adminRoleId.Value)
        {
            return NotFound(ApiResponse<AdminPasswordResetEmailDto>.Fail("Admin account not found"));
        }

        if (!user.IsActive)
        {
            return BadRequest(ApiResponse<AdminPasswordResetEmailDto>.Fail("Activate this admin account before sending a password reset email"));
        }

        var ticket = _passwordReset.CreateTicket(user.Email);
        var portalUrl = $"{Request.Scheme}://{Request.Host}/auth/forgot-password";
        var emailResult = await _email.SendPasswordResetAsync(
            user.Email,
            user.FullName,
            ticket.Code,
            ticket.ExpiresAtUtc,
            portalUrl,
            cancellationToken);

        await WriteAuditAsync(
            "AdminPasswordResetRequested",
            "User",
            user.UserId,
            user.FullName,
            $"Superadmin sent a password reset code to admin account {user.Email}.");

        var payload = new AdminPasswordResetEmailDto
        {
            Sent = emailResult.Sent,
            ResetCodePreview = _environment.IsDevelopment() ? ticket.Code : null,
            ExpiresAt = _environment.IsDevelopment() ? ticket.ExpiresAtUtc : null
        };

        return emailResult.Sent
            ? Ok(ApiResponse<AdminPasswordResetEmailDto>.Ok(payload, "Password reset email sent"))
            : BadRequest(ApiResponse<AdminPasswordResetEmailDto>.Fail(emailResult.Message));
    }

    private async Task<HospitalProfileDto> BuildHospitalProfileDtoAsync(CancellationToken cancellationToken)
    {
        var profile = await _db.HospitalProfiles.AsNoTracking().OrderBy(x => x.HospitalProfileId).FirstOrDefaultAsync(cancellationToken);
        return profile is null ? new HospitalProfileDto() : new HospitalProfileDto
        {
            HospitalProfileId = profile.HospitalProfileId,
            HospitalName = profile.HospitalName,
            LogoUrl = profile.LogoUrl,
            AddressLine1 = profile.AddressLine1,
            City = profile.City,
            StateOrProvince = profile.StateOrProvince,
            PostalCode = profile.PostalCode,
            Country = profile.Country,
            ContactEmail = profile.ContactEmail,
            ContactPhone = profile.ContactPhone,
            TaxLabel = profile.TaxLabel,
            TaxRegistrationNumber = profile.TaxRegistrationNumber,
            TaxPercentage = profile.TaxPercentage,
            CurrencyCode = profile.CurrencyCode,
            InvoicePrefix = profile.InvoicePrefix,
            InvoiceStartingNumber = profile.InvoiceStartingNumber,
            InvoiceFooterNote = profile.InvoiceFooterNote,
            MultiBranchEnabled = profile.MultiBranchEnabled
        };
    }

    private IQueryable<BranchDto> BuildBranchDtos() =>
        _db.Branches.AsNoTracking().OrderByDescending(x => x.IsPrimary).ThenBy(x => x.Name).Select(x => new BranchDto
        {
            BranchId = x.BranchId,
            Name = x.Name,
            Code = x.Code,
            Address = x.Address,
            ContactPhone = x.ContactPhone,
            ContactEmail = x.ContactEmail,
            IsPrimary = x.IsPrimary,
            IsActive = x.IsActive,
            TotalBeds = x.TotalBeds,
            OccupiedBeds = x.OccupiedBeds
        });

    private async Task<SuperAdminUserDto> BuildAdminDtoAsync(long userId, CancellationToken cancellationToken)
    {
        var users = await _db.Users
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);
        var user = users.First();
        var roleName = await _db.Roles
            .AsNoTracking()
            .Where(x => x.RoleId == user.RoleId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
        var hospitalName = user.HospitalProfileId.HasValue
            ? await _db.HospitalProfiles
                .AsNoTracking()
                .Where(x => x.HospitalProfileId == user.HospitalProfileId.Value)
                .Select(x => x.HospitalName)
                .FirstOrDefaultAsync(cancellationToken) ?? "Unassigned"
            : "Unassigned";

        return MapAdmin(user, roleName, hospitalName);
    }

    private static SuperAdminUserDto MapAdmin(User user, string roleName, string hospitalName) => new()
    {
        UserId = user.UserId,
        HospitalProfileId = user.HospitalProfileId,
        HospitalName = hospitalName,
        FullName = user.FullName,
        Email = user.Email,
        Phone = user.Phone,
        Role = roleName,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };

    private static BranchDto MapBranch(Branch branch) => new()
    {
        BranchId = branch.BranchId,
        Name = branch.Name,
        Code = branch.Code,
        Address = branch.Address,
        ContactPhone = branch.ContactPhone,
        ContactEmail = branch.ContactEmail,
        IsPrimary = branch.IsPrimary,
        IsActive = branch.IsActive,
        TotalBeds = branch.TotalBeds,
        OccupiedBeds = branch.OccupiedBeds
    };

    private async Task<string?> ValidateBranchAsync(UpsertBranchDto dto, long? branchId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Code))
        {
            return "Branch name and code are required";
        }

        var code = NormalizeRequired(dto.Code);
        var duplicateCode = await _db.Branches.AnyAsync(x => x.Code.ToLower() == code.ToLower() && x.BranchId != branchId, cancellationToken);
        if (duplicateCode)
        {
            return "Branch code already exists";
        }

        if (dto.OccupiedBeds > dto.TotalBeds)
        {
            return "Occupied beds cannot exceed total beds";
        }

        return null;
    }

    private static void ApplyBranch(Branch branch, UpsertBranchDto dto)
    {
        branch.Name = NormalizeRequired(dto.Name);
        branch.Code = NormalizeRequired(dto.Code).ToUpperInvariant();
        branch.Address = NormalizeRequired(dto.Address);
        branch.ContactPhone = NormalizeRequired(dto.ContactPhone);
        branch.ContactEmail = NormalizeRequired(dto.ContactEmail);
        branch.IsPrimary = dto.IsPrimary;
        branch.IsActive = dto.IsActive;
        branch.TotalBeds = Math.Max(dto.TotalBeds, 0);
        branch.OccupiedBeds = Math.Max(dto.OccupiedBeds, 0);
        branch.UpdatedAt = DateTime.UtcNow;
    }

    private async Task EnforceSinglePrimaryBranchAsync(Branch selectedBranch, CancellationToken cancellationToken)
    {
        if (!selectedBranch.IsPrimary)
        {
            return;
        }

        var otherPrimaryBranches = await _db.Branches
            .Where(x => x.BranchId != selectedBranch.BranchId && x.IsPrimary)
            .ToListAsync(cancellationToken);
        foreach (var branch in otherPrimaryBranches)
        {
            branch.IsPrimary = false;
            branch.UpdatedAt = DateTime.UtcNow;
        }

        if (otherPrimaryBranches.Count != 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private Task WriteAuditAsync(string action, string entityName, long entityId, string target, string summary) =>
        _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.Administration,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            TargetDisplayName = target,
            Summary = summary,
            PerformedByUserId = GetCurrentUserId(),
            PerformedByRole = "SuperAdmin"
        });

    private long? GetCurrentUserId() =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var userId)
            ? userId
            : null;

    private static string NormalizeRequired(string? value) => value?.Trim() ?? string.Empty;

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
    {
        using var hmac = new HMACSHA512();
        salt = hmac.Key;
        hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }
}
