using Microsoft.EntityFrameworkCore;
using WooFoodIntegration.API.Data;
using WooFoodIntegration.Domain.Models;
using WooFoodIntegration.Domain.Repositories;

namespace WooFoodIntegration.API.Repositories
{
    public class ApiKeyRepository : Repository<ApiKey>, IApiKeyRepository
    {
        public ApiKeyRepository(AppDbContext context) : base(context) { }

        public async Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken)
        {
            return await _context.ApiKeys.SingleOrDefaultAsync(ak => ak.Key == keyHash, cancellationToken);
        }
    }
}
