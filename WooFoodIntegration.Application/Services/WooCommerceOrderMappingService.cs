using WooFoodIntegration.Application.DTOs;
using WooFoodIntegration.Application.Interfaces;

namespace WooFoodIntegration.Application.Services
{
    public class WooCommerceOrderMappingService : IWooCommerceOrderMappingService
    {
        public FoodicsOrderCreateDto MapToFoodicsOrderCreateDto(WooCommerceOrderWebhookDto wooCommerceOrder, CancellationToken cancellationToken)
        {
            var foodicsLineItems = wooCommerceOrder.LineItems.Select(item => new FoodicsOrderCreateDto.FoodicsLineItemDto
            {
                ProductReference = item.ProductId, // Assuming ProductId maps to Foodics Product Reference
                Quantity = item.Quantity,
                Price = item.Price
            }).ToList();

            // Apply business rule: WooCommerce 'processing' -> Foodics 'pending'
            string foodicsStatus = wooCommerceOrder.Status.Equals("processing", StringComparison.OrdinalIgnoreCase) ? "pending" : wooCommerceOrder.Status;

            return new FoodicsOrderCreateDto
            {
                Reference = wooCommerceOrder.Id.ToString(),
                TotalPrice = wooCommerceOrder.Total,
                CustomerNotes = "", // No direct mapping for notes in current DTO
                Status = foodicsStatus,
                Products = foodicsLineItems,
                Customer = new FoodicsOrderCreateDto.FoodicsCustomerDto
                {
                    Name = $"{wooCommerceOrder.Billing.FirstName} {wooCommerceOrder.Billing.LastName}",
                    Phone = wooCommerceOrder.Billing.Phone,
                    Email = wooCommerceOrder.Billing.Email
                }
            };
        }
    }
}