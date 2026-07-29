namespace ProductApi.Domain.Entities;

/// <summary>
/// Minimal application user store backing JWT authentication.
/// Deliberately lightweight (no ASP.NET Core Identity dependency) to keep the
/// Domain layer framework-free, per Clean Architecture guidance.
/// </summary>
public class ApplicationUser
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
