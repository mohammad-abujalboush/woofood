using WooFoodIntegration.Domain.Models;

namespace WooFoodIntegration.Domain.Repositories
{
    public interface IApiKeyRepository : IRepository<ApiKey>
    {
        Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken);
    }
}
