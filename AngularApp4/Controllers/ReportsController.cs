using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = "AdminOnly")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("dashboard-summary")]
    public async Task<ActionResult<ApiResponse<object>>> DashboardSummary()
    {
        var today = DateTime.UtcNow.Date;
        var data = new
        {
            TotalDoctors = await _db.Doctors.CountAsync(),
            TotalStaff = await _db.Staff.CountAsync(),
            TotalUsers = await _db.Users.CountAsync(),
            TotalAppointments = await _db.Appointments.CountAsync(),
            TodayAppointments = await _db.Appointments.CountAsync(x => x.AppointmentDate == today),
            PendingAppointments = await _db.Appointments.CountAsync(x => x.Status == AppointmentStatus.Pending)
        };

        return Ok(ApiResponse<object>.Ok(data));
    }
}
