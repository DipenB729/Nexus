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
    public async Task<IActionResult> GetServices()
    {
        var services = await _db.Services
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(services);
    }

    [HttpPost]
    public async Task<IActionResult> AddService([FromBody] Service newService)
    {
        newService.CreatedAt = DateTime.UtcNow;
        _db.Services.Add(newService);
        await _db.SaveChangesAsync();
        return Ok(newService);
    }
}
