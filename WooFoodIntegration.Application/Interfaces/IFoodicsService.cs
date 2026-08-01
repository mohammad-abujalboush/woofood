using WooFoodIntegration.Application.DTOs;

namespace WooFoodIntegration.Application.Interfaces
{
    public interface IFoodicsService
    {
        Task<bool> CreateOrderAsync(Guid tenantId, FoodicsOrderCreateDto orderDto, CancellationToken cancellationToken);
        Task<bool> UpdateProductStockAsync(Guid tenantId, FoodicsStockUpdateDto stockUpdateDto, CancellationToken cancellationToken);
        Task<List<FoodicsStockUpdateDto>> GetFoodicsStockAsync(Guid tenantId, CancellationToken cancellationToken);
        // Other Foodics specific API calls
    }
}
