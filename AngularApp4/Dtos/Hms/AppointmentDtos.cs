using AngularApp4.Model.Hms;

namespace AngularApp4.Dtos.Hms;

public class AppointmentCreateDto
{
    public long DoctorId { get; set; }
    public long? ScheduleId { get; set; }
    public long? ServiceId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan SlotStartTime { get; set; }
    public TimeSpan SlotEndTime { get; set; }
    public string? Reason { get; set; }
}

public class AppointmentStatusUpdateDto
{
    public AppointmentStatus Status { get; set; }
    public string? AdminRemarks { get; set; }
}
