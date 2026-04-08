using System.ComponentModel.DataAnnotations;

namespace AngularApp4.Model
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        // Customer Info
        [Required]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string CustomerEmail { get; set; } = string.Empty;

        public string? CustomerPhone { get; set; }

        // Booking Info
        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; } // Calculated based on Service Duration

        [Required]
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        public string? Notes { get; set; }

        // Relationship: One Service belongs to many Appointments
        [Required]
        public int ServiceId { get; set; }
        public virtual Service? Service { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
