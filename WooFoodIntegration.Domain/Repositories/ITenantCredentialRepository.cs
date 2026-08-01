using WooFoodIntegration.Domain.Models;

namespace WooFoodIntegration.Domain.Repositories
{
    public interface ITenantCredentialRepository : IRepository<TenantCredential>
    {
        Task<TenantCredential?> GetByTenantIdAndSystemTypeAsync(Guid tenantId, string systemType, CancellationToken cancellationToken);
    }
}
