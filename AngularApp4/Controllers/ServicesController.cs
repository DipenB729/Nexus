using AngularApp4.Data;
using AngularApp4.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/services")]
public class ServicesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ServicesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceItem>>> GetAll()
    {
        return Ok(await _db.Services.OrderByDescending(s => s.Id).ToListAsync());
    }

    [HttpPost]
    public async Task<ActionResult<ServiceItem>> Create([FromBody] ServiceItem item)
    {
        _db.Services.Add(item);
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ServiceItem item)
    {
        var existing = await _db.Services.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Name = item.Name;
        existing.Description = item.Description;
        existing.Category = item.Category;
        existing.Duration = item.Duration;
        existing.Price = item.Price;
        existing.Icon = item.Icon;
        existing.Color = item.Color;

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _db.Services.FindAsync(id);
        if (existing == null) return NotFound();

        _db.Services.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
