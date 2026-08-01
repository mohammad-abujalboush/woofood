namespace WooFoodIntegration.Application.DTOs
{
    public class FoodicsStockUpdateDto
    {
        public required string ProductReference { get; set; } // WooCommerce Product ID or SKU
        public int NewQuantity { get; set; }
        // Other fields like warehouse ID if necessary for Foodics
    }
}
