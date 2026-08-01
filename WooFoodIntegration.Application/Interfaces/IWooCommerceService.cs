using WooFoodIntegration.Application.DTOs;

namespace WooFoodIntegration.Application.Interfaces
{
    public interface IWooCommerceService
    {
        Task<bool> UpdateProductStockAsync(Guid tenantId, WooCommerceStockUpdateDto stockUpdateDto, CancellationToken cancellationToken);
        // Other WooCommerce specific API calls, e.g., get product details, verify webhook signature
    }
}
