namespace ProductApi.Domain.Entities;

/// <summary>
/// Persisted refresh token used to implement the refresh-token rotation strategy
/// referenced in the assessment's authentication requirement.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime ExpiresOn { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedOn { get; set; }
    public string? ReplacedByToken { get; set; }

    public bool IsActive => RevokedOn is null && DateTime.UtcNow < ExpiresOn;
}
