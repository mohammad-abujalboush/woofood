using WooFoodIntegration.Application.DTOs;

namespace WooFoodIntegration.Application.Interfaces
{
    public interface IWooCommerceOrderMappingService
    {
        FoodicsOrderCreateDto MapToFoodicsOrderCreateDto(WooCommerceOrderWebhookDto wooCommerceOrder, CancellationToken cancellationToken);
        // Other mapping methods as needed
    }
}
