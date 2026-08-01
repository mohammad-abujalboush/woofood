using WooFoodIntegration.Domain.Models;

namespace WooFoodIntegration.Domain.Repositories
{
    public interface ITenantRepository : IRepository<Tenant>
    {
        // Add tenant-specific methods here if any, e.g., GetTenantByNameAsync
    }
}
