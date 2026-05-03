using System.Security.Claims;
using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Hubs;

[Authorize]
public class CareCommunicationHub : Hub
{
    private static readonly AppointmentStatus[] OpenCareStatuses = { AppointmentStatus.Approved, AppointmentStatus.Completed };
    private readonly AppDbContext _db;

    public CareCommunicationHub(AppDbContext db)
    {
        _db = db;
    }

    public override async Task OnConnectedAsync()
    {
        var participant = await ResolveParticipantAsync(Context.ConnectionAborted);
        if (participant is null)
        {
            Context.Abort();
            return;
        }

        var appointmentIds = await BuildParticipantAppointments(participant)
            .Select(x => x.AppointmentId)
            .ToListAsync(Context.ConnectionAborted);

        foreach (var appointmentId in appointmentIds)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(appointmentId), Context.ConnectionAborted);
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinThread(long appointmentId)
    {
        var participant = await ResolveParticipantAsync(Context.ConnectionAborted);
        if (participant is null)
        {
            throw new HubException("Session profile not found");
        }

        var canAccess = await BuildParticipantAppointments(participant)
            .AnyAsync(x => x.AppointmentId == appointmentId, Context.ConnectionAborted);
        if (!canAccess)
        {
            throw new HubException("Approved appointment thread not found");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(appointmentId), Context.ConnectionAborted);
    }

    public async Task<CareConversationMessageDto> SendMessage(long appointmentId, string message)
    {
        var participant = await ResolveParticipantAsync(Context.ConnectionAborted);
        if (participant is null)
        {
            throw new HubException("Session profile not found");
        }

        var normalized = Normalize(message);
        if (normalized is null)
        {
            throw new HubException("Message is required");
        }

        var appointment = await BuildParticipantAppointments(participant)
            .FirstOrDefaultAsync(x => x.AppointmentId == appointmentId, Context.ConnectionAborted);
        if (appointment is null)
        {
            throw new HubException("Approved appointment thread not found");
        }

        var entity = new CareConversationMessage
        {
            AppointmentId = appointment.AppointmentId,
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId,
            SenderUserId = participant.UserId,
            SenderRole = participant.Role,
            SenderName = participant.DisplayName,
            Message = normalized,
            CreatedAt = DateTime.UtcNow
        };

        _db.CareConversationMessages.Add(entity);
        await _db.SaveChangesAsync(Context.ConnectionAborted);

        var dto = new CareConversationMessageDto
        {
            CareConversationMessageId = entity.CareConversationMessageId,
            AppointmentId = entity.AppointmentId,
            SenderRole = entity.SenderRole,
            SenderName = entity.SenderName,
            Message = entity.Message,
            CreatedAt = entity.CreatedAt
        };

        await Clients.Group(GroupName(appointmentId)).SendAsync("careMessageReceived", dto, Context.ConnectionAborted);
        return dto;
    }

    private IQueryable<Appointment> BuildParticipantAppointments(CareParticipant participant)
    {
        var query = _db.Appointments
            .AsNoTracking()
            .Where(x => OpenCareStatuses.Contains(x.Status));

        return participant.Role == "Doctor"
            ? query.Where(x => x.DoctorId == participant.DoctorId)
            : query.Where(x => x.PatientId == participant.PatientId);
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

    private long GetUserId() => long.Parse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirstValue("sub")!);

    private static string GroupName(long appointmentId) => $"care-appointment-{appointmentId}";

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record CareParticipant(long UserId, string Role, string DisplayName, long? PatientId, long? DoctorId);
}
