using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ProductApi.Application.DTOs;
using ProductApi.Application.Interfaces;
using ProductApi.Application.Mapping;
using ProductApi.Application.Services;
using ProductApi.Domain.Entities;
using ProductApi.Domain.Exceptions;
using ProductApi.Infrastructure.Data;
using Xunit;

namespace ProductApi.Application.Tests;

public class ItemServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ItemService _sut;

    public ItemServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        _uow = new UnitOfWork(_context);

        // AutoMapper 13+ removed ILoggerFactory constructor overload
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        _mapper = mapperConfig.CreateMapper();

        _sut = new ItemService(
            _uow,
            _mapper,
            NullLogger<ItemService>.Instance);
    }

    private async Task<Product> SeedProductAsync()
    {
        var product = new Product
        {
            ProductName = "Seed Product",
            CreatedBy = "seed",
            CreatedOn = DateTime.UtcNow
        };

        _context.Products.Add(product);

        await _context.SaveChangesAsync();

        return product;
    }

    [Fact]
    public async Task CreateAsync_Should_Throw_NotFound_When_Product_Does_Not_Exist()
    {
        var dto = new CreateItemDto
        {
            ProductId = 12345,
            Quantity = 10
        };

        var act = async () => await _sut.CreateAsync(dto, "tester");

        await act.Should()
            .ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_Should_Persist_Item_Linked_To_Product()
    {
        var product = await SeedProductAsync();

        var dto = new CreateItemDto
        {
            ProductId = product.Id,
            Quantity = 20
        };

        var result = await _sut.CreateAsync(dto, "tester");

        result.ProductId.Should()
            .Be(product.Id);

        result.Quantity.Should()
            .Be(20);

        result.ProductName.Should()
            .Be(product.ProductName);
    }

    [Fact]
    public async Task UpdateAsync_Should_Update_Quantity_And_ModifiedBy()
    {
        var product = await SeedProductAsync();

        var item = new Item
        {
            ProductId = product.Id,
            Quantity = 5,
            CreatedBy = "seed",
            CreatedOn = DateTime.UtcNow
        };

        _context.Items.Add(item);

        await _context.SaveChangesAsync();

        var result = await _sut.UpdateAsync(
            item.Id,
            new UpdateItemDto
            {
                Quantity = 50
            },
            "updater");

        result.Quantity.Should()
            .Be(50);

        var stored = await _context.Items.FindAsync(item.Id);

        stored!.ModifiedBy.Should()
            .Be("updater");
    }

    [Fact]
    public async Task DeleteAsync_Should_Remove_Item()
    {
        var product = await SeedProductAsync();

        var item = new Item
        {
            ProductId = product.Id,
            Quantity = 5,
            CreatedBy = "seed",
            CreatedOn = DateTime.UtcNow
        };

        _context.Items.Add(item);

        await _context.SaveChangesAsync();

        await _sut.DeleteAsync(item.Id);

        (await _context.Items.FindAsync(item.Id))
            .Should()
            .BeNull();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();

        _context.Dispose();

        GC.SuppressFinalize(this);
    }
}