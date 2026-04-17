using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Services.Hms;

public interface IDoctorAvailabilityService
{
    Task<IReadOnlyList<ResolvedDoctorSchedule>> GetSchedulesForDateAsync(long doctorId, DateTime date, CancellationToken cancellationToken = default);
    Task<ResolvedDoctorSchedule?> ResolveScheduleForSlotAsync(long doctorId, DateTime date, TimeSpan slotStartTime, TimeSpan slotEndTime, CancellationToken cancellationToken = default);
    Task<List<DoctorAvailableSlotDto>> GetAvailableSlotsAsync(long doctorId, DateTime date, CancellationToken cancellationToken = default);
    Task<int> GetBookedPatientCountAsync(long doctorId, DateTime date, TimeSpan slotStartTime, TimeSpan slotEndTime, long? excludeAppointmentId = null, CancellationToken cancellationToken = default);
    Task SyncDoctorAvailabilitySummaryAsync(long doctorId, CancellationToken cancellationToken = default);
}

public sealed class ResolvedDoctorSchedule
{
    public long? ScheduleId { get; init; }
    public long DoctorId { get; init; }
    public byte DayOfWeek { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public TimeSpan? BreakStartTime { get; init; }
    public TimeSpan? BreakEndTime { get; init; }
    public int SlotDurationMinutes { get; init; }
    public int MaxPatientsPerSlot { get; init; }
    public bool OnlineBookingEnabled { get; init; }
}

public sealed class DoctorAvailabilityService : IDoctorAvailabilityService
{
    private readonly AppDbContext _db;

    public DoctorAvailabilityService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ResolvedDoctorSchedule>> GetSchedulesForDateAsync(long doctorId, DateTime date, CancellationToken cancellationToken = default)
    {
        var blockedDate = await _db.DoctorAvailabilityExceptions
            .AsNoTracking()
            .AnyAsync(x =>
                x.DoctorId == doctorId &&
                x.IsActive &&
                x.StartDate.Date <= date.Date &&
                x.EndDate.Date >= date.Date,
                cancellationToken);

        if (blockedDate)
        {
            return Array.Empty<ResolvedDoctorSchedule>();
        }

        var dayOfWeek = ToScheduleDayOfWeek(date.DayOfWeek);
        var schedules = await _db.DoctorSchedules
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId && x.DayOfWeek == dayOfWeek && x.IsActive)
            .OrderBy(x => x.StartTime)
            .ThenBy(x => x.EndTime)
            .Select(x => new ResolvedDoctorSchedule
            {
                ScheduleId = x.ScheduleId,
                DoctorId = x.DoctorId,
                DayOfWeek = x.DayOfWeek,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                BreakStartTime = x.BreakStartTime,
                BreakEndTime = x.BreakEndTime,
                SlotDurationMinutes = x.SlotDurationMinutes,
                MaxPatientsPerSlot = x.MaxPatientsPerSlot,
                OnlineBookingEnabled = x.OnlineBookingEnabled
            })
            .ToListAsync(cancellationToken);

        if (schedules.Count > 0)
        {
            return schedules;
        }

        var hasConfiguredSchedules = await _db.DoctorSchedules
            .AsNoTracking()
            .AnyAsync(x => x.DoctorId == doctorId, cancellationToken);

        if (hasConfiguredSchedules)
        {
            return Array.Empty<ResolvedDoctorSchedule>();
        }

        var doctor = await _db.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.DoctorId == doctorId && x.IsActive, cancellationToken);

        if (doctor is null ||
            !doctor.OpdStartTime.HasValue ||
            !doctor.OpdEndTime.HasValue ||
            !IsDoctorAvailableOnDay(doctor.OpdDays, date.DayOfWeek))
        {
            return Array.Empty<ResolvedDoctorSchedule>();
        }

