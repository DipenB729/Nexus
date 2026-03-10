namespace AngularApp4.Model;

public class BookingRecord
{
    public int Id { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string Time { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public decimal Price { get; set; }
}
