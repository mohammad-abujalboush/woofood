using System.Linq.Expressions;

namespace WooFoodIntegration.Domain.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);
        Task AddAsync(T entity, CancellationToken cancellationToken);
        Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken);
        Task UpdateAsync(T entity, CancellationToken cancellationToken);
        Task RemoveAsync(T entity, CancellationToken cancellationToken);
        Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken);
    }
}
