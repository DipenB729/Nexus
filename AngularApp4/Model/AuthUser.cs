using System.ComponentModel.DataAnnotations;

namespace AngularApp4.Model
{
    public enum AccountRole
    {
        Admin = 1,
        User = 2
    }

    public class AuthUser
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(256)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public AccountRole Role { get; set; } = AccountRole.User;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
