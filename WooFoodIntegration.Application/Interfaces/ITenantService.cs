using WooFoodIntegration.Application.DTOs;
using WooFoodIntegration.Domain.Models;

namespace WooFoodIntegration.Application.Interfaces
{
    public interface ITenantService
    {
        Task<TenantDto> CreateTenantAsync(CreateTenantDto createTenantDto, CancellationToken cancellationToken);
        Task<TenantDto> GetTenantByIdAsync(Guid tenantId, CancellationToken cancellationToken);
        Task UpdateTenantCredentialsAsync(Guid tenantId, CreateTenantCredentialDto credentialsDto, CancellationToken cancellationToken);
        Task<TenantCredential> GetTenantCredentialAsync(Guid tenantId, string systemType, CancellationToken cancellationToken);
    }
}
