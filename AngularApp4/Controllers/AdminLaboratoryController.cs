using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/admin/laboratory")]
[Authorize(Policy = "AdminOnly")]
public class AdminLaboratoryController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminLaboratoryController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("lab-tests")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LabTestMasterDto>>>> GetLabTests()
    {
        var items = await _db.LabTestMasters
            .AsNoTracking()
            .OrderBy(x => x.DepartmentName)
            .ThenBy(x => x.TestName)
            .Select(x => MapLabTest(x))
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<LabTestMasterDto>>.Ok(items));
    }

    [HttpPost("lab-tests")]
    public async Task<ActionResult<ApiResponse<LabTestMasterDto>>> CreateLabTest([FromBody] SaveLabTestMasterDto dto)
    {
        var testName = Normalize(dto.TestName);
        var departmentName = Normalize(dto.DepartmentName);
        var sampleType = Normalize(dto.SampleType);
        var reportFormat = Normalize(dto.ReportFormat);

        if (string.IsNullOrWhiteSpace(testName) ||
            string.IsNullOrWhiteSpace(departmentName) ||
            string.IsNullOrWhiteSpace(sampleType) ||
            string.IsNullOrWhiteSpace(reportFormat))
        {
            return BadRequest(ApiResponse<LabTestMasterDto>.Fail("Test name, department, sample type, and report format are required"));
        }

        if (await _db.LabTestMasters.AnyAsync(x => x.TestName == testName && x.DepartmentName == departmentName))
        {
            return BadRequest(ApiResponse<LabTestMasterDto>.Fail("A lab test with this name already exists for the selected department"));
        }

        var item = new LabTestMaster
        {
            TestName = testName,
            DepartmentName = departmentName,
            Price = Math.Max(dto.Price, 0m),
            SampleType = sampleType,
            ReportFormat = reportFormat,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.LabTestMasters.Add(item);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<LabTestMasterDto>.Ok(MapLabTest(item), "Lab test created"));
    }

    [HttpPut("lab-tests/{labTestMasterId:long}")]
    public async Task<ActionResult<ApiResponse<LabTestMasterDto>>> UpdateLabTest(long labTestMasterId, [FromBody] SaveLabTestMasterDto dto)
    {
        var item = await _db.LabTestMasters.FirstOrDefaultAsync(x => x.LabTestMasterId == labTestMasterId);
        if (item is null)
        {
            return NotFound(ApiResponse<LabTestMasterDto>.Fail("Lab test not found"));
        }

        var testName = Normalize(dto.TestName);
        var departmentName = Normalize(dto.DepartmentName);
        var sampleType = Normalize(dto.SampleType);
        var reportFormat = Normalize(dto.ReportFormat);

        if (string.IsNullOrWhiteSpace(testName) ||
            string.IsNullOrWhiteSpace(departmentName) ||
            string.IsNullOrWhiteSpace(sampleType) ||
            string.IsNullOrWhiteSpace(reportFormat))
        {
            return BadRequest(ApiResponse<LabTestMasterDto>.Fail("Test name, department, sample type, and report format are required"));
        }

        var exists = await _db.LabTestMasters.AnyAsync(x =>
            x.LabTestMasterId != labTestMasterId &&
            x.TestName == testName &&
            x.DepartmentName == departmentName);

        if (exists)
        {
            return BadRequest(ApiResponse<LabTestMasterDto>.Fail("A lab test with this name already exists for the selected department"));
        }

        item.TestName = testName;
        item.DepartmentName = departmentName;
        item.Price = Math.Max(dto.Price, 0m);
        item.SampleType = sampleType;
        item.ReportFormat = reportFormat;
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<LabTestMasterDto>.Ok(MapLabTest(item), "Lab test updated"));
    }

    [HttpGet("packages")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ServicePackageDto>>>> GetPackages([FromQuery] string? kind = null)
    {
        var items = await _db.ServicePackages
            .AsNoTracking()
            .OrderBy(x => x.Kind)
            .ThenBy(x => x.PackageName)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(kind))
        {
            var filterKind = ParseEnum(kind, ServicePackageKind.HealthPackage);
            items = items.Where(x => x.Kind == filterKind).ToList();
        }

        return Ok(ApiResponse<IEnumerable<ServicePackageDto>>.Ok(items.Select(MapPackage).ToList()));
    }

    [HttpPost("packages")]
    public async Task<ActionResult<ApiResponse<ServicePackageDto>>> CreatePackage([FromBody] SaveServicePackageDto dto)
    {
        var kind = ParseEnum(dto.Kind, ServicePackageKind.HealthPackage);
        var packageName = Normalize(dto.PackageName);

        if (string.IsNullOrWhiteSpace(packageName))
        {
            return BadRequest(ApiResponse<ServicePackageDto>.Fail("Package name is required"));
        }

        if (await _db.ServicePackages.AnyAsync(x => x.Kind == kind && x.PackageName == packageName))
        {
            return BadRequest(ApiResponse<ServicePackageDto>.Fail("A package with this name already exists in the selected category"));
        }

        var item = new ServicePackage
        {
            Kind = kind,
            PackageName = packageName,
            DepartmentName = Normalize(dto.DepartmentName),
            Price = Math.Max(dto.Price, 0m),
            DiscountAmount = ClampDiscount(dto.Price, dto.DiscountAmount),
            Description = Normalize(dto.Description),
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.ServicePackages.Add(item);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<ServicePackageDto>.Ok(MapPackage(item), "Package created"));
    }

    [HttpPut("packages/{servicePackageId:long}")]
    public async Task<ActionResult<ApiResponse<ServicePackageDto>>> UpdatePackage(long servicePackageId, [FromBody] SaveServicePackageDto dto)
    {
        var item = await _db.ServicePackages.FirstOrDefaultAsync(x => x.ServicePackageId == servicePackageId);
        if (item is null)
        {
            return NotFound(ApiResponse<ServicePackageDto>.Fail("Package not found"));
        }

        var kind = ParseEnum(dto.Kind, item.Kind);
        var packageName = Normalize(dto.PackageName);

        if (string.IsNullOrWhiteSpace(packageName))
        {
            return BadRequest(ApiResponse<ServicePackageDto>.Fail("Package name is required"));
        }

        var exists = await _db.ServicePackages.AnyAsync(x =>
            x.ServicePackageId != servicePackageId &&
            x.Kind == kind &&
            x.PackageName == packageName);

        if (exists)
        {
            return BadRequest(ApiResponse<ServicePackageDto>.Fail("A package with this name already exists in the selected category"));
        }

        item.Kind = kind;
        item.PackageName = packageName;
        item.DepartmentName = Normalize(dto.DepartmentName);
        item.Price = Math.Max(dto.Price, 0m);
        item.DiscountAmount = ClampDiscount(dto.Price, dto.DiscountAmount);
        item.Description = Normalize(dto.Description);
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<ServicePackageDto>.Ok(MapPackage(item), "Package updated"));
    }

    private static LabTestMasterDto MapLabTest(LabTestMaster item)
    {
        return new LabTestMasterDto
        {
            LabTestMasterId = item.LabTestMasterId,
            TestName = item.TestName,
            DepartmentName = item.DepartmentName,
            Price = item.Price,
            SampleType = item.SampleType,
            ReportFormat = item.ReportFormat,
            IsActive = item.IsActive
        };
    }

    private static ServicePackageDto MapPackage(ServicePackage item)
    {
        return new ServicePackageDto
        {
            ServicePackageId = item.ServicePackageId,
            Kind = item.Kind.ToString(),
            PackageName = item.PackageName,
            DepartmentName = item.DepartmentName,
            Price = item.Price,
            DiscountAmount = item.DiscountAmount,
            NetAmount = Math.Max(item.Price - item.DiscountAmount, 0m),
            Description = item.Description,
            IsActive = item.IsActive
        };
    }

    private static decimal ClampDiscount(decimal price, decimal discountAmount)
    {
        return Math.Clamp(Math.Max(discountAmount, 0m), 0m, Math.Max(price, 0m));
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct
    {
        return Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : fallback;
    }
}
