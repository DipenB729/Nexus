namespace AngularApp4.Dtos.Hms;

public class AppNotificationDto
{
    public long AppNotificationId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime ScheduledForUtc { get; set; }
    public DateTime CreatedAt { get; set; }
}
