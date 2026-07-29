namespace ProductApi.Domain.Events;

/// <summary>
/// Raised (and logged) when an item's quantity drops to/below a low-stock threshold.
/// Kept intentionally simple - a lightweight domain event without a full event bus,
/// wired up through the logging pipeline in ItemService.
/// </summary>
public sealed class ProductQuantityLowEvent
{
    public int ProductId { get; }
    public int Quantity { get; }
    public DateTime OccurredOn { get; }

    public ProductQuantityLowEvent(int productId, int quantity)
    {
        ProductId = productId;
        Quantity = quantity;
        OccurredOn = DateTime.UtcNow;
    }
}
