using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/patient-categories")]
[Authorize(Policy = "AdminOnly")]
public class PatientCategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public PatientCategoriesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<PatientCategoryDto>>>> GetAll([FromQuery] string? search = null, [FromQuery] bool? isActive = null)
    {
        var query = _db.PatientCategories.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Name.Contains(term) || (x.Description ?? string.Empty).Contains(term));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        var items = await query
            .OrderBy(x => x.PriorityOrder)
            .ThenBy(x => x.Name)
            .Select(x => new PatientCategoryDto
            {
                PatientCategoryId = x.PatientCategoryId,
                Name = x.Name,
                Description = x.Description,
                PriorityOrder = x.PriorityOrder,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<PatientCategoryDto>>.Ok(items));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PatientCategoryDto>>> Create([FromBody] SavePatientCategoryDto dto)
    {
        var item = new PatientCategory
        {
            Name = dto.Name.Trim(),
            Description = Normalize(dto.Description),
            PriorityOrder = dto.PriorityOrder,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.PatientCategories.Add(item);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<PatientCategoryDto>.Ok(Map(item), "Patient category created"));
    }

    [HttpPut("{patientCategoryId:long}")]
    public async Task<ActionResult<ApiResponse<PatientCategoryDto>>> Update(long patientCategoryId, [FromBody] SavePatientCategoryDto dto)
    {
        var item = await _db.PatientCategories.FirstOrDefaultAsync(x => x.PatientCategoryId == patientCategoryId);
        if (item is null)
        {
            return NotFound(ApiResponse<PatientCategoryDto>.Fail("Patient category not found"));
        }

        item.Name = dto.Name.Trim();
        item.Description = Normalize(dto.Description);
        item.PriorityOrder = dto.PriorityOrder;
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<PatientCategoryDto>.Ok(Map(item), "Patient category updated"));
    }

    [HttpPut("{patientCategoryId:long}/status")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateStatus(long patientCategoryId, [FromBody] StatusUpdateDto dto)
    {
        var item = await _db.PatientCategories.FirstOrDefaultAsync(x => x.PatientCategoryId == patientCategoryId);
        if (item is null)
        {
            return NotFound(ApiResponse<object>.Fail("Patient category not found"));
        }

        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(null, dto.IsActive ? "Patient category activated" : "Patient category deactivated"));
    }

    private static PatientCategoryDto Map(PatientCategory item)
    {
        return new PatientCategoryDto
        {
            PatientCategoryId = item.PatientCategoryId,
            Name = item.Name,
            Description = item.Description,
            PriorityOrder = item.PriorityOrder,
            IsActive = item.IsActive
        };
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
