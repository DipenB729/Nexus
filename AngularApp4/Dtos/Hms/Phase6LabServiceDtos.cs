namespace AngularApp4.Dtos.Hms;

public class LabTestMasterDto
{
    public long LabTestMasterId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string SampleType { get; set; } = string.Empty;
    public string ReportFormat { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class SaveLabTestMasterDto
{
    public string TestName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string SampleType { get; set; } = string.Empty;
    public string ReportFormat { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class ServicePackageDto
{
    public long ServicePackageId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public decimal Price { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class SaveServicePackageDto
{
    public string Kind { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public decimal Price { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
