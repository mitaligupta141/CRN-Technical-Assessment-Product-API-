namespace ProductApi.Domain.Entities;

/// <summary>
/// Common audit fields shared by all persisted domain entities.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
