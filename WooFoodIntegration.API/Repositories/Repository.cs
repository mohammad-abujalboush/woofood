using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WooFoodIntegration.API.Data;
using WooFoodIntegration.Domain.Repositories;

namespace WooFoodIntegration.API.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly AppDbContext _context;

        public Repository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(T entity, CancellationToken cancellationToken)
        {
            await _context.Set<T>().AddAsync(entity, cancellationToken);
        }

        public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken)
        {
            await _context.Set<T>().AddRangeAsync(entities, cancellationToken);
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        {
            return await _context.Set<T>().Where(predicate).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.Set<T>().ToListAsync(cancellationToken);
        }

        public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Set<T>().FindAsync(new object[] { id }, cancellationToken);
        }

        public Task RemoveAsync(T entity, CancellationToken cancellationToken)
        {
            _context.Set<T>().Remove(entity);
            return Task.CompletedTask;
        }

        public Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken)
        {
            _context.Set<T>().RemoveRange(entities);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(T entity, CancellationToken cancellationToken)
        {
            _context.Set<T>().Update(entity);
            return Task.CompletedTask;
        }
    }
}
