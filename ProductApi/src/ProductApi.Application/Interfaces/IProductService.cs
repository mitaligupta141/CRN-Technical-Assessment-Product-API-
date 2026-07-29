using ProductApi.Application.DTOs;

namespace ProductApi.Application.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetAllAsync(PaginationQuery query, CancellationToken ct = default);
    Task<ProductDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ItemDto>> GetItemsForProductAsync(int productId, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(CreateProductDto dto, string user, CancellationToken ct = default);
    Task<ProductDto> UpdateAsync(int id, UpdateProductDto dto, string user, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
