namespace AngularApp4.Model;

public class Doctor
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
    public string AvailableFrom { get; set; } = "09:00 AM";
    public string AvailableTo { get; set; } = "05:00 PM";
}
