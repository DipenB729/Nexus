using System.Security.Claims;
using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/care-communication")]
[Authorize]
public class CareCommunicationController : ControllerBase
{
    private static readonly AppointmentStatus[] OpenCareStatuses = { AppointmentStatus.Approved, AppointmentStatus.Completed };
    private static readonly HashSet<string> AllowedReportExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public CareCommunicationController(AppDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    [HttpGet("threads")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CareThreadSummaryDto>>>> GetThreads(CancellationToken cancellationToken)
    {
        var participant = await ResolveParticipantAsync(cancellationToken);
        if (participant is null)
        {
            return Unauthorized(ApiResponse<IEnumerable<CareThreadSummaryDto>>.Fail("Session profile not found"));
        }

        var query = BuildThreadQuery(participant);
        var rows = await query
            .OrderByDescending(x => x.AppointmentDate)
            .ThenByDescending(x => x.SlotStartTime)
            .Select(x => new CareThreadSummaryDto
            {
                AppointmentId = x.AppointmentId,
                PatientId = x.PatientId,
                DoctorId = x.DoctorId,
                PatientName = x.PatientName,
                DoctorName = x.DoctorName,
                DoctorSpecialization = x.DoctorSpecialization,
                MedicalRecordNumber = x.MedicalRecordNumber,
                AppointmentDate = x.AppointmentDate,
                SlotStartTime = x.SlotStartTime,
                SlotEndTime = x.SlotEndTime,
                Status = x.Status,
                ServiceName = x.ServiceName
            })
            .ToListAsync(cancellationToken);

        var appointmentIds = rows.Select(x => x.AppointmentId).ToList();
        var lastMessageRows = await _db.CareConversationMessages
            .AsNoTracking()
            .Where(x => appointmentIds.Contains(x.AppointmentId))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.AppointmentId,
                x.Message,
                x.CreatedAt
            })
            .ToListAsync(cancellationToken);
        var lastMessages = lastMessageRows
            .GroupBy(x => x.AppointmentId)
            .ToDictionary(x => x.Key, x => x.First());

        var reportCounts = await _db.PatientCaseReports
            .AsNoTracking()
            .Where(x => appointmentIds.Contains(x.AppointmentId))
            .GroupBy(x => x.AppointmentId)
            .Select(x => new { AppointmentId = x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            if (lastMessages.TryGetValue(row.AppointmentId, out var lastMessage))
            {
                row.LastMessage = lastMessage.Message;
                row.LastMessageAt = lastMessage.CreatedAt;
            }
            row.ReportCount = reportCounts.FirstOrDefault(x => x.AppointmentId == row.AppointmentId)?.Count ?? 0;
        }

        return Ok(ApiResponse<IEnumerable<CareThreadSummaryDto>>.Ok(rows));
    }

    [HttpGet("threads/{appointmentId:long}")]
    public async Task<ActionResult<ApiResponse<CareThreadDetailDto>>> GetThread(long appointmentId, CancellationToken cancellationToken)
    {
        var participant = await ResolveParticipantAsync(cancellationToken);
        if (participant is null)
        {
            return Unauthorized(ApiResponse<CareThreadDetailDto>.Fail("Session profile not found"));
        }

        var thread = await BuildThreadQuery(participant)
            .Where(x => x.AppointmentId == appointmentId)
            .Select(x => new CareThreadSummaryDto
            {
                AppointmentId = x.AppointmentId,
                PatientId = x.PatientId,
                DoctorId = x.DoctorId,
                PatientName = x.PatientName,
                DoctorName = x.DoctorName,
                DoctorSpecialization = x.DoctorSpecialization,
                MedicalRecordNumber = x.MedicalRecordNumber,
                AppointmentDate = x.AppointmentDate,
                SlotStartTime = x.SlotStartTime,
                SlotEndTime = x.SlotEndTime,
                Status = x.Status,
                ServiceName = x.ServiceName
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (thread is null)
        {
            return NotFound(ApiResponse<CareThreadDetailDto>.Fail("Approved appointment thread not found"));
        }

        var messages = await _db.CareConversationMessages
            .AsNoTracking()
            .Where(x => x.AppointmentId == appointmentId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new CareConversationMessageDto
            {
                CareConversationMessageId = x.CareConversationMessageId,
                AppointmentId = x.AppointmentId,
                SenderRole = x.SenderRole,
                SenderName = x.SenderName,
                Message = x.Message,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var reports = await _db.PatientCaseReports
            .AsNoTracking()
            .Where(x => x.AppointmentId == appointmentId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PatientCaseReportDto
            {
                PatientCaseReportId = x.PatientCaseReportId,
                AppointmentId = x.AppointmentId,
                Symptoms = x.Symptoms,
                PreviousReportSummary = x.PreviousReportSummary,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                Document = x.PatientDocument == null ? null : new PatientDocumentDto
                {
                    PatientDocumentId = x.PatientDocument.PatientDocumentId,
                    Category = x.PatientDocument.Category,
                    Title = x.PatientDocument.Title,
                    FileUrl = x.PatientDocument.FileUrl,
                    Notes = x.PatientDocument.Notes,
                    UploadedByRole = x.PatientDocument.UploadedByRole,
                    UploadedAt = x.PatientDocument.UploadedAt
                }
            })
            .ToListAsync(cancellationToken);

        var consultation = await _db.DoctorConsultations
            .AsNoTracking()
            .Where(x => x.AppointmentId == appointmentId)
            .Select(x => new DoctorConsultationDto
            {
                DoctorConsultationId = x.DoctorConsultationId,
                Symptoms = x.Symptoms,
                Diagnosis = x.Diagnosis,
                Notes = x.Notes,
                VitalObservations = x.VitalObservations,
                Advice = x.Advice,
                FollowUpDate = x.FollowUpDate,
                Status = x.Status.ToString(),
                UpdatedAt = x.UpdatedAt ?? x.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken) ?? new DoctorConsultationDto();

        var prescriptions = await _db.DoctorPrescriptions
            .AsNoTracking()
            .Where(x => x.AppointmentId == appointmentId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new DoctorPrescriptionDto
            {
                DoctorPrescriptionId = x.DoctorPrescriptionId,
                AppointmentId = x.AppointmentId,
                CreatedAt = x.CreatedAt,
                Notes = x.Notes
            })
            .ToListAsync(cancellationToken);

        var prescriptionIds = prescriptions.Select(x => x.DoctorPrescriptionId).ToList();
        var prescriptionItems = await _db.DoctorPrescriptionItems
            .AsNoTracking()
            .Where(x => prescriptionIds.Contains(x.DoctorPrescriptionId))
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        foreach (var prescription in prescriptions)
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
            .Where(x => x.AppointmentId == appointmentId)
            .OrderByDescending(x => x.CreatedAt)
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

        thread.LastMessage = messages.LastOrDefault()?.Message;
        thread.LastMessageAt = messages.LastOrDefault()?.CreatedAt;
        thread.ReportCount = reports.Count;

        return Ok(ApiResponse<CareThreadDetailDto>.Ok(new CareThreadDetailDto
        {
            Thread = thread,
            Messages = messages,
            Reports = reports,
            Consultation = consultation,
            Prescriptions = prescriptions,
            DiagnosticRequests = diagnosticRequests
        }));
    }

    [HttpPost("threads/{appointmentId:long}/messages")]
    public async Task<ActionResult<ApiResponse<CareConversationMessageDto>>> SendMessage(long appointmentId, [FromBody] SaveCareConversationMessageDto dto, CancellationToken cancellationToken)
    {
        var participant = await ResolveParticipantAsync(cancellationToken);
        if (participant is null)
        {
            return Unauthorized(ApiResponse<CareConversationMessageDto>.Fail("Session profile not found"));
        }

        var appointment = await ResolveThreadAppointmentAsync(appointmentId, participant, cancellationToken);
        if (appointment is null)
        {
            return NotFound(ApiResponse<CareConversationMessageDto>.Fail("Approved appointment thread not found"));
        }

        var messageText = Normalize(dto.Message);
        if (messageText is null)
        {
            return BadRequest(ApiResponse<CareConversationMessageDto>.Fail("Message is required"));
        }

        var message = new CareConversationMessage
        {
            AppointmentId = appointment.AppointmentId,
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId,
            SenderUserId = participant.UserId,
            SenderRole = participant.Role,
            SenderName = participant.DisplayName,
            Message = messageText,
            CreatedAt = DateTime.UtcNow
        };

        _db.CareConversationMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<CareConversationMessageDto>.Ok(new CareConversationMessageDto
        {
            CareConversationMessageId = message.CareConversationMessageId,
            AppointmentId = message.AppointmentId,
            SenderRole = message.SenderRole,
            SenderName = message.SenderName,
            Message = message.Message,
            CreatedAt = message.CreatedAt
        }, "Message sent"));
    }

    [HttpPost("threads/{appointmentId:long}/reports")]
    [Authorize(Policy = "UserOnly")]
    public async Task<ActionResult<ApiResponse<PatientCaseReportDto>>> SubmitReport(long appointmentId, [FromBody] SavePatientCaseReportDto dto, CancellationToken cancellationToken)
    {
        return await SavePatientReportAsync(appointmentId, dto, null, cancellationToken);
    }

    [HttpPost("threads/{appointmentId:long}/reports/upload")]
    [Authorize(Policy = "UserOnly")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<ApiResponse<PatientCaseReportDto>>> SubmitReportWithAttachment(long appointmentId, [FromForm] SavePatientCaseReportFormDto dto, CancellationToken cancellationToken)
    {
        var reportUrl = Normalize(dto.ReportUrl);
        if (dto.Attachment is not null)
        {
            var uploadedUrl = await SaveReportAttachmentAsync(dto.Attachment, cancellationToken);
            if (uploadedUrl is null)
            {
                return BadRequest(ApiResponse<PatientCaseReportDto>.Fail("Only JPG, PNG, WebP, and PDF report files up to 10 MB are supported"));
            }

            reportUrl = uploadedUrl;
        }

        return await SavePatientReportAsync(appointmentId, new SavePatientCaseReportDto
        {
            Symptoms = dto.Symptoms,
            PreviousReportSummary = dto.PreviousReportSummary,
            ReportTitle = dto.ReportTitle,
            ReportUrl = reportUrl,
            ReportCategory = dto.ReportCategory,
            ReportNotes = dto.ReportNotes
        }, null, cancellationToken);
    }

    private async Task<ActionResult<ApiResponse<PatientCaseReportDto>>> SavePatientReportAsync(long appointmentId, SavePatientCaseReportDto dto, PatientDocument? _, CancellationToken cancellationToken)
    {
        var participant = await ResolveParticipantAsync(cancellationToken);
        if (participant is null || participant.PatientId is null)
        {
            return Unauthorized(ApiResponse<PatientCaseReportDto>.Fail("Patient session not found"));
        }

        var appointment = await ResolveThreadAppointmentAsync(appointmentId, participant, cancellationToken);
        if (appointment is null)
        {
            return NotFound(ApiResponse<PatientCaseReportDto>.Fail("Approved appointment thread not found"));
        }

        var symptoms = Normalize(dto.Symptoms);
        if (symptoms is null)
        {
            return BadRequest(ApiResponse<PatientCaseReportDto>.Fail("Symptoms are required"));
        }

        PatientDocument? document = null;
        var reportUrl = Normalize(dto.ReportUrl);
        var reportTitle = Normalize(dto.ReportTitle);
        if (reportUrl is not null)
        {
            document = new PatientDocument
            {
                PatientId = appointment.PatientId,
                AppointmentId = appointment.AppointmentId,
                Category = Normalize(dto.ReportCategory) ?? "Previous report",
                Title = reportTitle ?? "Previous report",
                FileUrl = reportUrl,
                Notes = Normalize(dto.ReportNotes),
                UploadedByRole = "Patient",
                UploadedAt = DateTime.UtcNow
            };
            _db.PatientDocuments.Add(document);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var report = new PatientCaseReport
        {
            AppointmentId = appointment.AppointmentId,
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId,
            Symptoms = symptoms,
            PreviousReportSummary = Normalize(dto.PreviousReportSummary),
            PatientDocumentId = document?.PatientDocumentId,
            CreatedAt = DateTime.UtcNow
        };

        _db.PatientCaseReports.Add(report);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<PatientCaseReportDto>.Ok(new PatientCaseReportDto
        {
            PatientCaseReportId = report.PatientCaseReportId,
            AppointmentId = report.AppointmentId,
            Symptoms = report.Symptoms,
            PreviousReportSummary = report.PreviousReportSummary,
            CreatedAt = report.CreatedAt,
            Document = document is null ? null : new PatientDocumentDto
            {
                PatientDocumentId = document.PatientDocumentId,
                Category = document.Category,
                Title = document.Title,
                FileUrl = document.FileUrl,
                Notes = document.Notes,
                UploadedByRole = document.UploadedByRole,
                UploadedAt = document.UploadedAt
            }
        }, "Report submitted"));
    }

    private async Task<string?> SaveReportAttachmentAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0 || file.Length > 10_000_000)
        {
            return null;
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedReportExtensions.Contains(extension))
        {
            return null;
        }

        var webRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        }

        var uploadsRoot = Path.Combine(webRoot, "uploads", "patient-reports");
        Directory.CreateDirectory(uploadsRoot);
        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var absolutePath = Path.Combine(uploadsRoot, fileName);
        await using var stream = System.IO.File.Create(absolutePath);
        await file.CopyToAsync(stream, cancellationToken);

        return $"/uploads/patient-reports/{fileName}";
    }

    private async Task<CareParticipant?> ResolveParticipantAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var user = await _db.Users.AsNoTracking().Include(x => x.Role).FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive, cancellationToken);
        if (user is null || user.Role is null)
        {
            return null;
        }

        if (string.Equals(user.Role.Name, "Doctor", StringComparison.OrdinalIgnoreCase))
        {
            var email = user.Email.Trim().ToLowerInvariant();
            var doctor = await _db.Doctors.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email && x.IsActive, cancellationToken);
            return doctor is null ? null : new CareParticipant(user.UserId, "Doctor", user.FullName, null, doctor.DoctorId);
        }

        if (string.Equals(user.Role.Name, "User", StringComparison.OrdinalIgnoreCase))
        {
            var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == user.UserId && x.IsActive, cancellationToken);
            return patient is null ? null : new CareParticipant(user.UserId, "Patient", user.FullName, patient.PatientId, null);
        }

