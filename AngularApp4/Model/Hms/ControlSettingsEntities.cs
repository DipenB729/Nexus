using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AngularApp4.Model.Hms;

public class NotificationSetting
{
    [Key] public long NotificationSettingId { get; set; }
    public bool LowStockAlertsEnabled { get; set; } = true;
    [MaxLength(80)] public string LowStockAlertChannels { get; set; } = "Dashboard,Email";
    public int LowStockReminderFrequencyHours { get; set; } = 12;
    public bool ExpiryAlertsEnabled { get; set; } = true;
    public int ExpiryAlertDays { get; set; } = 30;
    [MaxLength(80)] public string ExpiryAlertChannels { get; set; } = "Dashboard,Email";
    public bool AppointmentRemindersEnabled { get; set; } = true;
    public int AppointmentReminderHoursBefore { get; set; } = 24;
    [MaxLength(80)] public string AppointmentReminderChannels { get; set; } = "SMS,Email";
    public bool PaymentDueAlertsEnabled { get; set; } = true;
    public int PaymentDueReminderDaysBefore { get; set; } = 2;
    [MaxLength(80)] public string PaymentDueAlertChannels { get; set; } = "Dashboard,Email";
    [MaxLength(500)] public string RecipientEmails { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class SystemControlSetting
{
    [Key] public long SystemControlSettingId { get; set; }
    [Required, MaxLength(10)] public string DefaultCurrencyCode { get; set; } = "NPR";
    [Required, MaxLength(80)] public string TimeZoneId { get; set; } = "Asia/Kathmandu";
    [Required, MaxLength(20)] public string InvoicePrefix { get; set; } = "NEX";
    public int NextInvoiceNumber { get; set; } = 5001;
    [MaxLength(80)] public string SmsProviderName { get; set; } = string.Empty;
    [MaxLength(250)] public string SmsApiUrl { get; set; } = string.Empty;
    [MaxLength(200)] public string SmsApiKey { get; set; } = string.Empty;
    [MaxLength(80)] public string SmsSenderId { get; set; } = string.Empty;
    [MaxLength(80)] public string EmailProviderName { get; set; } = string.Empty;
    [MaxLength(250)] public string EmailApiUrl { get; set; } = string.Empty;
    public int EmailSmtpPort { get; set; } = 587;
    [MaxLength(150)] public string EmailSmtpUsername { get; set; } = string.Empty;
    [MaxLength(200)] public string EmailApiKey { get; set; } = string.Empty;
    [MaxLength(150)] public string EmailFromAddress { get; set; } = string.Empty;
    public bool EmailUseSsl { get; set; } = true;
    [MaxLength(250)] public string DoctorPortalBaseUrl { get; set; } = string.Empty;
    public bool AutoBackupEnabled { get; set; } = true;
    [MaxLength(10)] public string AutoBackupTime { get; set; } = "02:00";
    public int BackupRetentionCount { get; set; } = 10;
    [MaxLength(250)] public string BackupStoragePath { get; set; } = "App_Data/Backups";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class SecuritySetting
{
    [Key] public long SecuritySettingId { get; set; }
    public int SessionTimeoutMinutes { get; set; } = 120;
    public int MinPasswordLength { get; set; } = 8;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialCharacter { get; set; } = true;
    public int PasswordExpiryDays { get; set; } = 90;
    public int PermissionReviewIntervalDays { get; set; } = 30;
    public DateTime? LastPermissionReviewAt { get; set; }
    [MaxLength(150)] public string? LastPermissionReviewedByName { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class BackupLog
{
    [Key] public long BackupLogId { get; set; }
    [Required, MaxLength(120)] public string BackupName { get; set; } = string.Empty;
    [Required, MaxLength(40)] public string BackupType { get; set; } = string.Empty;
    [Required, MaxLength(40)] public string Status { get; set; } = string.Empty;
    [Required, MaxLength(300)] public string Summary { get; set; } = string.Empty;
    public string? SnapshotJson { get; set; }
    public long? TriggeredByUserId { get; set; }
    [MaxLength(150)] public string? TriggeredByName { get; set; }
    public DateTime? RestoredAt { get; set; }
    [MaxLength(150)] public string? RestoredByName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AppNotification
{
    [Key] public long AppNotificationId { get; set; }
    public long RecipientUserId { get; set; }
    [Required, MaxLength(60)] public string Category { get; set; } = "Appointment";
    [Required, MaxLength(80)] public string NotificationType { get; set; } = string.Empty;
    [Required, MaxLength(140)] public string Title { get; set; } = string.Empty;
    [Required, MaxLength(500)] public string Message { get; set; } = string.Empty;
    [MaxLength(120)] public string? RelatedEntityName { get; set; }
    public long? RelatedEntityId { get; set; }
    [MaxLength(250)] public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public bool IsDismissed { get; set; }
    public DateTime ScheduledForUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAtUtc { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
