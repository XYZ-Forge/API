namespace XYZForge.Models
{
    public record User
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
    }

    public record UserRegistration(string Username, string Password, string? Role = "User");
    public record UserLogin(string Username, string Password);
    public record UserUpdate(string IssuerJWT, string Username, string TargetRole, string TargetUsername, string TargetPassword);
}
