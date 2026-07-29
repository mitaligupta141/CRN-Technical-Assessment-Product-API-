namespace ProductApi.Domain.Entities;

/// <summary>
/// Represents a sellable product. Maps to dbo.Product as defined in the assessment schema.
/// </summary>
public class Product : BaseEntity
{
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Soft-delete flag. Allows "deleting" a product without losing referential
    /// history for any Items that point at it.
    /// </summary>
    public bool IsDeleted { get; set; }

    public ICollection<Item> Items { get; set; } = new List<Item>();
}
