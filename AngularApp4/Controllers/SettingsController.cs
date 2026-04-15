using System.Security.Claims;
using System.Text.Json;
using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using AngularApp4.Services.Hms;
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
    private readonly IAuditLogService _audit;

    public SettingsController(AppDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet("control-center")]
    public async Task<ActionResult<ApiResponse<ControlCenterSettingsDto>>> GetControlCenter(CancellationToken cancellationToken)
    {
        var payload = await BuildControlCenterAsync(cancellationToken);
        return Ok(ApiResponse<ControlCenterSettingsDto>.Ok(payload));
    }

    [HttpGet("organization")]
    public async Task<ActionResult<ApiResponse<OrganizationSettingsDto>>> GetOrganizationSettings(CancellationToken cancellationToken)
    {
        var payload = await BuildOrganizationAsync(cancellationToken);
        return Ok(ApiResponse<OrganizationSettingsDto>.Ok(payload));
    }

    [HttpPut("organization")]
    public async Task<ActionResult<ApiResponse<OrganizationSettingsDto>>> UpdateOrganizationSettings([FromBody] OrganizationSettingsDto dto, CancellationToken cancellationToken)
    {
        var profile = await _db.HospitalProfiles.OrderBy(x => x.HospitalProfileId).FirstOrDefaultAsync(cancellationToken);
        if (profile is null)
        {
            profile = new HospitalProfile();
            _db.HospitalProfiles.Add(profile);
        }

        ApplyProfile(profile, dto.Profile);
        profile.UpdatedAt = DateTime.UtcNow;

        var existingBranches = await _db.Branches.OrderBy(x => x.BranchId).ToListAsync(cancellationToken);
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
                branch = new Branch { CreatedAt = DateTime.UtcNow };
                _db.Branches.Add(branch);
                existingBranches.Add(branch);
            }

            ApplyBranch(branch, branchDto, primaryIndex >= 0 && primaryIndex == index);
        }

        var savedPrimary = existingBranches.FirstOrDefault(x => x.IsPrimary);
        if (savedPrimary is null && existingBranches.Count > 0)
        {
            existingBranches[0].IsPrimary = true;
        }

        await SyncSystemSettingsFromProfileAsync(profile, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.Administration,
            Action = "Updated",
            EntityName = "Organization Settings",
            TargetDisplayName = profile.HospitalName,
            Summary = $"Organization settings were updated for {profile.HospitalName} across {existingBranches.Count} branches.",
            Metadata = new { profile.HospitalName, BranchCount = existingBranches.Count, profile.CurrencyCode, profile.InvoicePrefix }
        }, cancellationToken);

        await TryCreateAutoBackupAsync("organization settings update", cancellationToken);

        var response = await BuildOrganizationAsync(cancellationToken);
        return Ok(ApiResponse<OrganizationSettingsDto>.Ok(response, "Organization settings updated"));
    }

    [HttpPut("notifications")]
    public async Task<ActionResult<ApiResponse<NotificationSettingsDto>>> UpdateNotificationSettings([FromBody] NotificationSettingsDto dto, CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateNotificationSettingsAsync(cancellationToken);
        ApplyNotificationSettings(settings, dto);
        settings.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.Administration,
            Action = "Updated",
            EntityName = "Notification Settings",
            TargetDisplayName = "Control Center Notifications",
            Summary = "Notification alert thresholds and reminder channels were updated.",
            Metadata = new
            {
                settings.LowStockAlertsEnabled,
                settings.ExpiryAlertsEnabled,
                settings.AppointmentRemindersEnabled,
                settings.PaymentDueAlertsEnabled
            }
        }, cancellationToken);

        await TryCreateAutoBackupAsync("notification settings update", cancellationToken);

        var response = await BuildNotificationSettingsAsync(cancellationToken);
        return Ok(ApiResponse<NotificationSettingsDto>.Ok(response, "Notification settings updated"));
    }

    [HttpPut("system")]
    public async Task<ActionResult<ApiResponse<SystemSettingsDto>>> UpdateSystemSettings([FromBody] SystemSettingsDto dto, CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateSystemSettingsAsync(cancellationToken);
        ApplySystemSettings(settings, dto);
        settings.UpdatedAt = DateTime.UtcNow;

        var profile = await _db.HospitalProfiles.OrderBy(x => x.HospitalProfileId).FirstOrDefaultAsync(cancellationToken);
        if (profile is not null)
        {
            profile.CurrencyCode = settings.DefaultCurrencyCode;
            profile.InvoicePrefix = settings.InvoicePrefix;
            profile.InvoiceStartingNumber = settings.NextInvoiceNumber;
            profile.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.Administration,
            Action = "Updated",
            EntityName = "System Settings",
            TargetDisplayName = "Control Center System",
            Summary = $"System settings were updated with time zone {settings.TimeZoneId} and currency {settings.DefaultCurrencyCode}.",
            Metadata = new { settings.DefaultCurrencyCode, settings.TimeZoneId, settings.InvoicePrefix, settings.NextInvoiceNumber, settings.AutoBackupEnabled }
        }, cancellationToken);

        await TryCreateAutoBackupAsync("system settings update", cancellationToken);

        return Ok(ApiResponse<SystemSettingsDto>.Ok(MapSystemSettings(settings), "System settings updated"));
    }

    [HttpPut("security")]
    public async Task<ActionResult<ApiResponse<SecurityCenterDto>>> UpdateSecuritySettings([FromBody] SecuritySettingsDto dto, CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateSecuritySettingsAsync(cancellationToken);
        ApplySecuritySettings(settings, dto);
        settings.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.Administration,
            Action = "Updated",
            EntityName = "Security Settings",
            TargetDisplayName = "Control Center Security",
            Summary = $"Security settings were updated with a {settings.SessionTimeoutMinutes}-minute session timeout and minimum password length {settings.MinPasswordLength}.",
            Metadata = new { settings.SessionTimeoutMinutes, settings.MinPasswordLength, settings.RequireUppercase, settings.RequireLowercase, settings.RequireDigit, settings.RequireSpecialCharacter }
        }, cancellationToken);

        await TryCreateAutoBackupAsync("security settings update", cancellationToken);

        var response = await BuildSecurityCenterAsync(cancellationToken);
        return Ok(ApiResponse<SecurityCenterDto>.Ok(response, "Security settings updated"));
    }

    [HttpPost("security/permission-review")]
    public async Task<ActionResult<ApiResponse<PermissionReviewSummaryDto>>> MarkPermissionReview(CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateSecuritySettingsAsync(cancellationToken);
        settings.LastPermissionReviewAt = DateTime.UtcNow;
        settings.LastPermissionReviewedByName = GetCurrentActorName();
        settings.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.Administration,
            Action = "PermissionReviewCompleted",
            EntityName = "Permission Review",
            TargetDisplayName = "Role permissions",
            Summary = $"{GetCurrentActorName()} completed a permission review.",
            PerformedByUserId = GetCurrentActorId(),
            PerformedByName = GetCurrentActorName(),
            PerformedByRole = GetCurrentActorRole(),
            ActorEmail = GetCurrentActorEmail()
        }, cancellationToken);

        var response = await BuildPermissionReviewSummaryAsync(settings, cancellationToken);
        return Ok(ApiResponse<PermissionReviewSummaryDto>.Ok(response, "Permission review marked as completed"));
    }

    [HttpPost("backups/manual")]
    public async Task<ActionResult<ApiResponse<BackupCenterDto>>> CreateManualBackup([FromBody] CreateBackupRequestDto? dto, CancellationToken cancellationToken)
    {
        await CreateBackupLogAsync("Manual", dto?.BackupName, "Manual configuration backup created from admin settings.", cancellationToken);
        await _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.Administration,
            Action = "ManualBackupCreated",
            EntityName = "Backup",
            TargetDisplayName = dto?.BackupName,
            Summary = $"{GetCurrentActorName()} created a manual control-settings backup.",
            PerformedByUserId = GetCurrentActorId(),
            PerformedByName = GetCurrentActorName(),
            PerformedByRole = GetCurrentActorRole(),
            ActorEmail = GetCurrentActorEmail()
        }, cancellationToken);

        var response = await BuildBackupCenterAsync(cancellationToken);
        return Ok(ApiResponse<BackupCenterDto>.Ok(response, "Manual backup created"));
    }

    [HttpPost("backups/{backupLogId:long}/restore")]
    public async Task<ActionResult<ApiResponse<BackupCenterDto>>> RestoreBackup(long backupLogId, CancellationToken cancellationToken)
    {
        var backup = await _db.BackupLogs.FirstOrDefaultAsync(x => x.BackupLogId == backupLogId, cancellationToken);
        if (backup is null || string.IsNullOrWhiteSpace(backup.SnapshotJson))
        {
            return NotFound(ApiResponse<BackupCenterDto>.Fail("Backup snapshot not found"));
        }

        var snapshot = JsonSerializer.Deserialize<ControlSettingsBackupSnapshot>(backup.SnapshotJson);
        if (snapshot is null)
        {
            return BadRequest(ApiResponse<BackupCenterDto>.Fail("Backup snapshot is invalid"));
        }

        await ApplyOrganizationSnapshotAsync(snapshot.Organization, cancellationToken);

        var notificationSettings = await GetOrCreateNotificationSettingsAsync(cancellationToken);
        ApplyNotificationSettings(notificationSettings, snapshot.Notifications);
        notificationSettings.UpdatedAt = DateTime.UtcNow;

        var systemSettings = await GetOrCreateSystemSettingsAsync(cancellationToken);
        ApplySystemSettings(systemSettings, snapshot.System);
        systemSettings.UpdatedAt = DateTime.UtcNow;

        var securitySettings = await GetOrCreateSecuritySettingsAsync(cancellationToken);
        ApplySecuritySettings(securitySettings, snapshot.Security);
        securitySettings.LastPermissionReviewAt = snapshot.Security.LastPermissionReviewAt;
        securitySettings.LastPermissionReviewedByName = snapshot.Security.LastPermissionReviewedByName;
        securitySettings.UpdatedAt = DateTime.UtcNow;

        backup.RestoredAt = DateTime.UtcNow;
        backup.RestoredByName = GetCurrentActorName();

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.Administration,
            Action = "BackupRestored",
            EntityName = "Backup",
            EntityId = backup.BackupLogId,
            TargetDisplayName = backup.BackupName,
            Summary = $"{GetCurrentActorName()} restored settings from backup {backup.BackupName}.",
            PerformedByUserId = GetCurrentActorId(),
            PerformedByName = GetCurrentActorName(),
            PerformedByRole = GetCurrentActorRole(),
            ActorEmail = GetCurrentActorEmail()
        }, cancellationToken);

        var response = await BuildBackupCenterAsync(cancellationToken);
        return Ok(ApiResponse<BackupCenterDto>.Ok(response, "Backup restored successfully"));
    }

    private async Task<ControlCenterSettingsDto> BuildControlCenterAsync(CancellationToken cancellationToken)
    {
        return new ControlCenterSettingsDto
        {
            Organization = await BuildOrganizationAsync(cancellationToken),
            Notifications = await BuildNotificationSettingsAsync(cancellationToken),
            System = MapSystemSettings(await GetOrCreateSystemSettingsAsync(cancellationToken)),
            Backups = await BuildBackupCenterAsync(cancellationToken),
            Security = await BuildSecurityCenterAsync(cancellationToken)
        };
    }

    private async Task<OrganizationSettingsDto> BuildOrganizationAsync(CancellationToken cancellationToken)
    {
        var profile = await _db.HospitalProfiles.AsNoTracking().OrderBy(x => x.HospitalProfileId).FirstOrDefaultAsync(cancellationToken);
        var branches = await _db.Branches.AsNoTracking().OrderByDescending(x => x.IsPrimary).ThenBy(x => x.Name).ToListAsync(cancellationToken);

        return new OrganizationSettingsDto
        {
            Profile = profile is null ? new HospitalProfileDto() : MapProfile(profile),
            Branches = branches.Select(MapBranch).ToList()
        };
    }

    private async Task<NotificationSettingsDto> BuildNotificationSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateNotificationSettingsAsync(cancellationToken);
        var response = MapNotificationSettings(settings);
        response.Preview = await BuildNotificationPreviewAsync(settings, cancellationToken);
        return response;
    }

    private async Task<NotificationPreviewDto> BuildNotificationPreviewAsync(NotificationSetting settings, CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var now = DateTime.Now;

        var stockBatchQuery =
            from batch in _db.StockBatches.AsNoTracking()
            join location in _db.StockLocations.AsNoTracking() on batch.StockLocationId equals location.StockLocationId
            join branchRow in _db.Branches.AsNoTracking() on location.BranchId equals branchRow.BranchId into branchJoin
            from branch in branchJoin.DefaultIfEmpty()
            join medicineRow in _db.MedicineMasters.AsNoTracking() on batch.MedicineMasterId equals medicineRow.MedicineMasterId into medicineJoin
            from medicine in medicineJoin.DefaultIfEmpty()
            join itemRow in _db.StockItemMasters.AsNoTracking() on batch.StockItemMasterId equals itemRow.StockItemMasterId into itemJoin
            from item in itemJoin.DefaultIfEmpty()
            where batch.QuantityOnHand > 0
            select new
            {
                batch.StockBatchId,
                Name = medicine != null ? medicine.MedicineName : item != null ? item.ItemName : "Unknown item",
                BranchName = branch != null ? branch.Name : location.Name,
                QuantityOnHand = batch.QuantityOnHand,
                ReorderLevel = medicine != null ? medicine.MinimumStock : item != null ? item.MinimumStock : 0m,
                batch.ExpiryDate
            };

        var lowStockQuery = stockBatchQuery
            .Where(x => x.QuantityOnHand <= x.ReorderLevel && (!x.ExpiryDate.HasValue || x.ExpiryDate.Value.Date >= today));
        var expiryQuery = stockBatchQuery
            .Where(x => x.ExpiryDate.HasValue && x.ExpiryDate.Value.Date <= today.AddDays(settings.ExpiryAlertDays));

        var lowStockAlerts = await lowStockQuery
            .OrderBy(x => x.QuantityOnHand)
            .Take(5)
            .Select(x => new StockAlertDto
            {
                ItemId = x.StockBatchId,
                Name = x.Name,
                BranchName = x.BranchName,
                QuantityInStock = (int)Math.Round(x.QuantityOnHand, MidpointRounding.AwayFromZero),
                ReorderLevel = (int)Math.Round(x.ReorderLevel, MidpointRounding.AwayFromZero),
                ExpiryDate = x.ExpiryDate ?? today
            })
            .ToListAsync(cancellationToken);

        var expiryAlerts = await expiryQuery
            .OrderBy(x => x.ExpiryDate)
            .Take(5)
            .Select(x => new StockAlertDto
            {
                ItemId = x.StockBatchId,
                Name = x.Name,
                BranchName = x.BranchName,
                QuantityInStock = (int)Math.Round(x.QuantityOnHand, MidpointRounding.AwayFromZero),
                ReorderLevel = (int)Math.Round(x.ReorderLevel, MidpointRounding.AwayFromZero),
                ExpiryDate = x.ExpiryDate ?? today
            })
            .ToListAsync(cancellationToken);

        var reminderLookAheadDays = Math.Max((int)Math.Ceiling(Math.Max(settings.AppointmentReminderHoursBefore, 1) / 24d), 1) + 1;
        var appointmentRows = await _db.Appointments
            .AsNoTracking()
            .Where(x => x.AppointmentDate >= today && x.AppointmentDate <= today.AddDays(reminderLookAheadDays))
            .Where(x => x.Status == AppointmentStatus.Pending || x.Status == AppointmentStatus.Approved || x.Status == AppointmentStatus.Rescheduled)
            .OrderBy(x => x.AppointmentDate)
            .ThenBy(x => x.SlotStartTime)
            .ToListAsync(cancellationToken);

        var windowEnd = now.AddHours(Math.Max(settings.AppointmentReminderHoursBefore, 1));
        var relevantAppointments = appointmentRows
            .Where(x =>
            {
                var appointmentAt = x.AppointmentDate.Date + x.SlotStartTime;
                return appointmentAt >= now && appointmentAt <= windowEnd;
            })
            .ToList();

        var appointmentPatientIds = relevantAppointments.Select(x => x.PatientId).Distinct().ToList();
        var patients = await _db.Patients.AsNoTracking()
            .Where(x => appointmentPatientIds.Contains(x.PatientId))
            .ToDictionaryAsync(x => x.PatientId, cancellationToken);
        var patientUsers = await _db.Users.AsNoTracking()
            .Where(x => patients.Values.Select(y => y.UserId).Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId, cancellationToken);
        var doctors = await _db.Doctors.AsNoTracking()
            .Where(x => relevantAppointments.Select(y => y.DoctorId).Contains(x.DoctorId))
            .ToDictionaryAsync(x => x.DoctorId, cancellationToken);

        var appointmentReminders = relevantAppointments
            .Take(5)
            .Select(x => new AppointmentReminderPreviewDto
            {
                AppointmentId = x.AppointmentId,
                PatientName = patients.TryGetValue(x.PatientId, out var patient) && patientUsers.TryGetValue(patient.UserId, out var user)
                    ? user.FullName
                    : $"Patient #{x.PatientId}",
                DoctorName = doctors.TryGetValue(x.DoctorId, out var doctor) ? doctor.FullName : $"Doctor #{x.DoctorId}",
                AppointmentDate = x.AppointmentDate.Date + x.SlotStartTime,
                TimeSlot = $"{x.SlotStartTime:hh\\:mm} - {x.SlotEndTime:hh\\:mm}",
                Status = x.Status.ToString()
            })
            .ToList();

        var paymentThresholdDate = today.AddDays(Math.Max(settings.PaymentDueReminderDaysBefore, 0));
        var pendingInvoiceRows = await _db.BillingInvoices
            .AsNoTracking()
            .Where(x => (x.Status == InvoiceStatus.Pending || x.Status == InvoiceStatus.Partial) && x.DueDate.HasValue && x.DueDate.Value.Date <= paymentThresholdDate)
            .OrderBy(x => x.DueDate)
            .ThenBy(x => x.InvoiceDate)
            .ToListAsync(cancellationToken);

        var pendingPatientIds = pendingInvoiceRows.Where(x => x.PatientId.HasValue).Select(x => x.PatientId!.Value).Distinct().ToList();
        var pendingPatients = await _db.Patients.AsNoTracking()
            .Where(x => pendingPatientIds.Contains(x.PatientId))
            .ToDictionaryAsync(x => x.PatientId, cancellationToken);
        var pendingUsers = await _db.Users.AsNoTracking()
            .Where(x => pendingPatients.Values.Select(y => y.UserId).Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId, cancellationToken);

        var paymentDueAlerts = pendingInvoiceRows
            .Take(5)
            .Select(invoice =>
            {
                var patientName = "Unknown patient";
                if (invoice.PatientId.HasValue &&
                    pendingPatients.TryGetValue(invoice.PatientId.Value, out var patientRow) &&
                    pendingUsers.TryGetValue(patientRow.UserId, out var userRow))
                {
                    patientName = userRow.FullName;
                }

                return new PendingPaymentDto
                {
                    InvoiceId = invoice.BillingInvoiceId,
                    InvoiceNumber = invoice.InvoiceNumber,
                    PatientName = patientName,
                    DueAmount = Math.Max(invoice.TotalAmount - invoice.ApprovedDiscountAmount - invoice.AmountPaid, 0m),
                    DueDate = invoice.DueDate,
                    Status = invoice.Status.ToString()
                };
            })
            .ToList();

        return new NotificationPreviewDto
        {
            LowStockCount = await lowStockQuery.CountAsync(cancellationToken),
            NearExpiryCount = await expiryQuery.CountAsync(cancellationToken),
            AppointmentReminderCount = relevantAppointments.Count,
            PaymentDueCount = pendingInvoiceRows.Count,
            LowStockAlerts = lowStockAlerts,
            ExpiryAlerts = expiryAlerts,
            AppointmentReminders = appointmentReminders,
            PaymentDueAlerts = paymentDueAlerts
        };
    }

    private async Task<BackupCenterDto> BuildBackupCenterAsync(CancellationToken cancellationToken)
    {
        var logs = await _db.BackupLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        return new BackupCenterDto
        {
            LastBackupAt = logs.FirstOrDefault()?.CreatedAt,
            LastRestoreAt = logs.Where(x => x.RestoredAt.HasValue).OrderByDescending(x => x.RestoredAt).Select(x => x.RestoredAt).FirstOrDefault(),
            Logs = logs.Select(MapBackupLog).ToList()
        };
    }

    private async Task<SecurityCenterDto> BuildSecurityCenterAsync(CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateSecuritySettingsAsync(cancellationToken);
        return new SecurityCenterDto
        {
            Settings = MapSecuritySettings(settings),
            PermissionReview = await BuildPermissionReviewSummaryAsync(settings, cancellationToken),
            DeviceLogs = await BuildSecurityDeviceLogsAsync(cancellationToken)
        };
    }

    private async Task<PermissionReviewSummaryDto> BuildPermissionReviewSummaryAsync(SecuritySetting settings, CancellationToken cancellationToken)
    {
        var roles = await _db.Roles.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var permissions = await _db.RolePermissions.AsNoTracking().ToListAsync(cancellationToken);
        var users = await _db.Users.AsNoTracking().ToListAsync(cancellationToken);

        return new PermissionReviewSummaryDto
        {
            TotalRoles = roles.Count,
            ActiveRoles = roles.Count(x => x.IsActive),
            TotalUsers = users.Count,
            LastReviewedAt = settings.LastPermissionReviewAt,
            LastReviewedByName = settings.LastPermissionReviewedByName,
            NextReviewDueAt = settings.LastPermissionReviewAt?.AddDays(settings.PermissionReviewIntervalDays),
            Roles = roles.Select(role =>
            {
                var rolePermissions = permissions.Where(x => x.RoleId == role.RoleId).ToList();
                return new PermissionReviewRoleDto
                {
                    RoleId = role.RoleId,
                    RoleName = role.Name,
                    IsActive = role.IsActive,
                    UserCount = users.Count(x => x.RoleId == role.RoleId && x.IsActive),
                    ModulesWithView = rolePermissions.Count(x => x.CanView),
                    ModulesWithEdit = rolePermissions.Count(x => x.CanEdit),
                    ModulesWithDelete = rolePermissions.Count(x => x.CanDelete)
                };
            }).ToList()
        };
    }

    private async Task<List<SecurityDeviceLogDto>> BuildSecurityDeviceLogsAsync(CancellationToken cancellationToken)
    {
        return await _db.AuditLogEntries
            .AsNoTracking()
            .Where(x => x.Category == AuditLogCategories.Authentication)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.AuditLogEntryId)
            .Take(20)
            .Select(x => new SecurityDeviceLogDto
            {
                AuditLogEntryId = x.AuditLogEntryId,
                Action = x.Action,
                ActorName = x.PerformedByName ?? x.ActorEmail ?? "Unknown actor",
                ActorEmail = x.ActorEmail,
                ActorRole = x.PerformedByRole,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    private async Task CreateBackupLogAsync(string backupType, string? backupName, string summary, CancellationToken cancellationToken)
    {
        var snapshot = await BuildBackupSnapshotAsync(cancellationToken);
        var payload = JsonSerializer.Serialize(snapshot);

        _db.BackupLogs.Add(new BackupLog
        {
            BackupName = string.IsNullOrWhiteSpace(backupName)
                ? $"{backupType} Backup {DateTime.UtcNow:yyyy-MM-dd HH:mm}"
                : backupName.Trim(),
            BackupType = backupType,
            Status = "Completed",
            Summary = summary,
            SnapshotJson = payload,
            TriggeredByUserId = GetCurrentActorId(),
            TriggeredByName = GetCurrentActorName(),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        if (string.Equals(backupType, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            await TrimAutoBackupsAsync(cancellationToken);
        }
    }

    private async Task TryCreateAutoBackupAsync(string reason, CancellationToken cancellationToken)
    {
        var systemSettings = await _db.SystemControlSettings
            .AsNoTracking()
            .OrderBy(x => x.SystemControlSettingId)
            .FirstOrDefaultAsync(cancellationToken);

        if (systemSettings?.AutoBackupEnabled != true)
        {
            return;
        }

        await CreateBackupLogAsync("Auto", null, $"Automatic backup created after {reason}.", cancellationToken);
    }

    private async Task TrimAutoBackupsAsync(CancellationToken cancellationToken)
    {
        var retention = await _db.SystemControlSettings
            .AsNoTracking()
            .OrderBy(x => x.SystemControlSettingId)
            .Select(x => (int?)x.BackupRetentionCount)
            .FirstOrDefaultAsync(cancellationToken)
            ?? 10;

        var staleLogs = await _db.BackupLogs
            .Where(x => x.BackupType == "Auto")
            .OrderByDescending(x => x.CreatedAt)
            .Skip(Math.Max(retention, 1))
            .ToListAsync(cancellationToken);

        if (staleLogs.Count == 0)
        {
            return;
        }

        _db.BackupLogs.RemoveRange(staleLogs);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<ControlSettingsBackupSnapshot> BuildBackupSnapshotAsync(CancellationToken cancellationToken)
    {
        var organization = await BuildOrganizationAsync(cancellationToken);
        var notificationSettings = MapNotificationSettings(await GetOrCreateNotificationSettingsAsync(cancellationToken));
        notificationSettings.Preview = new NotificationPreviewDto();

        return new ControlSettingsBackupSnapshot
        {
            Organization = organization,
            Notifications = notificationSettings,
            System = MapSystemSettings(await GetOrCreateSystemSettingsAsync(cancellationToken)),
            Security = MapSecuritySettings(await GetOrCreateSecuritySettingsAsync(cancellationToken))
        };
    }

    private async Task ApplyOrganizationSnapshotAsync(OrganizationSettingsDto dto, CancellationToken cancellationToken)
    {
        var profile = await _db.HospitalProfiles.OrderBy(x => x.HospitalProfileId).FirstOrDefaultAsync(cancellationToken);
        if (profile is null)
        {
            profile = new HospitalProfile();
            _db.HospitalProfiles.Add(profile);
        }

        ApplyProfile(profile, dto.Profile);
        profile.UpdatedAt = DateTime.UtcNow;

        var existingBranches = await _db.Branches.OrderBy(x => x.BranchId).ToListAsync(cancellationToken);
        var incomingBranches = dto.Branches?.ToList() ?? new List<BranchDto>();
        var primaryIndex = incomingBranches.FindIndex(x => x.IsPrimary);
        var seenBranchIds = new HashSet<long>();

        for (var index = 0; index < incomingBranches.Count; index++)
        {
            var branchDto = incomingBranches[index];
            Branch? branch = null;
            if (branchDto.BranchId > 0)
            {
                branch = existingBranches.FirstOrDefault(x => x.BranchId == branchDto.BranchId);
                if (branch is not null)
                {
                    seenBranchIds.Add(branch.BranchId);
                }
            }

            if (branch is null)
            {
                branch = new Branch { CreatedAt = DateTime.UtcNow };
                _db.Branches.Add(branch);
                existingBranches.Add(branch);
            }

            ApplyBranch(branch, branchDto, primaryIndex >= 0 && primaryIndex == index);
        }

        foreach (var branch in existingBranches.Where(x => !seenBranchIds.Contains(x.BranchId) && incomingBranches.All(y => y.BranchId != x.BranchId)))
        {
            branch.IsActive = false;
            branch.IsPrimary = false;
            branch.UpdatedAt = DateTime.UtcNow;
        }

        var savedPrimary = existingBranches.FirstOrDefault(x => x.IsPrimary);
        if (savedPrimary is null && existingBranches.Count > 0)
        {
            existingBranches[0].IsPrimary = true;
        }

        await SyncSystemSettingsFromProfileAsync(profile, cancellationToken);
    }

    private async Task SyncSystemSettingsFromProfileAsync(HospitalProfile profile, CancellationToken cancellationToken)
    {
        var systemSettings = await GetOrCreateSystemSettingsAsync(cancellationToken);
        systemSettings.DefaultCurrencyCode = profile.CurrencyCode.Trim().ToUpperInvariant();
        systemSettings.InvoicePrefix = profile.InvoicePrefix.Trim().ToUpperInvariant();
        systemSettings.NextInvoiceNumber = Math.Max(profile.InvoiceStartingNumber, 1);
        systemSettings.UpdatedAt = DateTime.UtcNow;
    }

    private async Task<NotificationSetting> GetOrCreateNotificationSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await _db.NotificationSettings.OrderBy(x => x.NotificationSettingId).FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new NotificationSetting();
        _db.NotificationSettings.Add(settings);
        await _db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private async Task<SystemControlSetting> GetOrCreateSystemSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await _db.SystemControlSettings.OrderBy(x => x.SystemControlSettingId).FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new SystemControlSetting();
        _db.SystemControlSettings.Add(settings);
        await _db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private async Task<SecuritySetting> GetOrCreateSecuritySettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await _db.SecuritySettings.OrderBy(x => x.SecuritySettingId).FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new SecuritySetting();
        _db.SecuritySettings.Add(settings);
        await _db.SaveChangesAsync(cancellationToken);
        return settings;
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

    private static NotificationSettingsDto MapNotificationSettings(NotificationSetting settings)
    {
        return new NotificationSettingsDto
        {
            LowStockAlertsEnabled = settings.LowStockAlertsEnabled,
            LowStockAlertChannels = settings.LowStockAlertChannels,
            LowStockReminderFrequencyHours = settings.LowStockReminderFrequencyHours,
            ExpiryAlertsEnabled = settings.ExpiryAlertsEnabled,
            ExpiryAlertDays = settings.ExpiryAlertDays,
            ExpiryAlertChannels = settings.ExpiryAlertChannels,
            AppointmentRemindersEnabled = settings.AppointmentRemindersEnabled,
            AppointmentReminderHoursBefore = settings.AppointmentReminderHoursBefore,
            AppointmentReminderChannels = settings.AppointmentReminderChannels,
            PaymentDueAlertsEnabled = settings.PaymentDueAlertsEnabled,
            PaymentDueReminderDaysBefore = settings.PaymentDueReminderDaysBefore,
            PaymentDueAlertChannels = settings.PaymentDueAlertChannels,
            RecipientEmails = settings.RecipientEmails,
            UpdatedAt = settings.UpdatedAt
        };
    }

    private static SystemSettingsDto MapSystemSettings(SystemControlSetting settings)
    {
        return new SystemSettingsDto
        {
            DefaultCurrencyCode = settings.DefaultCurrencyCode,
            TimeZoneId = settings.TimeZoneId,
            InvoicePrefix = settings.InvoicePrefix,
            NextInvoiceNumber = settings.NextInvoiceNumber,
            SmsProviderName = settings.SmsProviderName,
            SmsApiUrl = settings.SmsApiUrl,
            SmsApiKey = settings.SmsApiKey,
            SmsSenderId = settings.SmsSenderId,
            EmailProviderName = settings.EmailProviderName,
            EmailApiUrl = settings.EmailApiUrl,
            EmailSmtpPort = settings.EmailSmtpPort,
            EmailSmtpUsername = settings.EmailSmtpUsername,
            EmailApiKey = settings.EmailApiKey,
            EmailFromAddress = settings.EmailFromAddress,
            EmailUseSsl = settings.EmailUseSsl,
            DoctorPortalBaseUrl = settings.DoctorPortalBaseUrl,
            AutoBackupEnabled = settings.AutoBackupEnabled,
            AutoBackupTime = settings.AutoBackupTime,
            BackupRetentionCount = settings.BackupRetentionCount,
            BackupStoragePath = settings.BackupStoragePath,
            UpdatedAt = settings.UpdatedAt
        };
    }

    private static SecuritySettingsDto MapSecuritySettings(SecuritySetting settings)
    {
        return new SecuritySettingsDto
        {
            SessionTimeoutMinutes = settings.SessionTimeoutMinutes,
            MinPasswordLength = settings.MinPasswordLength,
            RequireUppercase = settings.RequireUppercase,
            RequireLowercase = settings.RequireLowercase,
            RequireDigit = settings.RequireDigit,
            RequireSpecialCharacter = settings.RequireSpecialCharacter,
            PasswordExpiryDays = settings.PasswordExpiryDays,
            PermissionReviewIntervalDays = settings.PermissionReviewIntervalDays,
            LastPermissionReviewAt = settings.LastPermissionReviewAt,
            LastPermissionReviewedByName = settings.LastPermissionReviewedByName,
            UpdatedAt = settings.UpdatedAt
        };
    }

    private static BackupLogDto MapBackupLog(BackupLog log)
    {
        return new BackupLogDto
        {
            BackupLogId = log.BackupLogId,
            BackupName = log.BackupName,
            BackupType = log.BackupType,
            Status = log.Status,
            Summary = log.Summary,
            TriggeredByName = log.TriggeredByName,
            CreatedAt = log.CreatedAt,
            RestoredAt = log.RestoredAt,
            RestoredByName = log.RestoredByName,
            CanRestore = !string.IsNullOrWhiteSpace(log.SnapshotJson)
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
        target.InvoiceStartingNumber = Math.Max(source.InvoiceStartingNumber, 1);
        target.InvoiceFooterNote = source.InvoiceFooterNote.Trim();
        target.MultiBranchEnabled = source.MultiBranchEnabled;
    }

    private static void ApplyBranch(Branch branch, BranchDto source, bool isPrimary)
    {
        branch.Name = source.Name.Trim();
        branch.Code = source.Code.Trim().ToUpperInvariant();
        branch.Address = source.Address.Trim();
        branch.ContactPhone = source.ContactPhone.Trim();
        branch.ContactEmail = source.ContactEmail.Trim();
        branch.TotalBeds = Math.Max(source.TotalBeds, 0);
        branch.OccupiedBeds = Math.Min(Math.Max(source.OccupiedBeds, 0), branch.TotalBeds);
        branch.IsActive = source.IsActive;
        branch.IsPrimary = isPrimary;
        branch.UpdatedAt = DateTime.UtcNow;
    }

    private static void ApplyNotificationSettings(NotificationSetting target, NotificationSettingsDto source)
    {
        target.LowStockAlertsEnabled = source.LowStockAlertsEnabled;
        target.LowStockAlertChannels = NormalizeChannels(source.LowStockAlertChannels, "Dashboard,Email");
        target.LowStockReminderFrequencyHours = Math.Max(source.LowStockReminderFrequencyHours, 1);
        target.ExpiryAlertsEnabled = source.ExpiryAlertsEnabled;
        target.ExpiryAlertDays = Math.Max(source.ExpiryAlertDays, 1);
        target.ExpiryAlertChannels = NormalizeChannels(source.ExpiryAlertChannels, "Dashboard,Email");
        target.AppointmentRemindersEnabled = source.AppointmentRemindersEnabled;
        target.AppointmentReminderHoursBefore = Math.Max(source.AppointmentReminderHoursBefore, 1);
        target.AppointmentReminderChannels = NormalizeChannels(source.AppointmentReminderChannels, "SMS,Email");
        target.PaymentDueAlertsEnabled = source.PaymentDueAlertsEnabled;
        target.PaymentDueReminderDaysBefore = Math.Max(source.PaymentDueReminderDaysBefore, 0);
        target.PaymentDueAlertChannels = NormalizeChannels(source.PaymentDueAlertChannels, "Dashboard,Email");
        target.RecipientEmails = string.IsNullOrWhiteSpace(source.RecipientEmails) ? string.Empty : source.RecipientEmails.Trim();
    }

    private static void ApplySystemSettings(SystemControlSetting target, SystemSettingsDto source)
    {
        target.DefaultCurrencyCode = string.IsNullOrWhiteSpace(source.DefaultCurrencyCode) ? "NPR" : source.DefaultCurrencyCode.Trim().ToUpperInvariant();
        target.TimeZoneId = string.IsNullOrWhiteSpace(source.TimeZoneId) ? "Asia/Kathmandu" : source.TimeZoneId.Trim();
        target.InvoicePrefix = string.IsNullOrWhiteSpace(source.InvoicePrefix) ? "NEX" : source.InvoicePrefix.Trim().ToUpperInvariant();
        target.NextInvoiceNumber = Math.Max(source.NextInvoiceNumber, 1);
        target.SmsProviderName = NormalizeText(source.SmsProviderName);
        target.SmsApiUrl = NormalizeText(source.SmsApiUrl);
        target.SmsApiKey = NormalizeText(source.SmsApiKey);
        target.SmsSenderId = NormalizeText(source.SmsSenderId);
        target.EmailProviderName = NormalizeText(source.EmailProviderName);
        target.EmailApiUrl = NormalizeText(source.EmailApiUrl);
        target.EmailSmtpPort = source.EmailSmtpPort > 0 ? source.EmailSmtpPort : 587;
        target.EmailSmtpUsername = NormalizeText(source.EmailSmtpUsername);
        target.EmailApiKey = NormalizeText(source.EmailApiKey);
        target.EmailFromAddress = NormalizeText(source.EmailFromAddress);
        target.EmailUseSsl = source.EmailUseSsl;
        target.DoctorPortalBaseUrl = NormalizeText(source.DoctorPortalBaseUrl);
        target.AutoBackupEnabled = source.AutoBackupEnabled;
        target.AutoBackupTime = string.IsNullOrWhiteSpace(source.AutoBackupTime) ? "02:00" : source.AutoBackupTime.Trim();
        target.BackupRetentionCount = Math.Max(source.BackupRetentionCount, 1);
        target.BackupStoragePath = NormalizeText(source.BackupStoragePath);
    }

    private static void ApplySecuritySettings(SecuritySetting target, SecuritySettingsDto source)
    {
        target.SessionTimeoutMinutes = Math.Max(source.SessionTimeoutMinutes, 5);
        target.MinPasswordLength = Math.Max(source.MinPasswordLength, 6);
        target.RequireUppercase = source.RequireUppercase;
        target.RequireLowercase = source.RequireLowercase;
        target.RequireDigit = source.RequireDigit;
        target.RequireSpecialCharacter = source.RequireSpecialCharacter;
        target.PasswordExpiryDays = Math.Max(source.PasswordExpiryDays, 0);
        target.PermissionReviewIntervalDays = Math.Max(source.PermissionReviewIntervalDays, 1);
    }

    private static string NormalizeChannels(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var channels = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return channels.Length == 0 ? fallback : string.Join(',', channels);
    }

    private static string NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private long? GetCurrentActorId()
    {
        return long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var userId)
            ? userId
            : null;
    }

    private string GetCurrentActorName()
    {
        return User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "System Admin";
    }

    private string? GetCurrentActorRole()
    {
        return User.FindFirstValue(ClaimTypes.Role);
    }

    private string? GetCurrentActorEmail()
    {
        return User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
    }

    private sealed class ControlSettingsBackupSnapshot
    {
        public OrganizationSettingsDto Organization { get; set; } = new();
        public NotificationSettingsDto Notifications { get; set; } = new();
        public SystemSettingsDto System { get; set; } = new();
        public SecuritySettingsDto Security { get; set; } = new();
    }
}
