namespace AngularApp4.Dtos.Hms;

public class DoctorScheduleDto
{
    public long ScheduleId { get; set; }
    public long DoctorId { get; set; }
    public byte DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan? BreakStartTime { get; set; }
    public TimeSpan? BreakEndTime { get; set; }
    public int SlotDurationMinutes { get; set; }
    public int MaxPatientsPerSlot { get; set; }
    public bool OnlineBookingEnabled { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class SaveDoctorScheduleDto
{
    public byte DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan? BreakStartTime { get; set; }
    public TimeSpan? BreakEndTime { get; set; }
    public int SlotDurationMinutes { get; set; }
    public int MaxPatientsPerSlot { get; set; }
    public bool OnlineBookingEnabled { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public class DoctorAvailableSlotDto
{
    public long? ScheduleId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int MaxPatientsPerSlot { get; set; }
    public int BookedPatients { get; set; }
    public int RemainingPatients { get; set; }
}
