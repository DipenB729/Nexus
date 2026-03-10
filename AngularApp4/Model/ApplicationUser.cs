using Microsoft.AspNetCore.Identity;

namespace AngularApp4.Model;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
