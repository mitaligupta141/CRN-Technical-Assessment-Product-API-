using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProductApi.Domain.Entities;
using ProductApi.Infrastructure.Data;
using ProductApi.Infrastructure.Data.Repositories;
using Xunit;

namespace ProductApi.Infrastructure.Tests;

public class GenericRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GenericRepository<Product> _repository;

    public GenericRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new GenericRepository<Product>(_context);
    }

    [Fact]
    public async Task AddAsync_Then_SaveChanges_Should_Persist_Entity()
    {
        var product = new Product { ProductName = "Repo Test", CreatedBy = "seed", CreatedOn = DateTime.UtcNow };

        await _repository.AddAsync(product);
        await _context.SaveChangesAsync();

        (await _context.Products.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_For_Unknown_Id()
    {
        var result = await _repository.GetByIdAsync(9999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Query_AsNoTracking_Should_Not_Track_Returned_Entities()
    {
        var product = new Product { ProductName = "Untracked", CreatedBy = "seed", CreatedOn = DateTime.UtcNow };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var fetched = await _repository.Query(asNoTracking: true).FirstAsync(p => p.Id == product.Id);

        _context.Entry(fetched).State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task AnyAsync_Should_Reflect_Predicate_Match()
    {
        _context.Products.Add(new Product { ProductName = "Findable", CreatedBy = "seed", CreatedOn = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        (await _repository.AnyAsync(p => p.ProductName == "Findable")).Should().BeTrue();
        (await _repository.AnyAsync(p => p.ProductName == "Missing")).Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
