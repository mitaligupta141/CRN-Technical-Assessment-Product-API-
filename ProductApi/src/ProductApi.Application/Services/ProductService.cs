using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductApi.Application.DTOs;
using ProductApi.Application.Interfaces;
using ProductApi.Domain.Entities;
using ProductApi.Domain.Exceptions;

namespace ProductApi.Application.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IUnitOfWork uow, IMapper mapper, ILogger<ProductService> logger)
    {
        _uow = uow;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<ProductDto>> GetAllAsync(PaginationQuery query, CancellationToken ct = default)
    {
        // AsNoTracking() for read-only queries, per the performance requirement.
        var baseQuery = _uow.Products.Query(asNoTracking: true)
            .Include(p => p.Items)
            .Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            baseQuery = baseQuery.Where(p => p.ProductName.ToLower().Contains(term));
        }

        var totalCount = await baseQuery.CountAsync(ct);

        var products = await baseQuery
            .OrderBy(p => p.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<ProductDto>
        {
            Items = _mapper.Map<List<ProductDto>>(products),
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProductDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var product = await _uow.Products.Query(asNoTracking: true)
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

        if (product is null)
            throw new NotFoundException(nameof(Product), id);

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<IReadOnlyList<ItemDto>> GetItemsForProductAsync(int productId, CancellationToken ct = default)
    {
        var exists = await _uow.Products.AnyAsync(p => p.Id == productId && !p.IsDeleted, ct);
        if (!exists)
            throw new NotFoundException(nameof(Product), productId);

        var items = await _uow.Items.Query(asNoTracking: true)
            .Where(i => i.ProductId == productId)
            .ToListAsync(ct);

        return _mapper.Map<List<ItemDto>>(items);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto, string user, CancellationToken ct = default)
    {
        var product = _mapper.Map<Product>(dto);
        product.CreatedBy = user;
        product.CreatedOn = DateTime.UtcNow;

        await _uow.Products.AddAsync(product, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Product {ProductId} created by {User}", product.Id, user);

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductDto dto, string user, CancellationToken ct = default)
    {
        var product = await _uow.Products.Query(asNoTracking: false)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

        if (product is null)
            throw new NotFoundException(nameof(Product), id);

        product.ProductName = dto.ProductName;
        product.ModifiedBy = user;
        product.ModifiedOn = DateTime.UtcNow;

        _uow.Products.Update(product);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Product {ProductId} updated by {User}", product.Id, user);

        return _mapper.Map<ProductDto>(product);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var product = await _uow.Products.Query(asNoTracking: false)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

        if (product is null)
            throw new NotFoundException(nameof(Product), id);

        // Soft delete keeps referential integrity with existing Items intact.
        product.IsDeleted = true;
        product.ModifiedOn = DateTime.UtcNow;

        _uow.Products.Update(product);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Product {ProductId} soft-deleted", id);
    }
}
