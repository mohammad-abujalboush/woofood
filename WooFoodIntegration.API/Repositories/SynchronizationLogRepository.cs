using WooFoodIntegration.API.Data;
using WooFoodIntegration.Domain.Models;
using WooFoodIntegration.Domain.Repositories;

namespace WooFoodIntegration.API.Repositories
{
    public class SynchronizationLogRepository : Repository<SynchronizationLog>, ISynchronizationLogRepository
    {
        public SynchronizationLogRepository(AppDbContext context) : base(context) { }
    }
}
