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
[Route("api/doctor/workspace")]
[Authorize(Policy = "DoctorOnly")]
public class DoctorWorkspaceController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IDoctorAvailabilityService _availability;
    private readonly IAppNotificationService _notifications;

    public DoctorWorkspaceController(AppDbContext db, IDoctorAvailabilityService availability, IAppNotificationService notifications)
    {
        _db = db;
        _availability = availability;
        _notifications = notifications;
    }

    [HttpGet("availability")]
    public async Task<ActionResult<ApiResponse<DoctorAvailabilityWorkspaceDto>>> GetAvailability(CancellationToken cancellationToken)
    {
        var doctor = await GetCurrentDoctorAsync(cancellationToken);
        if (doctor is null)
        {
            return NotFound(ApiResponse<DoctorAvailabilityWorkspaceDto>.Fail("Doctor profile not found"));
        }

        var schedules = await _db.DoctorSchedules
            .AsNoTracking()
            .Where(x => x.DoctorId == doctor.DoctorId)
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.StartTime)
            .Select(x => new DoctorScheduleDto
            {
                ScheduleId = x.ScheduleId,
                DoctorId = x.DoctorId,
                DayOfWeek = x.DayOfWeek,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                SlotDurationMinutes = x.SlotDurationMinutes,
                MaxPatientsPerSlot = x.MaxPatientsPerSlot,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var exceptions = await _db.DoctorAvailabilityExceptions
            .AsNoTracking()
            .Where(x => x.DoctorId == doctor.DoctorId)
            .OrderByDescending(x => x.StartDate)
            .ThenByDescending(x => x.EndDate)
            .Select(x => new DoctorAvailabilityExceptionDto
            {
                DoctorAvailabilityExceptionId = x.DoctorAvailabilityExceptionId,
                ExceptionType = x.ExceptionType.ToString(),
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<DoctorAvailabilityWorkspaceDto>.Ok(new DoctorAvailabilityWorkspaceDto
        {
            Schedules = schedules,
            Exceptions = exceptions
        }));
    }

    [HttpPost("schedules")]
    public async Task<ActionResult<ApiResponse<DoctorScheduleDto>>> CreateSchedule([FromBody] SaveDoctorScheduleDto dto, CancellationToken cancellationToken)
    {
        var doctor = await GetCurrentDoctorAsync(cancellationToken);
        if (doctor is null)
        {
            return NotFound(ApiResponse<DoctorScheduleDto>.Fail("Doctor profile not found"));
        }

        var validation = await ValidateScheduleAsync(doctor.DoctorId, dto, null, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<DoctorScheduleDto>.Fail(validation));
        }

        var schedule = new DoctorSchedule
        {
            DoctorId = doctor.DoctorId,
            DayOfWeek = dto.DayOfWeek,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            SlotDurationMinutes = dto.SlotDurationMinutes,
            MaxPatientsPerSlot = dto.MaxPatientsPerSlot,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.DoctorSchedules.Add(schedule);
        await _db.SaveChangesAsync(cancellationToken);
        await _availability.SyncDoctorAvailabilitySummaryAsync(doctor.DoctorId, cancellationToken);

        return Ok(ApiResponse<DoctorScheduleDto>.Ok(MapSchedule(schedule), "Schedule created"));
    }

    [HttpPut("schedules/{scheduleId:long}")]
    public async Task<ActionResult<ApiResponse<DoctorScheduleDto>>> UpdateSchedule(long scheduleId, [FromBody] SaveDoctorScheduleDto dto, CancellationToken cancellationToken)
    {
        var doctor = await GetCurrentDoctorAsync(cancellationToken);
        if (doctor is null)
        {
            return NotFound(ApiResponse<DoctorScheduleDto>.Fail("Doctor profile not found"));
        }

        var schedule = await _db.DoctorSchedules.FirstOrDefaultAsync(x => x.ScheduleId == scheduleId && x.DoctorId == doctor.DoctorId, cancellationToken);
        if (schedule is null)
        {
            return NotFound(ApiResponse<DoctorScheduleDto>.Fail("Schedule not found"));
        }

        var validation = await ValidateScheduleAsync(doctor.DoctorId, dto, scheduleId, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<DoctorScheduleDto>.Fail(validation));
        }

        schedule.DayOfWeek = dto.DayOfWeek;
        schedule.StartTime = dto.StartTime;
        schedule.EndTime = dto.EndTime;
        schedule.SlotDurationMinutes = dto.SlotDurationMinutes;
        schedule.MaxPatientsPerSlot = dto.MaxPatientsPerSlot;
        schedule.IsActive = dto.IsActive;
        schedule.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await _availability.SyncDoctorAvailabilitySummaryAsync(doctor.DoctorId, cancellationToken);

        return Ok(ApiResponse<DoctorScheduleDto>.Ok(MapSchedule(schedule), "Schedule updated"));
    }

    [HttpDelete("schedules/{scheduleId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteSchedule(long scheduleId, CancellationToken cancellationToken)
    {
        var doctor = await GetCurrentDoctorAsync(cancellationToken);
        if (doctor is null)
        {
            return NotFound(ApiResponse<object>.Fail("Doctor profile not found"));
        }

        var schedule = await _db.DoctorSchedules.FirstOrDefaultAsync(x => x.ScheduleId == scheduleId && x.DoctorId == doctor.DoctorId, cancellationToken);
        if (schedule is null)
        {
            return NotFound(ApiResponse<object>.Fail("Schedule not found"));
        }

        _db.DoctorSchedules.Remove(schedule);
        await _db.SaveChangesAsync(cancellationToken);
        await _availability.SyncDoctorAvailabilitySummaryAsync(doctor.DoctorId, cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, "Schedule deleted"));
    }

    [HttpPost("exceptions")]
    public async Task<ActionResult<ApiResponse<DoctorAvailabilityExceptionDto>>> CreateException([FromBody] SaveDoctorAvailabilityExceptionDto dto, CancellationToken cancellationToken)
    {
        var doctor = await GetCurrentDoctorAsync(cancellationToken);
        if (doctor is null)
        {
            return NotFound(ApiResponse<DoctorAvailabilityExceptionDto>.Fail("Doctor profile not found"));
        }

        if (!Enum.TryParse<DoctorAvailabilityExceptionType>(dto.ExceptionType, true, out var exceptionType))
        {
            return BadRequest(ApiResponse<DoctorAvailabilityExceptionDto>.Fail("Invalid availability exception type"));
        }

        if (dto.EndDate.Date < dto.StartDate.Date)
        {
            return BadRequest(ApiResponse<DoctorAvailabilityExceptionDto>.Fail("End date must be on or after the start date"));
        }

        var item = new DoctorAvailabilityException
        {
            DoctorId = doctor.DoctorId,
            ExceptionType = exceptionType,
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate.Date,
            Notes = Normalize(dto.Notes),
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.DoctorAvailabilityExceptions.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<DoctorAvailabilityExceptionDto>.Ok(MapException(item), "Availability exception saved"));
    }

    [HttpDelete("exceptions/{exceptionId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteException(long exceptionId, CancellationToken cancellationToken)
    {
        var doctor = await GetCurrentDoctorAsync(cancellationToken);
        if (doctor is null)
        {
            return NotFound(ApiResponse<object>.Fail("Doctor profile not found"));
        }

        var item = await _db.DoctorAvailabilityExceptions
            .FirstOrDefaultAsync(x => x.DoctorAvailabilityExceptionId == exceptionId && x.DoctorId == doctor.DoctorId, cancellationToken);
        if (item is null)
        {
            return NotFound(ApiResponse<object>.Fail("Availability exception not found"));
        }

        _db.DoctorAvailabilityExceptions.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Availability exception deleted"));
    }

    [HttpGet("appointments/{appointmentId:long}")]
    public async Task<ActionResult<ApiResponse<DoctorWorkspaceAppointmentDetailDto>>> GetAppointmentDetail(long appointmentId, CancellationToken cancellationToken)
    {
        var doctor = await GetCurrentDoctorAsync(cancellationToken);
        if (doctor is null)
        {
            return NotFound(ApiResponse<DoctorWorkspaceAppointmentDetailDto>.Fail("Doctor profile not found"));
        }

        var appointment = await _db.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AppointmentId == appointmentId && x.DoctorId == doctor.DoctorId, cancellationToken);
        if (appointment is null)
        {
            return NotFound(ApiResponse<DoctorWorkspaceAppointmentDetailDto>.Fail("Appointment not found"));
        }

        var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(x => x.PatientId == appointment.PatientId, cancellationToken);
        if (patient is null)
        {
            return NotFound(ApiResponse<DoctorWorkspaceAppointmentDetailDto>.Fail("Patient record not found"));
        }

        var patientUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == patient.UserId, cancellationToken);
        var serviceName = await _db.Services.AsNoTracking()
            .Where(x => x.Id == appointment.ServiceId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken);
        var departmentName = await _db.Doctors.AsNoTracking()
            .Where(x => x.DoctorId == doctor.DoctorId)
            .Join(_db.Departments.AsNoTracking(), d => d.DepartmentId, dept => dept.DepartmentId, (d, dept) => dept.Name)
            .FirstOrDefaultAsync(cancellationToken);
        var clinicalProfile = await _db.PatientClinicalProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.PatientId == patient.PatientId, cancellationToken);
        var consultation = await _db.DoctorConsultations.AsNoTracking().FirstOrDefaultAsync(x => x.AppointmentId == appointment.AppointmentId, cancellationToken);

        var previousPrescriptions = await _db.DoctorPrescriptions
            .AsNoTracking()
            .Where(x => x.PatientId == patient.PatientId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
            .Select(x => new DoctorPrescriptionDto
            {
                DoctorPrescriptionId = x.DoctorPrescriptionId,
                AppointmentId = x.AppointmentId,
                CreatedAt = x.CreatedAt,
                Notes = x.Notes
            })
            .ToListAsync(cancellationToken);

        var prescriptionIds = previousPrescriptions.Select(x => x.DoctorPrescriptionId).ToList();
        var prescriptionItems = await _db.DoctorPrescriptionItems
            .AsNoTracking()
            .Where(x => prescriptionIds.Contains(x.DoctorPrescriptionId))
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);
        foreach (var prescription in previousPrescriptions)
        {
            prescription.Items = prescriptionItems
                .Where(x => x.DoctorPrescriptionId == prescription.DoctorPrescriptionId)
                .Select(x => new DoctorPrescriptionItemDto
                {
                    MedicineMasterId = x.MedicineMasterId,
                    MedicineName = x.MedicineName,
                    Dosage = x.Dosage,
                    Frequency = x.Frequency,
                    Duration = x.Duration,
                    Instructions = x.Instructions
                })
                .ToList();
        }

        var diagnosticRequests = await _db.DiagnosticRequests
            .AsNoTracking()
            .Where(x => x.PatientId == patient.PatientId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .Select(x => new DiagnosticRequestDto
            {
                DiagnosticRequestId = x.DiagnosticRequestId,
                AppointmentId = x.AppointmentId,
                RequestType = x.RequestType.ToString(),
                LabTestMasterId = x.LabTestMasterId,
                RequestedItemName = x.RequestedItemName,
                Remarks = x.Remarks,
                ResultSummary = x.ResultSummary,
                Status = x.Status.ToString(),
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var pastAppointments = await (
                from item in _db.Appointments.AsNoTracking()
                join itemDoctor in _db.Doctors.AsNoTracking() on item.DoctorId equals itemDoctor.DoctorId
                join itemService in _db.Services.AsNoTracking() on item.ServiceId equals itemService.Id into serviceJoin
                from itemService in serviceJoin.DefaultIfEmpty()
                join itemConsultation in _db.DoctorConsultations.AsNoTracking() on item.AppointmentId equals itemConsultation.AppointmentId into consultationJoin
                from itemConsultation in consultationJoin.DefaultIfEmpty()
                where item.PatientId == patient.PatientId && item.AppointmentId != appointment.AppointmentId
                orderby item.AppointmentDate descending, item.SlotStartTime descending
                select new PatientAppointmentHistoryDto
                {
                    AppointmentId = item.AppointmentId,
                    AppointmentDate = item.AppointmentDate,
                    DoctorName = itemDoctor.FullName,
                    Diagnosis = itemConsultation != null ? itemConsultation.Diagnosis : null,
                    ServiceName = itemService != null ? itemService.Name : null,
                    Status = item.Status.ToString()
                })
            .Take(12)
            .ToListAsync(cancellationToken);

        var admissionHistory = await _db.PatientAdmissions
            .AsNoTracking()
            .Where(x => x.PatientId == patient.PatientId)
            .OrderByDescending(x => x.AdmissionDate)
            .Take(10)
            .Select(x => new PatientAdmissionHistoryDto
            {
                PatientAdmissionId = x.PatientAdmissionId,
                AdmissionNumber = x.AdmissionNumber,
                AdmissionDate = x.AdmissionDate,
                DischargeDate = x.DischargeDate,
                Status = x.Status.ToString(),
                WardName = x.Ward != null ? x.Ward.Name : null,
                BedNumber = x.Bed != null ? x.Bed.BedNumber : null,
                Reason = x.Reason
            })
            .ToListAsync(cancellationToken);

        var documents = await _db.PatientDocuments
            .AsNoTracking()
            .Where(x => x.PatientId == patient.PatientId)
            .OrderByDescending(x => x.UploadedAt)
            .Take(10)
            .Select(x => new PatientDocumentDto
            {
                PatientDocumentId = x.PatientDocumentId,
                Category = x.Category,
                Title = x.Title,
                FileUrl = x.FileUrl,
                Notes = x.Notes,
                UploadedByRole = x.UploadedByRole,
                UploadedAt = x.UploadedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<DoctorWorkspaceAppointmentDetailDto>.Ok(new DoctorWorkspaceAppointmentDetailDto
        {
            AppointmentId = appointment.AppointmentId,
            AppointmentDate = appointment.AppointmentDate,
            SlotStartTime = appointment.SlotStartTime,
            SlotEndTime = appointment.SlotEndTime,
            Status = appointment.Status.ToString(),
            TokenNumber = appointment.TokenNumber,
            Reason = appointment.Reason,
            AdminRemarks = appointment.AdminRemarks,
            DoctorId = doctor.DoctorId,
            DoctorName = doctor.FullName,
            PatientId = patient.PatientId,
            PatientName = patientUser?.FullName ?? "Unknown patient",
            MedicalRecordNumber = patient.MedicalRecordNumber ?? $"MRN-{patient.PatientId:D5}",
            PatientEmail = patientUser?.Email,
            PatientPhone = patientUser?.Phone,
            Gender = patient.Gender,
            DateOfBirth = patient.DateOfBirth,
            BloodGroup = patient.BloodGroup,
            EmergencyContact = patient.EmergencyContact,
            Address = patient.Address,
            ServiceName = serviceName,
            DepartmentName = departmentName,
            ClinicalProfile = new PatientClinicalProfileDto
            {
                MedicalHistory = clinicalProfile?.MedicalHistory,
                Allergies = clinicalProfile?.Allergies,
                ChronicConditions = clinicalProfile?.ChronicConditions,
                CurrentMedications = clinicalProfile?.CurrentMedications
            },
            Consultation = new DoctorConsultationDto
            {
                DoctorConsultationId = consultation?.DoctorConsultationId,
                Symptoms = consultation?.Symptoms,
                Diagnosis = consultation?.Diagnosis,
                Notes = consultation?.Notes,
                VitalObservations = consultation?.VitalObservations,
                Advice = consultation?.Advice,
                FollowUpDate = consultation?.FollowUpDate,
                Status = consultation?.Status.ToString() ?? DoctorConsultationStatus.Draft.ToString(),
                UpdatedAt = consultation?.UpdatedAt ?? consultation?.CreatedAt
            },
            PreviousPrescriptions = previousPrescriptions,
            DiagnosticRequests = diagnosticRequests,
            PastAppointments = pastAppointments,
            AdmissionHistory = admissionHistory,
            UploadedReports = documents
        }));
    }

    [HttpPut("appointments/{appointmentId:long}/consultation")]
    public async Task<ActionResult<ApiResponse<DoctorConsultationDto>>> SaveConsultation(long appointmentId, [FromBody] SaveDoctorConsultationDto dto, CancellationToken cancellationToken)
    {
        var doctor = await GetCurrentDoctorAsync(cancellationToken);
        if (doctor is null)
        {
            return NotFound(ApiResponse<DoctorConsultationDto>.Fail("Doctor profile not found"));
        }

        var appointment = await _db.Appointments.FirstOrDefaultAsync(x => x.AppointmentId == appointmentId && x.DoctorId == doctor.DoctorId, cancellationToken);
        if (appointment is null)
        {
            return NotFound(ApiResponse<DoctorConsultationDto>.Fail("Appointment not found"));
        }

        if (!Enum.TryParse<DoctorConsultationStatus>(dto.Status ?? "Draft", true, out var status))
        {
            return BadRequest(ApiResponse<DoctorConsultationDto>.Fail("Invalid consultation status"));
        }

        var consultation = await _db.DoctorConsultations.FirstOrDefaultAsync(x => x.AppointmentId == appointmentId, cancellationToken);
        if (consultation is null)
        {
            consultation = new DoctorConsultation
            {
                AppointmentId = appointment.AppointmentId,
                DoctorId = doctor.DoctorId,
                PatientId = appointment.PatientId,
                CreatedAt = DateTime.UtcNow
            };
            _db.DoctorConsultations.Add(consultation);
        }

        consultation.Symptoms = Normalize(dto.Symptoms);
        consultation.Diagnosis = Normalize(dto.Diagnosis);
        consultation.Notes = Normalize(dto.Notes);
        consultation.VitalObservations = Normalize(dto.VitalObservations);
        consultation.Advice = Normalize(dto.Advice);
        consultation.FollowUpDate = dto.FollowUpDate?.Date;
        consultation.Status = status;
        consultation.UpdatedAt = DateTime.UtcNow;

        if (status == DoctorConsultationStatus.Completed && appointment.Status != AppointmentStatus.Completed)
        {
            appointment.Status = AppointmentStatus.Completed;
            appointment.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        if (consultation.FollowUpDate.HasValue)
        {
            await _notifications.QueueFollowUpReminderAsync(appointment, consultation.FollowUpDate.Value, doctor.FullName, cancellationToken);
        }

        return Ok(ApiResponse<DoctorConsultationDto>.Ok(new DoctorConsultationDto
        {
            DoctorConsultationId = consultation.DoctorConsultationId,
            Symptoms = consultation.Symptoms,
            Diagnosis = consultation.Diagnosis,
            Notes = consultation.Notes,
            VitalObservations = consultation.VitalObservations,
            Advice = consultation.Advice,
            FollowUpDate = consultation.FollowUpDate,
            Status = consultation.Status.ToString(),
            UpdatedAt = consultation.UpdatedAt
        }, "Consultation saved"));
    }

    [HttpPost("appointments/{appointmentId:long}/prescriptions")]
    public async Task<ActionResult<ApiResponse<DoctorPrescriptionDto>>> SavePrescription(long appointmentId, [FromBody] SaveDoctorPrescriptionDto dto, CancellationToken cancellationToken)
    {
        var doctor = await GetCurrentDoctorAsync(cancellationToken);
        if (doctor is null)
        {
            return NotFound(ApiResponse<DoctorPrescriptionDto>.Fail("Doctor profile not found"));
        }

        var appointment = await _db.Appointments.FirstOrDefaultAsync(x => x.AppointmentId == appointmentId && x.DoctorId == doctor.DoctorId, cancellationToken);
        if (appointment is null)
        {
            return NotFound(ApiResponse<DoctorPrescriptionDto>.Fail("Appointment not found"));
        }

        if (dto.Items.Count == 0)
        {
            return BadRequest(ApiResponse<DoctorPrescriptionDto>.Fail("At least one medicine is required"));
        }

        var normalizedItems = dto.Items
            .Where(x => !string.IsNullOrWhiteSpace(x.MedicineName))
            .ToList();
        if (normalizedItems.Count == 0)
        {
            return BadRequest(ApiResponse<DoctorPrescriptionDto>.Fail("At least one medicine is required"));
        }

        var prescription = new DoctorPrescription
        {
            AppointmentId = appointment.AppointmentId,
            DoctorId = doctor.DoctorId,
            PatientId = appointment.PatientId,
            Notes = Normalize(dto.Notes),
            CreatedAt = DateTime.UtcNow
        };

        _db.DoctorPrescriptions.Add(prescription);
        await _db.SaveChangesAsync(cancellationToken);

        var items = normalizedItems.Select((item, index) => new DoctorPrescriptionItem
        {
            DoctorPrescriptionId = prescription.DoctorPrescriptionId,
            MedicineMasterId = item.MedicineMasterId,
            MedicineName = item.MedicineName.Trim(),
            Dosage = Normalize(item.Dosage),
            Frequency = Normalize(item.Frequency),
            Duration = Normalize(item.Duration),
            Instructions = Normalize(item.Instructions),
            SortOrder = index
        }).ToList();

        _db.DoctorPrescriptionItems.AddRange(items);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<DoctorPrescriptionDto>.Ok(new DoctorPrescriptionDto
        {
            DoctorPrescriptionId = prescription.DoctorPrescriptionId,
            AppointmentId = prescription.AppointmentId,
            CreatedAt = prescription.CreatedAt,
            Notes = prescription.Notes,
            Items = items.Select(x => new DoctorPrescriptionItemDto
            {
                MedicineMasterId = x.MedicineMasterId,
                MedicineName = x.MedicineName,
                Dosage = x.Dosage,
                Frequency = x.Frequency,
                Duration = x.Duration,
                Instructions = x.Instructions
            }).ToList()
        }, "Prescription saved"));
    }

    [HttpPost("appointments/{appointmentId:long}/requests")]
    public async Task<ActionResult<ApiResponse<DiagnosticRequestDto>>> SaveDiagnosticRequest(long appointmentId, [FromBody] SaveDiagnosticRequestDto dto, CancellationToken cancellationToken)
    {
        var doctor = await GetCurrentDoctorAsync(cancellationToken);
        if (doctor is null)
        {
            return NotFound(ApiResponse<DiagnosticRequestDto>.Fail("Doctor profile not found"));
        }

        var appointment = await _db.Appointments.FirstOrDefaultAsync(x => x.AppointmentId == appointmentId && x.DoctorId == doctor.DoctorId, cancellationToken);
        if (appointment is null)
        {
            return NotFound(ApiResponse<DiagnosticRequestDto>.Fail("Appointment not found"));
        }

        if (!Enum.TryParse<DiagnosticRequestType>(dto.RequestType, true, out var requestType))
        {
            return BadRequest(ApiResponse<DiagnosticRequestDto>.Fail("Invalid request type"));
        }

        var itemName = requestType == DiagnosticRequestType.Lab && dto.LabTestMasterId.HasValue
            ? await _db.LabTestMasters.AsNoTracking().Where(x => x.LabTestMasterId == dto.LabTestMasterId.Value).Select(x => x.TestName).FirstOrDefaultAsync(cancellationToken)
            : null;

        var requestedItemName = !string.IsNullOrWhiteSpace(itemName) ? itemName : dto.RequestedItemName.Trim();
        if (string.IsNullOrWhiteSpace(requestedItemName))
        {
            return BadRequest(ApiResponse<DiagnosticRequestDto>.Fail("Requested item name is required"));
        }

        var request = new DiagnosticRequest
        {
            AppointmentId = appointment.AppointmentId,
            DoctorId = doctor.DoctorId,
            PatientId = appointment.PatientId,
            RequestType = requestType,
            LabTestMasterId = requestType == DiagnosticRequestType.Lab ? dto.LabTestMasterId : null,
            RequestedItemName = requestedItemName,
            Remarks = Normalize(dto.Remarks),
            Status = DiagnosticRequestStatus.Requested,
            CreatedAt = DateTime.UtcNow
        };

        _db.DiagnosticRequests.Add(request);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<DiagnosticRequestDto>.Ok(new DiagnosticRequestDto
        {
            DiagnosticRequestId = request.DiagnosticRequestId,
            AppointmentId = request.AppointmentId,
            RequestType = request.RequestType.ToString(),
            LabTestMasterId = request.LabTestMasterId,
            RequestedItemName = request.RequestedItemName,
            Remarks = request.Remarks,
            ResultSummary = request.ResultSummary,
            Status = request.Status.ToString(),
            CreatedAt = request.CreatedAt
        }, "Diagnostic request saved"));
    }

    [HttpGet("medicines")]
    public async Task<ActionResult<ApiResponse<IEnumerable<MedicineSearchResultDto>>>> SearchMedicines([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var term = search?.Trim() ?? string.Empty;
        var query = _db.MedicineMasters.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(x =>
                x.MedicineName.Contains(term) ||
                (x.Brand != null && x.Brand.Contains(term)) ||
                (x.Strength != null && x.Strength.Contains(term)));
        }

        var items = await query
            .OrderBy(x => x.MedicineName)
            .ThenBy(x => x.Brand)
            .Take(20)
            .Select(x => new MedicineSearchResultDto
            {
                MedicineMasterId = x.MedicineMasterId,
                MedicineName = x.MedicineName,
                Brand = x.Brand,
                Strength = x.Strength,
                DosageForm = x.GenericName
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IEnumerable<MedicineSearchResultDto>>.Ok(items));
    }

    [HttpGet("lab-tests")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LabTestMasterDto>>>> SearchLabTests([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var term = search?.Trim() ?? string.Empty;
        var query = _db.LabTestMasters.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(x => x.TestName.Contains(term) || x.DepartmentName.Contains(term));
        }

        var items = await query
            .OrderBy(x => x.TestName)
            .Take(25)
            .Select(x => new LabTestMasterDto
            {
                LabTestMasterId = x.LabTestMasterId,
                TestName = x.TestName,
                DepartmentName = x.DepartmentName,
                Price = x.Price,
                SampleType = x.SampleType,
                ReportFormat = x.ReportFormat,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IEnumerable<LabTestMasterDto>>.Ok(items));
    }

    private async Task<Doctor?> GetCurrentDoctorAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var normalizedEmail = user.Email.Trim().ToLowerInvariant();
        return await _db.Doctors.FirstOrDefaultAsync(x => x.Email == normalizedEmail && x.IsActive, cancellationToken);
    }

    private long GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return long.Parse(raw!);
    }

    private async Task<string?> ValidateScheduleAsync(long doctorId, SaveDoctorScheduleDto dto, long? existingScheduleId, CancellationToken cancellationToken)
    {
        if (dto.DayOfWeek is < 1 or > 7)
        {
            return "Select a valid day of the week";
        }

        if (dto.EndTime <= dto.StartTime)
        {
            return "End time must be after start time";
        }

        if (dto.SlotDurationMinutes < 5)
        {
            return "Slot duration must be at least 5 minutes";
        }

        if (dto.MaxPatientsPerSlot < 1)
        {
            return "Max patients per slot must be at least 1";
        }

        var totalMinutes = (dto.EndTime - dto.StartTime).TotalMinutes;
        if (totalMinutes < dto.SlotDurationMinutes || totalMinutes % dto.SlotDurationMinutes != 0)
        {
            return "Schedule window must divide evenly into the slot duration";
        }

        if (!dto.IsActive)
        {
            return null;
        }

        var overlap = await _db.DoctorSchedules.AnyAsync(x =>
            x.DoctorId == doctorId &&
            x.ScheduleId != existingScheduleId &&
            x.IsActive &&
            x.DayOfWeek == dto.DayOfWeek &&
            dto.StartTime < x.EndTime &&
            x.StartTime < dto.EndTime,
            cancellationToken);

        return overlap ? "Schedule overlap detected" : null;
    }

    private static DoctorScheduleDto MapSchedule(DoctorSchedule schedule)
    {
        return new DoctorScheduleDto
        {
            ScheduleId = schedule.ScheduleId,
            DoctorId = schedule.DoctorId,
            DayOfWeek = schedule.DayOfWeek,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            SlotDurationMinutes = schedule.SlotDurationMinutes,
            MaxPatientsPerSlot = schedule.MaxPatientsPerSlot,
            IsActive = schedule.IsActive,
            CreatedAt = schedule.CreatedAt,
            UpdatedAt = schedule.UpdatedAt
        };
    }

    private static DoctorAvailabilityExceptionDto MapException(DoctorAvailabilityException item)
    {
        return new DoctorAvailabilityExceptionDto
        {
            DoctorAvailabilityExceptionId = item.DoctorAvailabilityExceptionId,
            ExceptionType = item.ExceptionType.ToString(),
            StartDate = item.StartDate,
            EndDate = item.EndDate,
            Notes = item.Notes,
            IsActive = item.IsActive
        };
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
