using AngularApp4.Data;
using AngularApp4.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/doctors")]
public class DoctorsController : ControllerBase
{
    private readonly AppDbContext _db;

    public DoctorsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Doctor>>> GetAll()
    {
        return Ok(await _db.Doctors.OrderBy(d => d.FullName).ToListAsync());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Doctor>> Create([FromBody] Doctor doctor)
    {
        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync();
        return Ok(doctor);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] Doctor doctor)
    {
        var existing = await _db.Doctors.FindAsync(id);
        if (existing == null) return NotFound();

        existing.FullName = doctor.FullName;
        existing.Specialty = doctor.Specialty;
        existing.Experience = doctor.Experience;
        existing.AvailableFrom = doctor.AvailableFrom;
        existing.AvailableTo = doctor.AvailableTo;

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _db.Doctors.FindAsync(id);
        if (existing == null) return NotFound();

        _db.Doctors.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
