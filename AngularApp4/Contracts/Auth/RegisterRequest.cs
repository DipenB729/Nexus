using AngularApp4.Model;
using System.ComponentModel.DataAnnotations;

namespace AngularApp4.Contracts.Auth
{
    public class RegisterRequest
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public AccountRole Role { get; set; } = AccountRole.User;
    }
}