        return new[]
        {
            new ResolvedDoctorSchedule
            {
                DoctorId = doctor.DoctorId,
                DayOfWeek = dayOfWeek,
                StartTime = doctor.OpdStartTime.Value,
                EndTime = doctor.OpdEndTime.Value,
                SlotDurationMinutes = 30,
                MaxPatientsPerSlot = 1,
                OnlineBookingEnabled = true
            }
        };
    }

    public async Task<ResolvedDoctorSchedule?> ResolveScheduleForSlotAsync(long doctorId, DateTime date, TimeSpan slotStartTime, TimeSpan slotEndTime, CancellationToken cancellationToken = default)
    {
        var schedules = await GetSchedulesForDateAsync(doctorId, date, cancellationToken);
        if (slotEndTime <= slotStartTime)
        {
            return null;
        }

        var hasBlockedSlot = await _db.DoctorBlockedSlots
            .AsNoTracking()
            .AnyAsync(x =>
                x.DoctorId == doctorId &&
                x.IsActive &&
                x.BlockDate.Date == date.Date &&
                slotStartTime < x.EndTime &&
                x.StartTime < slotEndTime,
                cancellationToken);

        if (hasBlockedSlot)
        {
            return null;
        }

        return schedules.FirstOrDefault(schedule =>
        {
            var expectedDuration = TimeSpan.FromMinutes(schedule.SlotDurationMinutes);
            var isAligned = slotStartTime >= schedule.StartTime &&
                slotEndTime <= schedule.EndTime &&
                slotEndTime - slotStartTime == expectedDuration &&
                (slotStartTime - schedule.StartTime).TotalMinutes % schedule.SlotDurationMinutes == 0 &&
                schedule.OnlineBookingEnabled &&
                !OverlapsBreak(schedule, slotStartTime, slotEndTime);

            return isAligned;
        });
    }

    public async Task<List<DoctorAvailableSlotDto>> GetAvailableSlotsAsync(long doctorId, DateTime date, CancellationToken cancellationToken = default)
    {
        var schedules = (await GetSchedulesForDateAsync(doctorId, date, cancellationToken))
            .Where(x => x.OnlineBookingEnabled)
            .ToList();
        if (schedules.Count == 0)
        {
            return new List<DoctorAvailableSlotDto>();
        }

        var blockedSlots = await _db.DoctorBlockedSlots
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId && x.IsActive && x.BlockDate.Date == date.Date)
            .OrderBy(x => x.StartTime)
            .Select(x => new { x.StartTime, x.EndTime })
            .ToListAsync(cancellationToken);

        var bookedCounts = await _db.Appointments
            .AsNoTracking()
            .Where(x =>
                x.DoctorId == doctorId &&
                x.AppointmentDate.Date == date.Date &&
                x.Status != AppointmentStatus.Cancelled)
            .GroupBy(x => new { x.SlotStartTime, x.SlotEndTime })
            .Select(group => new
            {
                group.Key.SlotStartTime,
                group.Key.SlotEndTime,
                BookedPatients = group.Count()
            })
            .ToListAsync(cancellationToken);

        var bookedLookup = bookedCounts.ToDictionary(
            x => $"{x.SlotStartTime:c}|{x.SlotEndTime:c}",
            x => x.BookedPatients,
            StringComparer.Ordinal);

        var slots = new List<DoctorAvailableSlotDto>();

        foreach (var schedule in schedules)
        {
            var slotLength = TimeSpan.FromMinutes(schedule.SlotDurationMinutes);
            for (var cursor = schedule.StartTime; cursor + slotLength <= schedule.EndTime; cursor += slotLength)
            {
                var end = cursor + slotLength;
                if (OverlapsBreak(schedule, cursor, end))
                {
                    continue;
                }

                if (blockedSlots.Any(x => cursor < x.EndTime && x.StartTime < end))
                {
                    continue;
                }

                var key = $"{cursor:c}|{end:c}";
                var bookedPatients = bookedLookup.GetValueOrDefault(key);
                var remainingPatients = Math.Max(schedule.MaxPatientsPerSlot - bookedPatients, 0);

                if (remainingPatients == 0)
                {
                    continue;
                }

                slots.Add(new DoctorAvailableSlotDto
                {
                    ScheduleId = schedule.ScheduleId,
                    StartTime = cursor,
                    EndTime = end,
                    MaxPatientsPerSlot = schedule.MaxPatientsPerSlot,
                    BookedPatients = bookedPatients,
                    RemainingPatients = remainingPatients
                });
            }
        }

        return slots
            .OrderBy(x => x.StartTime)
            .ThenBy(x => x.EndTime)
            .ToList();
    }

    public async Task<int> GetBookedPatientCountAsync(long doctorId, DateTime date, TimeSpan slotStartTime, TimeSpan slotEndTime, long? excludeAppointmentId = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Appointments
            .AsNoTracking()
            .Where(x =>
                x.DoctorId == doctorId &&
                x.AppointmentDate.Date == date.Date &&
                x.SlotStartTime == slotStartTime &&
                x.SlotEndTime == slotEndTime &&
                x.Status != AppointmentStatus.Cancelled);

        if (excludeAppointmentId.HasValue)
        {
            query = query.Where(x => x.AppointmentId != excludeAppointmentId.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task SyncDoctorAvailabilitySummaryAsync(long doctorId, CancellationToken cancellationToken = default)
    {
        var doctor = await _db.Doctors.FirstOrDefaultAsync(x => x.DoctorId == doctorId, cancellationToken);
        if (doctor is null)
        {
            return;
        }

        var schedules = await _db.DoctorSchedules
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId && x.IsActive)
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.StartTime)
            .ToListAsync(cancellationToken);

        if (schedules.Count == 0)
        {
            doctor.OpdDays = null;
            doctor.OpdStartTime = null;
            doctor.OpdEndTime = null;
            doctor.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        doctor.OpdDays = string.Join(",", schedules.Select(x => ToDayToken(x.DayOfWeek)).Distinct());
        doctor.OpdStartTime = schedules.Min(x => x.StartTime);
        doctor.OpdEndTime = schedules.Max(x => x.EndTime);
        doctor.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static byte ToScheduleDayOfWeek(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Sunday => 1,
            DayOfWeek.Monday => 2,
            DayOfWeek.Tuesday => 3,
            DayOfWeek.Wednesday => 4,
            DayOfWeek.Thursday => 5,
            DayOfWeek.Friday => 6,
            _ => 7
        };
    }

    private static string ToDayToken(byte dayOfWeek)
    {
        return dayOfWeek switch
        {
            1 => "sun",
            2 => "mon",
            3 => "tue",
            4 => "wed",
            5 => "thu",
            6 => "fri",
            _ => "sat"
        };
    }

    private static bool IsDoctorAvailableOnDay(string? opdDays, DayOfWeek dayOfWeek)
    {
        if (string.IsNullOrWhiteSpace(opdDays))
        {
            return false;
        }

        var normalized = opdDays.ToLowerInvariant();
        var token = dayOfWeek switch
        {
            DayOfWeek.Sunday => "sun",
            DayOfWeek.Monday => "mon",
            DayOfWeek.Tuesday => "tue",
            DayOfWeek.Wednesday => "wed",
            DayOfWeek.Thursday => "thu",
            DayOfWeek.Friday => "fri",
            _ => "sat"
        };

        return normalized.Contains(token);
    }

    private static bool OverlapsBreak(ResolvedDoctorSchedule schedule, TimeSpan slotStartTime, TimeSpan slotEndTime)
    {
        if (!schedule.BreakStartTime.HasValue || !schedule.BreakEndTime.HasValue)
        {
            return false;
        }

        return slotStartTime < schedule.BreakEndTime.Value && schedule.BreakStartTime.Value < slotEndTime;
    }
}
