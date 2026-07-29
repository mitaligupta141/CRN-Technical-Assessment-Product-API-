namespace ProductApi.Application.Interfaces;

using ProductApi.Domain.Entities;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<Product> Products { get; }
    IGenericRepository<Item> Items { get; }
    IGenericRepository<ApplicationUser> Users { get; }
    IGenericRepository<RefreshToken> RefreshTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
