using WooFoodIntegration.Application.DTOs;
using WooFoodIntegration.Domain.Models;

namespace WooFoodIntegration.Application.Interfaces
{
    public interface ISynchronizationService
    {
        Task<SynchronizationLogDto> ProcessWooCommerceOrderCreatedAsync(Guid tenantId, WooCommerceOrderWebhookDto orderWebhook, CancellationToken cancellationToken);
        Task<SynchronizationLogDto> ProcessWooCommerceOrderUpdatedAsync(Guid tenantId, WooCommerceOrderWebhookDto orderWebhook, CancellationToken cancellationToken);
        Task<List<SynchronizationLogDto>> SyncFoodicsStockToWooCommerceAsync(Guid tenantId, CancellationToken cancellationToken);
        Task<SynchronizationLogDto> GetSynchronizationStatusAsync(Guid tenantId, Guid syncLogId, CancellationToken cancellationToken);
    }
}
