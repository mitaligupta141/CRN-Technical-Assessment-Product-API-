using ProductApi.Application.DTOs;

namespace ProductApi.Application.Interfaces;

public interface IItemService
{
    Task<PagedResult<ItemDto>> GetAllAsync(PaginationQuery query, CancellationToken ct = default);
    Task<ItemDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ItemDto> CreateAsync(CreateItemDto dto, string user, CancellationToken ct = default);
    Task<ItemDto> UpdateAsync(int id, UpdateItemDto dto, string user, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
