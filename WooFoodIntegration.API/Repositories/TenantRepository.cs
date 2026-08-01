using WooFoodIntegration.API.Data;
using WooFoodIntegration.Domain.Models;
using WooFoodIntegration.Domain.Repositories;

namespace WooFoodIntegration.API.Repositories
{
    public class TenantRepository : Repository<Tenant>, ITenantRepository
    {
        public TenantRepository(AppDbContext context) : base(context) { }
    }
}
