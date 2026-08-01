namespace WooFoodIntegration.Application.DTOs
{
    public class FoodicsOrderCreateDto
    {
        public required string Reference { get; set; } // WooCommerce Order ID
        public decimal TotalPrice { get; set; }
        public string? CustomerNotes { get; set; }
        public required string Status { get; set; } // e.g., "pending"
        public required List<FoodicsLineItemDto> Products { get; set; }
        public required FoodicsCustomerDto Customer { get; set; }
        // Additional fields as required by Foodics API

        public class FoodicsLineItemDto
        {
            public required string ProductReference { get; set; } // WooCommerce Product ID or SKU
            public int Quantity { get; set; }
            public decimal Price { get; set; }
        }

        public class FoodicsCustomerDto
        {
            public required string Name { get; set; }
            public string? Phone { get; set; }
            public string? Email { get; set; }
        }
    }
}