        return null;
    }

    private IQueryable<CareThreadProjection> BuildThreadQuery(CareParticipant participant)
    {
        var query = from appointment in _db.Appointments.AsNoTracking()
                    join patient in _db.Patients.AsNoTracking() on appointment.PatientId equals patient.PatientId
                    join patientUser in _db.Users.AsNoTracking() on patient.UserId equals patientUser.UserId
                    join doctor in _db.Doctors.AsNoTracking() on appointment.DoctorId equals doctor.DoctorId
                    join service in _db.Services.AsNoTracking() on appointment.ServiceId equals service.Id into serviceGroup
                    from service in serviceGroup.DefaultIfEmpty()
                    where OpenCareStatuses.Contains(appointment.Status)
                    select new CareThreadProjection
                    {
                        AppointmentId = appointment.AppointmentId,
                        PatientId = appointment.PatientId,
                        DoctorId = appointment.DoctorId,
                        PatientName = patientUser.FullName,
                        DoctorName = doctor.FullName,
                        DoctorSpecialization = doctor.Specialization,
                        MedicalRecordNumber = patient.MedicalRecordNumber,
                        AppointmentDate = appointment.AppointmentDate,
                        SlotStartTime = appointment.SlotStartTime,
                        SlotEndTime = appointment.SlotEndTime,
                        Status = appointment.Status.ToString(),
                        ServiceName = service != null ? service.Name : null
                    };

        return participant.Role == "Doctor"
            ? query.Where(x => x.DoctorId == participant.DoctorId)
            : query.Where(x => x.PatientId == participant.PatientId);
    }

    private Task<Appointment?> ResolveThreadAppointmentAsync(long appointmentId, CareParticipant participant, CancellationToken cancellationToken)
    {
        var query = _db.Appointments.Where(x => x.AppointmentId == appointmentId && OpenCareStatuses.Contains(x.Status));
        query = participant.Role == "Doctor"
            ? query.Where(x => x.DoctorId == participant.DoctorId)
            : query.Where(x => x.PatientId == participant.PatientId);

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    private long GetUserId() => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record CareParticipant(long UserId, string Role, string DisplayName, long? PatientId, long? DoctorId);

    private sealed class CareThreadProjection
    {
        public long AppointmentId { get; init; }
        public long PatientId { get; init; }
        public long DoctorId { get; init; }
        public string PatientName { get; init; } = string.Empty;
        public string DoctorName { get; init; } = string.Empty;
        public string? DoctorSpecialization { get; init; }
        public string? MedicalRecordNumber { get; init; }
        public DateTime AppointmentDate { get; init; }
        public TimeSpan SlotStartTime { get; init; }
        public TimeSpan SlotEndTime { get; init; }
        public string Status { get; init; } = string.Empty;
        public string? ServiceName { get; init; }
    }
}
