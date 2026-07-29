namespace ProductApi.Domain.Entities;

/// <summary>
/// Represents stock/inventory quantity tied to a Product. Maps to dbo.Item.
/// </summary>
public class Item : BaseEntity
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }

    public Product? Product { get; set; }
}
