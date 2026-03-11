using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AngularApp4.Model
{
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
    }
}
