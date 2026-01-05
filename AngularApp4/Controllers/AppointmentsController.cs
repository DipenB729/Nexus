using Microsoft.AspNetCore.Mvc;
using AngularApp4.Model;

namespace AngularApp4.Controllers
{
    [ApiController]
    [Route("api/appointments")]
    public class AppointmentsController : ControllerBase
    {
        
        private static List<Appointment> _appointments = new List<Appointment>
        {
            new Appointment {
                Id = 101,
                CustomerName = "John Doe",
                CustomerEmail = "john@example.com",
                StartTime = DateTime.Now.AddDays(1).AddHours(9),
                EndTime = DateTime.Now.AddDays(1).AddHours(10),
                Status = AppointmentStatus.Confirmed,
                ServiceId = 1,
                Service = new Service { Name = "Initial Consultation", Color = "#3b82f6" }
            },
            new Appointment {
                Id = 102,
                CustomerName = "Sarah Smith",
                CustomerEmail = "sarah@test.com",
                StartTime = DateTime.Now.AddDays(2).AddHours(14),
                EndTime = DateTime.Now.AddDays(2).AddHours(15),
                Status = AppointmentStatus.Pending,
                ServiceId = 2,
                Service = new Service { Name = "Deep Tissue Massage", Color = "#8b5cf6" }
            }
        };

        [HttpGet]
        public IActionResult GetAppointments()
        {
            return Ok(_appointments);
        }
    }
}