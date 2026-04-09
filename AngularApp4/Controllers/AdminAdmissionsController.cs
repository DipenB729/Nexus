using System.Security.Claims;
using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/admin/admissions")]
[Authorize(Policy = "AdminOnly")]
public class AdminAdmissionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminAdmissionsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<AdmissionRecordDto>>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] AdmissionStatus? status = null)
    {
        var rows = await BuildAdmissionListAsync();

        if (status.HasValue)
        {
            rows = rows.Where(x => string.Equals(x.Status, status.Value.ToString(), StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            rows = rows.Where(x =>
                    x.PatientName.ToLowerInvariant().Contains(term) ||
                    x.AdmissionNumber.ToLowerInvariant().Contains(term) ||
                    x.MedicalRecordNumber.ToLowerInvariant().Contains(term) ||
                    x.DoctorName.ToLowerInvariant().Contains(term) ||
                    x.WardName.ToLowerInvariant().Contains(term) ||
                    x.BedNumber.ToLowerInvariant().Contains(term))
                .ToList();
        }

        return Ok(ApiResponse<IEnumerable<AdmissionRecordDto>>.Ok(rows));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AdmissionRecordDto>>> Create([FromBody] CreateAdmissionDto dto)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(x => x.PatientId == dto.PatientId && x.IsActive && !x.MergedIntoPatientId.HasValue);
        if (patient is null)
        {
            return BadRequest(ApiResponse<AdmissionRecordDto>.Fail("Selected patient is unavailable"));
        }

        var hasActiveAdmission = await _db.PatientAdmissions.AnyAsync(x =>
            x.PatientId == dto.PatientId &&
            x.Status != AdmissionStatus.Discharged);

        if (hasActiveAdmission)
        {
            return BadRequest(ApiResponse<AdmissionRecordDto>.Fail("Patient already has an active admission"));
        }

        var bed = await _db.Beds
            .Include(x => x.Ward)
            .FirstOrDefaultAsync(x => x.BedId == dto.BedId && x.IsActive);

        if (bed is null || bed.Ward is null)
        {
            return BadRequest(ApiResponse<AdmissionRecordDto>.Fail("Selected bed is unavailable"));
        }

        if (bed.WardId != dto.WardId)
        {
            return BadRequest(ApiResponse<AdmissionRecordDto>.Fail("Bed does not belong to the selected ward"));
        }

        if (bed.IsOccupied)
        {
            return BadRequest(ApiResponse<AdmissionRecordDto>.Fail("Selected bed is already occupied"));
        }

        if (dto.AppointmentId.HasValue)
        {
            var appointment = await _db.Appointments.FirstOrDefaultAsync(x =>
                x.AppointmentId == dto.AppointmentId.Value &&
                x.PatientId == dto.PatientId);

            if (appointment is null)
            {
                return BadRequest(ApiResponse<AdmissionRecordDto>.Fail("Selected appointment was not found for this patient"));
            }
        }

        if (dto.DoctorId.HasValue && !await _db.Doctors.AnyAsync(x => x.DoctorId == dto.DoctorId.Value))
        {
            return BadRequest(ApiResponse<AdmissionRecordDto>.Fail("Selected doctor was not found"));
        }

        var admission = new PatientAdmission
        {
            AdmissionNumber = await GenerateAdmissionNumberAsync(dto.AdmissionDate.Date),
            PatientId = dto.PatientId,
            AppointmentId = dto.AppointmentId,
            DoctorId = dto.DoctorId,
            BranchId = bed.BranchId,
            WardId = bed.WardId,
            BedId = bed.BedId,
            Status = AdmissionStatus.Active,
            AdmissionDate = dto.AdmissionDate,
            ExpectedDischargeDate = dto.ExpectedDischargeDate,
            Reason = Normalize(dto.Reason),
            Notes = Normalize(dto.Notes),
            CreatedAt = DateTime.UtcNow
        };

        bed.IsOccupied = true;
        bed.UpdatedAt = DateTime.UtcNow;

        _db.PatientAdmissions.Add(admission);
        await _db.SaveChangesAsync();
        await RefreshBranchOccupancyAsync(bed.BranchId);

        var payload = (await BuildAdmissionListAsync()).First(x => x.PatientAdmissionId == admission.PatientAdmissionId);
        return Ok(ApiResponse<AdmissionRecordDto>.Ok(payload, "Patient admitted successfully"));
    }

    [HttpPost("{admissionId:long}/transfer")]
    public async Task<ActionResult<ApiResponse<AdmissionRecordDto>>> Transfer(long admissionId, [FromBody] TransferAdmissionDto dto)
    {
        var admission = await _db.PatientAdmissions.FirstOrDefaultAsync(x => x.PatientAdmissionId == admissionId);
        if (admission is null)
        {
            return NotFound(ApiResponse<AdmissionRecordDto>.Fail("Admission record not found"));
        }

        if (admission.Status == AdmissionStatus.Discharged)
        {
            return BadRequest(ApiResponse<AdmissionRecordDto>.Fail("Discharged admissions cannot be transferred"));
        }

        var currentBed = await _db.Beds.FirstOrDefaultAsync(x => x.BedId == admission.BedId);
        var targetBed = await _db.Beds
            .Include(x => x.Ward)
            .FirstOrDefaultAsync(x => x.BedId == dto.BedId && x.IsActive);

        if (currentBed is null || targetBed is null || targetBed.Ward is null)
        {
            return BadRequest(ApiResponse<AdmissionRecordDto>.Fail("Unable to resolve source or destination bed"));
        }

        if (targetBed.WardId != dto.WardId)
        {
            return BadRequest(ApiResponse<AdmissionRecordDto>.Fail("Destination bed does not belong to the selected ward"));
        }

        if (targetBed.IsOccupied)
        {
            return BadRequest(ApiResponse<AdmissionRecordDto>.Fail("Destination bed is already occupied"));
        }

        _db.AdmissionTransfers.Add(new AdmissionTransfer
        {
            PatientAdmissionId = admission.PatientAdmissionId,
            FromWardId = admission.WardId,
            FromBedId = admission.BedId,
            ToWardId = targetBed.WardId,
            ToBedId = targetBed.BedId,
            TransferDate = dto.TransferDate,
            Notes = Normalize(dto.Notes),
            CreatedAt = DateTime.UtcNow
        });

        currentBed.IsOccupied = false;
        currentBed.UpdatedAt = DateTime.UtcNow;
        targetBed.IsOccupied = true;
        targetBed.UpdatedAt = DateTime.UtcNow;

        admission.BranchId = targetBed.BranchId;
        admission.WardId = targetBed.WardId;
        admission.BedId = targetBed.BedId;
        admission.Status = AdmissionStatus.Active;
        admission.UpdatedAt = DateTime.UtcNow;
        admission.Notes = CombineNotes(admission.Notes, dto.Notes);

        await _db.SaveChangesAsync();
        await RefreshBranchOccupancyAsync(currentBed.BranchId, targetBed.BranchId);

        var payload = (await BuildAdmissionListAsync()).First(x => x.PatientAdmissionId == admission.PatientAdmissionId);
        return Ok(ApiResponse<AdmissionRecordDto>.Ok(payload, "Patient transfer recorded"));
    }

    [HttpPost("{admissionId:long}/discharge")]
    public async Task<ActionResult<ApiResponse<AdmissionRecordDto>>> ApproveDischarge(long admissionId, [FromBody] ApproveDischargeDto dto)
    {
        var admission = await _db.PatientAdmissions.FirstOrDefaultAsync(x => x.PatientAdmissionId == admissionId);
        if (admission is null)
        {
            return NotFound(ApiResponse<AdmissionRecordDto>.Fail("Admission record not found"));
        }

        if (admission.Status == AdmissionStatus.Discharged)
        {
            return BadRequest(ApiResponse<AdmissionRecordDto>.Fail("Admission has already been discharged"));
        }

        if (string.IsNullOrWhiteSpace(dto.DischargeSummary))
        {
            return BadRequest(ApiResponse<AdmissionRecordDto>.Fail("Discharge summary is required for approval"));
        }

        var bed = await _db.Beds.FirstOrDefaultAsync(x => x.BedId == admission.BedId);
        if (bed is not null)
        {
            bed.IsOccupied = false;
            bed.UpdatedAt = DateTime.UtcNow;
        }

        admission.Status = AdmissionStatus.Discharged;
        admission.DischargeDate = dto.DischargeDate;
        admission.DischargeSummary = dto.DischargeSummary.Trim();
        admission.DischargeApprovedAt = DateTime.UtcNow;
        admission.DischargeApprovedByUserId = GetUserId();
        admission.Notes = CombineNotes(admission.Notes, dto.Notes);
        admission.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        if (bed is not null)
        {
            await RefreshBranchOccupancyAsync(bed.BranchId);
        }

        var payload = (await BuildAdmissionListAsync()).First(x => x.PatientAdmissionId == admission.PatientAdmissionId);
        return Ok(ApiResponse<AdmissionRecordDto>.Ok(payload, "Discharge summary approved"));
    }

    private async Task<List<AdmissionRecordDto>> BuildAdmissionListAsync()
    {
        var admissions = await _db.PatientAdmissions
            .AsNoTracking()
            .OrderByDescending(x => x.AdmissionDate)
            .ThenByDescending(x => x.PatientAdmissionId)
            .ToListAsync();

        var patientIds = admissions.Select(x => x.PatientId).Distinct().ToList();
        var doctorIds = admissions.Where(x => x.DoctorId.HasValue).Select(x => x.DoctorId!.Value).Distinct().ToList();
        var branchIds = admissions.Select(x => x.BranchId).Distinct().ToList();
        var wardIds = admissions.Select(x => x.WardId).Distinct().ToList();
        var bedIds = admissions.Select(x => x.BedId).Distinct().ToList();
        var admissionIds = admissions.Select(x => x.PatientAdmissionId).ToList();

        var patients = await _db.Patients
            .AsNoTracking()
            .Where(x => patientIds.Contains(x.PatientId))
            .ToDictionaryAsync(x => x.PatientId);

        var userIds = patients.Values.Select(x => x.UserId).Distinct().ToList();
        var users = await _db.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId);

        var doctors = await _db.Doctors
            .AsNoTracking()
            .Where(x => doctorIds.Contains(x.DoctorId))
            .ToDictionaryAsync(x => x.DoctorId);

        var branches = await _db.Branches
            .AsNoTracking()
            .Where(x => branchIds.Contains(x.BranchId))
            .ToDictionaryAsync(x => x.BranchId);

        var wards = await _db.Wards
            .AsNoTracking()
            .Where(x => wardIds.Contains(x.WardId))
            .ToDictionaryAsync(x => x.WardId);

        var beds = await _db.Beds
            .AsNoTracking()
            .Where(x => bedIds.Contains(x.BedId))
            .ToDictionaryAsync(x => x.BedId);

        var transfers = await _db.AdmissionTransfers
            .AsNoTracking()
            .Where(x => admissionIds.Contains(x.PatientAdmissionId))
            .OrderByDescending(x => x.TransferDate)
            .ThenByDescending(x => x.AdmissionTransferId)
            .ToListAsync();

        return admissions.Select(admission =>
        {
            patients.TryGetValue(admission.PatientId, out var patient);
            users.TryGetValue(patient?.UserId ?? 0, out var user);
            var admissionTransfers = transfers
                .Where(x => x.PatientAdmissionId == admission.PatientAdmissionId)
                .Select(transfer => new AdmissionTransferDto
                {
                    AdmissionTransferId = transfer.AdmissionTransferId,
                    FromWardName = transfer.FromWardId.HasValue && wards.TryGetValue(transfer.FromWardId.Value, out var fromWard) ? fromWard.Name : null,
                    FromBedNumber = transfer.FromBedId.HasValue && beds.TryGetValue(transfer.FromBedId.Value, out var fromBed) ? fromBed.BedNumber : null,
                    ToWardName = wards.TryGetValue(transfer.ToWardId, out var toWard) ? toWard.Name : string.Empty,
                    ToBedNumber = beds.TryGetValue(transfer.ToBedId, out var toBed) ? toBed.BedNumber : string.Empty,
                    TransferDate = transfer.TransferDate,
                    Notes = transfer.Notes
                })
                .ToList();

            return new AdmissionRecordDto
            {
                PatientAdmissionId = admission.PatientAdmissionId,
                AdmissionNumber = admission.AdmissionNumber,
                PatientId = admission.PatientId,
                PatientName = user?.FullName ?? "Unknown patient",
                MedicalRecordNumber = patient?.MedicalRecordNumber ?? $"MRN-{admission.PatientId:D5}",
                AppointmentId = admission.AppointmentId,
                DoctorId = admission.DoctorId,
                DoctorName = admission.DoctorId.HasValue && doctors.TryGetValue(admission.DoctorId.Value, out var doctor) ? doctor.FullName : string.Empty,
                BranchId = admission.BranchId,
                BranchName = branches.TryGetValue(admission.BranchId, out var branch) ? branch.Name : string.Empty,
                WardId = admission.WardId,
                WardName = wards.TryGetValue(admission.WardId, out var ward) ? ward.Name : string.Empty,
                BedId = admission.BedId,
                BedNumber = beds.TryGetValue(admission.BedId, out var bed) ? bed.BedNumber : string.Empty,
                AdmissionDate = admission.AdmissionDate,
                ExpectedDischargeDate = admission.ExpectedDischargeDate,
                DischargeDate = admission.DischargeDate,
                Status = admission.Status.ToString(),
                Reason = admission.Reason,
                Notes = admission.Notes,
                DischargeSummary = admission.DischargeSummary,
                DischargeApprovedAt = admission.DischargeApprovedAt,
                Transfers = admissionTransfers
            };
        }).ToList();
    }

    private async Task<string> GenerateAdmissionNumberAsync(DateTime admissionDate)
    {
        var count = await _db.PatientAdmissions.CountAsync(x => x.AdmissionDate.Date == admissionDate.Date);
        return $"ADM-{admissionDate:yyyyMMdd}-{(count + 1).ToString("D3")}";
    }

    private async Task RefreshBranchOccupancyAsync(params long[] branchIds)
    {
        var distinctBranchIds = branchIds.Distinct().ToList();
        if (distinctBranchIds.Count == 0)
        {
            return;
        }

        var occupancyMap = await _db.Beds
            .AsNoTracking()
            .Where(x => distinctBranchIds.Contains(x.BranchId) && x.IsActive)
            .GroupBy(x => x.BranchId)
            .Select(group => new { BranchId = group.Key, OccupiedBeds = group.Count(x => x.IsOccupied) })
            .ToDictionaryAsync(x => x.BranchId, x => x.OccupiedBeds);

        var branches = await _db.Branches.Where(x => distinctBranchIds.Contains(x.BranchId)).ToListAsync();
        foreach (var branch in branches)
        {
            branch.OccupiedBeds = occupancyMap.GetValueOrDefault(branch.BranchId);
            branch.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    private long? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return long.TryParse(value, out var userId) ? userId : null;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
