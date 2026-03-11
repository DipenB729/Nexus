using AngularApp4.Model;

namespace AngularApp4.Contracts.Auth
{
    public class AuthResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public AccountRole Role { get; set; }
    }
}
