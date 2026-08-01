using Microsoft.EntityFrameworkCore;
using WooFoodIntegration.API.Data;
using WooFoodIntegration.Domain.Models;
using WooFoodIntegration.Domain.Repositories;

namespace WooFoodIntegration.API.Repositories
{
    public class TenantCredentialRepository : Repository<TenantCredential>, ITenantCredentialRepository
    {
        public TenantCredentialRepository(AppDbContext context) : base(context) { }

        public async Task<TenantCredential?> GetByTenantIdAndSystemTypeAsync(Guid tenantId, string systemType, CancellationToken cancellationToken)
        {
            return await _context.TenantCredentials
                .Where(tc => tc.TenantId == tenantId && tc.SystemType == systemType)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
