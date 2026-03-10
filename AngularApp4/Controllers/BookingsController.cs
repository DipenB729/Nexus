using AngularApp4.Data;
using AngularApp4.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public BookingsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<BookingRecord>>> GetAll()
    {
        return Ok(await _db.Bookings.OrderByDescending(b => b.Id).ToListAsync());
    }

    [HttpGet("my")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<BookingRecord>>> GetMy()
    {
        var email = User.Identity?.Name ?? User.Claims.FirstOrDefault(c => c.Type.Contains("email"))?.Value;
        if (string.IsNullOrWhiteSpace(email)) return Ok(new List<BookingRecord>());

        return Ok(await _db.Bookings
            .Where(b => b.CustomerEmail == email)
            .OrderByDescending(b => b.Id)
            .ToListAsync());
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<BookingRecord>> Create([FromBody] BookingRecord booking)
    {
        booking.BookingCode = string.IsNullOrWhiteSpace(booking.BookingCode) ? $"BK-{Random.Shared.Next(1000, 9999)}" : booking.BookingCode;
        var email = User.Identity?.Name ?? User.Claims.FirstOrDefault(c => c.Type.Contains("email"))?.Value;
        if (!string.IsNullOrWhiteSpace(email)) booking.CustomerEmail = email;

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        return Ok(booking);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] BookingRecord booking)
    {
        var existing = await _db.Bookings.FindAsync(id);
        if (existing == null) return NotFound();

        existing.CustomerName = booking.CustomerName;
        existing.CustomerEmail = booking.CustomerEmail;
        existing.DoctorName = booking.DoctorName;
        existing.ServiceName = booking.ServiceName;
        existing.AppointmentDate = booking.AppointmentDate;
        existing.Time = booking.Time;
        existing.Status = booking.Status;
        existing.Price = booking.Price;

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _db.Bookings.FindAsync(id);
        if (existing == null) return NotFound();

        _db.Bookings.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
