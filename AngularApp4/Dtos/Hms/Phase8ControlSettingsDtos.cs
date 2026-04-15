namespace AngularApp4.Dtos.Hms;

public class ControlCenterSettingsDto
{
    public OrganizationSettingsDto Organization { get; set; } = new();
    public NotificationSettingsDto Notifications { get; set; } = new();
    public SystemSettingsDto System { get; set; } = new();
    public BackupCenterDto Backups { get; set; } = new();
    public SecurityCenterDto Security { get; set; } = new();
}

public class NotificationSettingsDto
{
    public bool LowStockAlertsEnabled { get; set; }
    public string LowStockAlertChannels { get; set; } = string.Empty;
    public int LowStockReminderFrequencyHours { get; set; }
    public bool ExpiryAlertsEnabled { get; set; }
    public int ExpiryAlertDays { get; set; }
    public string ExpiryAlertChannels { get; set; } = string.Empty;
    public bool AppointmentRemindersEnabled { get; set; }
    public int AppointmentReminderHoursBefore { get; set; }
    public string AppointmentReminderChannels { get; set; } = string.Empty;
    public bool PaymentDueAlertsEnabled { get; set; }
    public int PaymentDueReminderDaysBefore { get; set; }
    public string PaymentDueAlertChannels { get; set; } = string.Empty;
    public string RecipientEmails { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public NotificationPreviewDto Preview { get; set; } = new();
}

public class NotificationPreviewDto
{
    public int LowStockCount { get; set; }
    public int NearExpiryCount { get; set; }
    public int AppointmentReminderCount { get; set; }
    public int PaymentDueCount { get; set; }
    public IEnumerable<StockAlertDto> LowStockAlerts { get; set; } = Array.Empty<StockAlertDto>();
    public IEnumerable<StockAlertDto> ExpiryAlerts { get; set; } = Array.Empty<StockAlertDto>();
    public IEnumerable<AppointmentReminderPreviewDto> AppointmentReminders { get; set; } = Array.Empty<AppointmentReminderPreviewDto>();
    public IEnumerable<PendingPaymentDto> PaymentDueAlerts { get; set; } = Array.Empty<PendingPaymentDto>();
}

public class AppointmentReminderPreviewDto
{
    public long AppointmentId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class SystemSettingsDto
{
    public string DefaultCurrencyCode { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = string.Empty;
    public string InvoicePrefix { get; set; } = string.Empty;
    public int NextInvoiceNumber { get; set; }
    public string SmsProviderName { get; set; } = string.Empty;
    public string SmsApiUrl { get; set; } = string.Empty;
    public string SmsApiKey { get; set; } = string.Empty;
    public string SmsSenderId { get; set; } = string.Empty;
    public string EmailProviderName { get; set; } = string.Empty;
    public string EmailApiUrl { get; set; } = string.Empty;
    public int EmailSmtpPort { get; set; }
    public string EmailSmtpUsername { get; set; } = string.Empty;
    public string EmailApiKey { get; set; } = string.Empty;
    public string EmailFromAddress { get; set; } = string.Empty;
    public bool EmailUseSsl { get; set; }
    public string DoctorPortalBaseUrl { get; set; } = string.Empty;
    public bool AutoBackupEnabled { get; set; }
    public string AutoBackupTime { get; set; } = string.Empty;
    public int BackupRetentionCount { get; set; }
    public string BackupStoragePath { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

public class BackupCenterDto
{
    public DateTime? LastBackupAt { get; set; }
    public DateTime? LastRestoreAt { get; set; }
    public IEnumerable<BackupLogDto> Logs { get; set; } = Array.Empty<BackupLogDto>();
}

public class BackupLogDto
{
    public long BackupLogId { get; set; }
    public string BackupName { get; set; } = string.Empty;
    public string BackupType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? TriggeredByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RestoredAt { get; set; }
    public string? RestoredByName { get; set; }
    public bool CanRestore { get; set; }
}

public class CreateBackupRequestDto
{
    public string? BackupName { get; set; }
}

public class SecurityCenterDto
{
    public SecuritySettingsDto Settings { get; set; } = new();
    public PermissionReviewSummaryDto PermissionReview { get; set; } = new();
    public IEnumerable<SecurityDeviceLogDto> DeviceLogs { get; set; } = Array.Empty<SecurityDeviceLogDto>();
}

public class SecuritySettingsDto
{
    public int SessionTimeoutMinutes { get; set; }
    public int MinPasswordLength { get; set; }
    public bool RequireUppercase { get; set; }
    public bool RequireLowercase { get; set; }
    public bool RequireDigit { get; set; }
    public bool RequireSpecialCharacter { get; set; }
    public int PasswordExpiryDays { get; set; }
    public int PermissionReviewIntervalDays { get; set; }
    public DateTime? LastPermissionReviewAt { get; set; }
    public string? LastPermissionReviewedByName { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PermissionReviewSummaryDto
{
    public int TotalRoles { get; set; }
    public int ActiveRoles { get; set; }
    public int TotalUsers { get; set; }
    public DateTime? LastReviewedAt { get; set; }
    public string? LastReviewedByName { get; set; }
    public DateTime? NextReviewDueAt { get; set; }
    public IEnumerable<PermissionReviewRoleDto> Roles { get; set; } = Array.Empty<PermissionReviewRoleDto>();
}

public class PermissionReviewRoleDto
{
    public long RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int UserCount { get; set; }
    public int ModulesWithView { get; set; }
    public int ModulesWithEdit { get; set; }
    public int ModulesWithDelete { get; set; }
}

public class SecurityDeviceLogDto
{
    public long AuditLogEntryId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public string? ActorEmail { get; set; }
    public string? ActorRole { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PasswordPolicyValidationResultDto
{
    public bool IsValid { get; set; }
    public IEnumerable<string> Errors { get; set; } = Array.Empty<string>();
}
