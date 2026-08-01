using WooFoodIntegration.Application.DTOs;
using WooFoodIntegration.Domain.Models;

namespace WooFoodIntegration.Application.Interfaces
{
    public interface IApiKeyService
    {
        Task<string> GenerateApiKeyAsync(CreateApiKeyDto createApiKeyDto, CancellationToken cancellationToken);
        Task<ApiKey> GetApiKeyAsync(string key, CancellationToken cancellationToken);
        Task<bool> RevokeApiKeyAsync(string key, CancellationToken cancellationToken);
    }
}
