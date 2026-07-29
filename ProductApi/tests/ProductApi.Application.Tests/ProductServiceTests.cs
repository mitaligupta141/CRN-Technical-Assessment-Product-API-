using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

/// <summary>
/// Exercises ProductService against a real EF Core InMemory-backed UnitOfWork,
/// giving broader coverage than mocking every repository call while staying fast and isolated.
/// </summary>
public class ProductServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ProductService _sut;

    public ProductServiceTests()
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

        _sut = new ProductService(
            _uow,
            _mapper,
            NullLogger<ProductService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_Should_Persist_New_Product()
    {
        var dto = new CreateProductDto
        {
            ProductName = "Wireless Mouse"
        };

        var result = await _sut.CreateAsync(dto, "tester");

        result.Id.Should().BeGreaterThan(0);
        result.ProductName.Should().Be("Wireless Mouse");

        (await _context.Products.CountAsync())
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Throw_NotFoundException_When_Product_Missing()
    {
        var act = async () => await _sut.GetByIdAsync(999);

        await act.Should()
            .ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Product_With_TotalQuantity_Summed_From_Items()
    {
        var product = new Product
        {
            ProductName = "Keyboard",
            CreatedBy = "seed",
            CreatedOn = DateTime.UtcNow
        };

        product.Items.Add(new Item
        {
            Quantity = 5,
            CreatedBy = "seed",
            CreatedOn = DateTime.UtcNow
        });

        product.Items.Add(new Item
        {
            Quantity = 3,
            CreatedBy = "seed",
            CreatedOn = DateTime.UtcNow
        });

        _context.Products.Add(product);

        await _context.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(product.Id);

        result.TotalQuantity.Should().Be(8);
    }

    [Fact]
    public async Task DeleteAsync_Should_SoftDelete_Product_Not_Remove_Row()
    {
        var product = new Product
        {
            ProductName = "Monitor",
            CreatedBy = "seed",
            CreatedOn = DateTime.UtcNow
        };

        _context.Products.Add(product);

        await _context.SaveChangesAsync();

        await _sut.DeleteAsync(product.Id);

        var stored = await _context.Products.FindAsync(product.Id);

        stored.Should().NotBeNull();
        stored!.IsDeleted.Should().BeTrue();

        var act = async () => await _sut.GetByIdAsync(product.Id);

        await act.Should()
            .ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAllAsync_Should_Filter_By_Search_Term_Case_Insensitively()
    {
        _context.Products.AddRange(
            new Product
            {
                ProductName = "USB Cable",
                CreatedBy = "seed",
                CreatedOn = DateTime.UtcNow
            },
            new Product
            {
                ProductName = "HDMI Cable",
                CreatedBy = "seed",
                CreatedOn = DateTime.UtcNow
            },
            new Product
            {
                ProductName = "Laptop Stand",
                CreatedBy = "seed",
                CreatedOn = DateTime.UtcNow
            });

        await _context.SaveChangesAsync();

        var result = await _sut.GetAllAsync(
            new PaginationQuery
            {
                Search = "cable",
                PageNumber = 1,
                PageSize = 10
            });

        result.TotalCount.Should().Be(2);

        result.Items.Should()
            .OnlyContain(p => p.ProductName.ToLower().Contains("cable"));
    }

    [Fact]
    public async Task GetAllAsync_Should_Respect_Pagination()
    {
        for (var i = 1; i <= 15; i++)
        {
            _context.Products.Add(new Product
            {
                ProductName = $"Product {i}",
                CreatedBy = "seed",
                CreatedOn = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        var page2 = await _sut.GetAllAsync(
            new PaginationQuery
            {
                PageNumber = 2,
                PageSize = 10
            });

        page2.Items.Should()
            .HaveCount(5);

        page2.TotalCount.Should()
            .Be(15);

        page2.TotalPages.Should()
            .Be(2);

        page2.HasPreviousPage.Should()
            .BeTrue();

        page2.HasNextPage.Should()
            .BeFalse();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();

        _context.Dispose();

        GC.SuppressFinalize(this);
    }
}