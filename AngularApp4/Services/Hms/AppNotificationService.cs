using AngularApp4.Data;
using AngularApp4.Model.Hms;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Services.Hms;

public interface IAppNotificationService
{
    Task QueueAppointmentBookedAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task QueueAppointmentRescheduledAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task QueueAppointmentCancelledAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task QueueAppointmentApprovedAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task QueueFollowUpReminderAsync(Appointment appointment, DateTime followUpDate, string doctorName, CancellationToken cancellationToken = default);
}

public sealed class AppNotificationService : IAppNotificationService
{
    private readonly AppDbContext _db;

    public AppNotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task QueueAppointmentBookedAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        var context = await BuildContextAsync(appointment, cancellationToken);
        if (context is null)
        {
            return;
        }

        AddNotification(
            context.PatientUserId,
            "Appointment",
            "BookingConfirmation",
            "Booking confirmed",
            $"Your appointment with {context.DoctorName} on {FormatDate(appointment.AppointmentDate)} at {FormatTime(appointment.SlotStartTime)} has been booked.",
            "/patient/appointments",
            appointment);

        if (context.DoctorUserId.HasValue)
        {
            AddNotification(
                context.DoctorUserId.Value,
                "Appointment",
                "NewBooking",
                "New patient booking",
                $"{context.PatientName} booked an appointment on {FormatDate(appointment.AppointmentDate)} at {FormatTime(appointment.SlotStartTime)}.",
                "/doctor/appointments",
                appointment);
        }

        foreach (var adminUserId in context.AdminUserIds)
        {
            AddNotification(
                adminUserId,
                "Appointment",
                "AdminBookingNotice",
                "New booking request",
                $"{context.PatientName} booked an appointment with {context.DoctorName} for {FormatDate(appointment.AppointmentDate)}.",
                "/admin/bookings",
                appointment);
        }

