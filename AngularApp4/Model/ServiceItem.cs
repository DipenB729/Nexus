namespace AngularApp4.Model;

public class ServiceItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Icon { get; set; } = "medical_services";
    public string Color { get; set; } = "#3b82f6";
}
