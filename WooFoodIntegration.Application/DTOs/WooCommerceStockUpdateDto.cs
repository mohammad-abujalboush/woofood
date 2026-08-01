namespace WooFoodIntegration.Application.DTOs
{
    public class WooCommerceStockUpdateDto
    {
        public required string ProductId { get; set; }
        public int StockQuantity { get; set; }
        public bool ManageStock { get; set; } = true;
    }
}
