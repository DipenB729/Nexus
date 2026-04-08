using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize(Policy = "AdminOnly")]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SettingsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("organization")]
    public async Task<ActionResult<ApiResponse<OrganizationSettingsDto>>> GetOrganizationSettings()
    {
        var profile = await _db.HospitalProfiles.AsNoTracking().OrderBy(x => x.HospitalProfileId).FirstOrDefaultAsync();
        var branches = await _db.Branches.AsNoTracking().OrderByDescending(x => x.IsPrimary).ThenBy(x => x.Name).ToListAsync();

        var payload = new OrganizationSettingsDto
        {
            Profile = profile is null ? new HospitalProfileDto() : MapProfile(profile),
            Branches = branches.Select(MapBranch).ToList()
        };

        return Ok(ApiResponse<OrganizationSettingsDto>.Ok(payload));
    }

    [HttpPut("organization")]
    public async Task<ActionResult<ApiResponse<OrganizationSettingsDto>>> UpdateOrganizationSettings([FromBody] OrganizationSettingsDto dto)
    {
        var profile = await _db.HospitalProfiles.OrderBy(x => x.HospitalProfileId).FirstOrDefaultAsync();
        if (profile is null)
        {
            profile = new HospitalProfile();
            _db.HospitalProfiles.Add(profile);
        }

        ApplyProfile(profile, dto.Profile);
        profile.UpdatedAt = DateTime.UtcNow;

        var existingBranches = await _db.Branches.OrderBy(x => x.BranchId).ToListAsync();
        var incomingBranches = dto.Branches?.ToList() ?? new List<BranchDto>();
        var primaryIndex = incomingBranches.FindIndex(x => x.IsPrimary);

        for (var index = 0; index < incomingBranches.Count; index++)
        {
            var branchDto = incomingBranches[index];
            Branch? branch = null;
            if (branchDto.BranchId > 0)
            {
                branch = existingBranches.FirstOrDefault(x => x.BranchId == branchDto.BranchId);
            }

            if (branch is null)
            {
                branch = new Branch
                {
                    CreatedAt = DateTime.UtcNow
                };
                _db.Branches.Add(branch);
                existingBranches.Add(branch);
            }

            branch.Name = branchDto.Name.Trim();
            branch.Code = branchDto.Code.Trim().ToUpperInvariant();
            branch.Address = branchDto.Address.Trim();
            branch.ContactPhone = branchDto.ContactPhone.Trim();
            branch.ContactEmail = branchDto.ContactEmail.Trim();
            branch.TotalBeds = branchDto.TotalBeds;
            branch.OccupiedBeds = Math.Min(branchDto.OccupiedBeds, branchDto.TotalBeds);
            branch.IsActive = branchDto.IsActive;
            branch.IsPrimary = primaryIndex >= 0 && primaryIndex == index;
            branch.UpdatedAt = DateTime.UtcNow;
        }

        var savedPrimary = existingBranches.FirstOrDefault(x => x.IsPrimary);
        if (savedPrimary is null && existingBranches.Count > 0)
        {
            existingBranches[0].IsPrimary = true;
        }

        await _db.SaveChangesAsync();

        var response = new OrganizationSettingsDto
        {
            Profile = MapProfile(profile),
            Branches = existingBranches
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.Name)
                .Select(MapBranch)
                .ToList()
        };

        return Ok(ApiResponse<OrganizationSettingsDto>.Ok(response, "Organization settings updated"));
    }

    private static HospitalProfileDto MapProfile(HospitalProfile profile)
    {
        return new HospitalProfileDto
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

    private static BranchDto MapBranch(Branch branch)
    {
        return new BranchDto
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
    }

    private static void ApplyProfile(HospitalProfile target, HospitalProfileDto source)
    {
        target.HospitalName = source.HospitalName.Trim();
        target.LogoUrl = string.IsNullOrWhiteSpace(source.LogoUrl) ? null : source.LogoUrl.Trim();
        target.AddressLine1 = source.AddressLine1.Trim();
        target.City = source.City.Trim();
        target.StateOrProvince = source.StateOrProvince.Trim();
        target.PostalCode = source.PostalCode.Trim();
        target.Country = source.Country.Trim();
        target.ContactEmail = source.ContactEmail.Trim();
        target.ContactPhone = source.ContactPhone.Trim();
        target.TaxLabel = source.TaxLabel.Trim();
        target.TaxRegistrationNumber = source.TaxRegistrationNumber.Trim();
        target.TaxPercentage = source.TaxPercentage;
        target.CurrencyCode = source.CurrencyCode.Trim().ToUpperInvariant();
        target.InvoicePrefix = source.InvoicePrefix.Trim().ToUpperInvariant();
        target.InvoiceStartingNumber = source.InvoiceStartingNumber;
        target.InvoiceFooterNote = source.InvoiceFooterNote.Trim();
        target.MultiBranchEnabled = source.MultiBranchEnabled;
    }
}