        await QueueReminderNotificationsAsync(appointment, context, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task QueueAppointmentRescheduledAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        var context = await BuildContextAsync(appointment, cancellationToken);
        if (context is null)
        {
            return;
        }

        await DismissPendingRemindersAsync(appointment.AppointmentId, cancellationToken);

        AddNotification(
            context.PatientUserId,
            "Appointment",
            "RescheduleNotice",
            "Appointment rescheduled",
            $"Your appointment with {context.DoctorName} has been rescheduled to {FormatDate(appointment.AppointmentDate)} at {FormatTime(appointment.SlotStartTime)}.",
            "/patient/appointments",
            appointment);

        if (context.DoctorUserId.HasValue)
        {
            AddNotification(
                context.DoctorUserId.Value,
                "Appointment",
                "RescheduleNotice",
                "Appointment rescheduled",
                $"{context.PatientName}'s appointment has been moved to {FormatDate(appointment.AppointmentDate)} at {FormatTime(appointment.SlotStartTime)}.",
                "/doctor/appointments",
                appointment);
        }

        foreach (var adminUserId in context.AdminUserIds)
        {
            AddNotification(
                adminUserId,
                "Appointment",
                "AdminRescheduleNotice",
                "Appointment rescheduled",
                $"{context.PatientName}'s appointment with {context.DoctorName} was rescheduled.",
                "/admin/bookings",
                appointment);
        }

        await QueueReminderNotificationsAsync(appointment, context, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task QueueAppointmentCancelledAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        var context = await BuildContextAsync(appointment, cancellationToken);
        if (context is null)
        {
            return;
        }

        await DismissPendingRemindersAsync(appointment.AppointmentId, cancellationToken);

        AddNotification(
            context.PatientUserId,
            "Appointment",
            "CancellationNotice",
            "Appointment cancelled",
            $"Your appointment with {context.DoctorName} on {FormatDate(appointment.AppointmentDate)} at {FormatTime(appointment.SlotStartTime)} has been cancelled.",
            "/patient/appointments",
            appointment);

        if (context.DoctorUserId.HasValue)
        {
            AddNotification(
                context.DoctorUserId.Value,
                "Appointment",
                "CancellationNotice",
                "Appointment cancelled",
                $"{context.PatientName}'s appointment on {FormatDate(appointment.AppointmentDate)} at {FormatTime(appointment.SlotStartTime)} has been cancelled.",
                "/doctor/appointments",
                appointment);
        }

        foreach (var adminUserId in context.AdminUserIds)
        {
            AddNotification(
                adminUserId,
                "Appointment",
                "AdminCancellationNotice",
                "Appointment cancelled",
                $"{context.PatientName}'s appointment with {context.DoctorName} was cancelled.",
                "/admin/bookings",
                appointment);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task QueueAppointmentApprovedAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        var context = await BuildContextAsync(appointment, cancellationToken);
        if (context is null)
        {
            return;
        }

        AddNotification(
            context.PatientUserId,
            "Appointment",
            "ApprovalNotice",
            "Appointment approved",
            $"Your appointment with {context.DoctorName} on {FormatDate(appointment.AppointmentDate)} has been approved.",
            "/patient/appointments",
            appointment);

        if (context.DoctorUserId.HasValue)
        {
            AddNotification(
                context.DoctorUserId.Value,
                "Appointment",
                "ApprovalNotice",
                "Appointment approved",
                $"{context.PatientName}'s appointment on {FormatDate(appointment.AppointmentDate)} has been approved.",
                "/doctor/appointments",
                appointment);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task QueueFollowUpReminderAsync(Appointment appointment, DateTime followUpDate, string doctorName, CancellationToken cancellationToken = default)
    {
        var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(x => x.PatientId == appointment.PatientId, cancellationToken);
        if (patient is null)
        {
            return;
        }

        var patientUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == patient.UserId && x.IsActive, cancellationToken);
        if (patientUser is null)
        {
            return;
        }

        _db.AppNotifications.Add(new AppNotification
        {
            RecipientUserId = patientUser.UserId,
            Category = "FollowUp",
            NotificationType = "FollowUpReminder",
            Title = "Follow-up reminder",
            Message = $"Follow-up with {doctorName} is scheduled for {FormatDate(followUpDate)}.",
            RelatedEntityName = "Appointment",
            RelatedEntityId = appointment.AppointmentId,
            ActionUrl = "/patient/appointments",
            IsRead = false,
            IsDismissed = false,
            ScheduledForUtc = followUpDate.Date <= DateTime.UtcNow.Date ? DateTime.UtcNow : followUpDate.Date,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task QueueReminderNotificationsAsync(Appointment appointment, AppointmentContext context, CancellationToken cancellationToken)
    {
        var settings = await _db.NotificationSettings
            .AsNoTracking()
            .OrderBy(x => x.NotificationSettingId)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings?.AppointmentRemindersEnabled != true)
        {
            return;
        }

        var appointmentUtc = appointment.AppointmentDate.Date + appointment.SlotStartTime;
        var scheduledForUtc = appointmentUtc.AddHours(-Math.Max(settings.AppointmentReminderHoursBefore, 1));
        if (scheduledForUtc <= DateTime.UtcNow)
        {
            scheduledForUtc = DateTime.UtcNow;
        }

        AddNotification(
            context.PatientUserId,
            "Appointment",
            "Reminder",
            "Appointment reminder",
            $"Reminder: you have an appointment with {context.DoctorName} on {FormatDate(appointment.AppointmentDate)} at {FormatTime(appointment.SlotStartTime)}.",
            "/patient/appointments",
            appointment,
            scheduledForUtc);

        if (context.DoctorUserId.HasValue)
        {
            AddNotification(
                context.DoctorUserId.Value,
                "Appointment",
                "Reminder",
                "Appointment reminder",
                $"Reminder: {context.PatientName} is scheduled on {FormatDate(appointment.AppointmentDate)} at {FormatTime(appointment.SlotStartTime)}.",
                "/doctor/appointments",
                appointment,
                scheduledForUtc);
        }
    }

    private async Task DismissPendingRemindersAsync(long appointmentId, CancellationToken cancellationToken)
    {
        var reminders = await _db.AppNotifications
            .Where(x => x.RelatedEntityName == "Appointment"
                && x.RelatedEntityId == appointmentId
                && x.NotificationType == "Reminder"
                && !x.IsDismissed
                && !x.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var reminder in reminders)
        {
            reminder.IsDismissed = true;
        }
    }

    private void AddNotification(
        long recipientUserId,
        string category,
        string notificationType,
        string title,
        string message,
        string actionUrl,
        Appointment appointment,
        DateTime? scheduledForUtc = null)
    {
        _db.AppNotifications.Add(new AppNotification
        {
            RecipientUserId = recipientUserId,
            Category = category,
            NotificationType = notificationType,
            Title = title,
            Message = message,
            RelatedEntityName = "Appointment",
            RelatedEntityId = appointment.AppointmentId,
            ActionUrl = actionUrl,
            IsRead = false,
            IsDismissed = false,
            ScheduledForUtc = scheduledForUtc ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
    }

    private async Task<AppointmentContext?> BuildContextAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(x => x.PatientId == appointment.PatientId, cancellationToken);
        var patientUser = patient is null
            ? null
            : await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == patient.UserId && x.IsActive, cancellationToken);
        var doctor = await _db.Doctors.AsNoTracking().FirstOrDefaultAsync(x => x.DoctorId == appointment.DoctorId, cancellationToken);
        if (patientUser is null || doctor is null)
        {
            return null;
        }

        var normalizedDoctorEmail = doctor.Email.Trim().ToLowerInvariant();
        var doctorUserId = await _db.Users.AsNoTracking()
            .Where(x => x.Email == normalizedDoctorEmail && x.IsActive)
            .Select(x => (long?)x.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        var adminRoleId = await _db.Roles.AsNoTracking()
            .Where(x => x.Name == "Admin")
            .Select(x => x.RoleId)
            .FirstAsync(cancellationToken);

        var adminUserIds = await _db.Users.AsNoTracking()
            .Where(x => x.RoleId == adminRoleId && x.IsActive)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        return new AppointmentContext(
            patientUser.UserId,
            patientUser.FullName,
            doctorUserId,
            doctor.FullName,
            adminUserIds);
    }

    private static string FormatDate(DateTime date) => date.ToString("MMM dd, yyyy");

    private static string FormatTime(TimeSpan time) => $"{time:hh\\:mm}";

    private sealed record AppointmentContext(
        long PatientUserId,
        string PatientName,
        long? DoctorUserId,
        string DoctorName,
        List<long> AdminUserIds);
}
