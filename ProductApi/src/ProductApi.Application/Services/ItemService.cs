using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductApi.Application.DTOs;
using ProductApi.Application.Interfaces;
using ProductApi.Domain.Entities;
using ProductApi.Domain.Exceptions;
using ProductApi.Domain.Events;

namespace ProductApi.Application.Services;

public class ItemService : IItemService
{
    private const int LowStockThreshold = 5;

    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ILogger<ItemService> _logger;

    public ItemService(IUnitOfWork uow, IMapper mapper, ILogger<ItemService> logger)
    {
        _uow = uow;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<ItemDto>> GetAllAsync(PaginationQuery query, CancellationToken ct = default)
    {
        var baseQuery = _uow.Items.Query(asNoTracking: true).Include(i => i.Product);

        var totalCount = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderBy(i => i.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<ItemDto>
        {
            Items = _mapper.Map<List<ItemDto>>(items),
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ItemDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var item = await _uow.Items.Query(asNoTracking: true)
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (item is null)
            throw new NotFoundException(nameof(Item), id);

        return _mapper.Map<ItemDto>(item);
    }

    public async Task<ItemDto> CreateAsync(CreateItemDto dto, string user, CancellationToken ct = default)
    {
        var productExists = await _uow.Products.AnyAsync(p => p.Id == dto.ProductId && !p.IsDeleted, ct);
        if (!productExists)
            throw new NotFoundException(nameof(Product), dto.ProductId);

        var item = _mapper.Map<Item>(dto);
        item.CreatedBy = user;
        item.CreatedOn = DateTime.UtcNow;

        await _uow.Items.AddAsync(item, ct);
        await _uow.SaveChangesAsync(ct);

        RaiseLowStockWarningIfNeeded(item);

        var saved = await _uow.Items.Query(asNoTracking: true)
            .Include(i => i.Product)
            .FirstAsync(i => i.Id == item.Id, ct);

        return _mapper.Map<ItemDto>(saved);
    }

    public async Task<ItemDto> UpdateAsync(int id, UpdateItemDto dto, string user, CancellationToken ct = default)
    {
        var item = await _uow.Items.Query(asNoTracking: false)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (item is null)
            throw new NotFoundException(nameof(Item), id);

        item.Quantity = dto.Quantity;
        item.ModifiedBy = user;
        item.ModifiedOn = DateTime.UtcNow;

        _uow.Items.Update(item);
        await _uow.SaveChangesAsync(ct);

        RaiseLowStockWarningIfNeeded(item);

        var saved = await _uow.Items.Query(asNoTracking: true)
            .Include(i => i.Product)
            .FirstAsync(i => i.Id == item.Id, ct);

        return _mapper.Map<ItemDto>(saved);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var item = await _uow.Items.Query(asNoTracking: false)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (item is null)
            throw new NotFoundException(nameof(Item), id);

        _uow.Items.Remove(item);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Item {ItemId} deleted", id);
    }

    private void RaiseLowStockWarningIfNeeded(Item item)
    {
        if (item.Quantity > LowStockThreshold) return;

        var domainEvent = new ProductQuantityLowEvent(item.ProductId, item.Quantity);
        _logger.LogWarning(
            "Low stock warning: Product {ProductId} quantity is {Quantity} as of {OccurredOn}",
            domainEvent.ProductId, domainEvent.Quantity, domainEvent.OccurredOn);
    }
}
