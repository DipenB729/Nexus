using AngularApp4.Model;
 
using Microsoft.AspNetCore.Mvc;

namespace AngularApp4.Controllers
{
    [ApiController]
    [Route("api/services")]
    public class ServicesController : ControllerBase
    {
        
        private static List<Service> _services = new List<Service>
        {
            new Service { Id = 1, Name = "Initial Consultation", Category = "Health", DurationMinutes = 45, Price = 75.00m, Icon = "medical_services", Color = "#3b82f6", Description = "Comprehensive assessment" },
            new Service { Id = 2, Name = "Deep Tissue Massage", Category = "Therapy", DurationMinutes = 60, Price = 120.00m, Icon = "spa", Color = "#8b5cf6", Description = "Muscle recovery" },
            new Service { Id = 3, Name = "Wellness Coaching", Category = "Consulting", DurationMinutes = 60, Price = 90.00m, Icon = "psychology", Color = "#f59e0b", Description = "Lifestyle coaching" },
            new Service { Id = 3, Name = "Physical Coaching", Category = "Checkup", DurationMinutes = 60, Price = 90.00m, Icon = "psychology", Color = "#f59e0b", Description = "Lifestyle coaching" }
        };

        [HttpGet]
        public IActionResult GetServices()
        {
            return Ok(_services);
        }

        [HttpPost]
        public IActionResult AddService([FromBody] Service newService)
        {
            newService.Id = _services.Max(s => s.Id) + 1;
            _services.Add(newService);
            return Ok(newService);
        }
    }
}