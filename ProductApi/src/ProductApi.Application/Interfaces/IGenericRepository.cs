using System.Linq.Expressions;

namespace ProductApi.Application.Interfaces;

/// <summary>
/// Generic repository abstraction so the Application layer never depends
/// directly on EF Core (kept in Infrastructure).
/// </summary>
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    IQueryable<T> Query(bool asNoTracking = true);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
}
