namespace WooFoodIntegration.Application.DTOs
{
    public class WooCommerceOrderWebhookDto
    {
        public long Id { get; set; }
        public required string Status { get; set; }
        public decimal Total { get; set; }
        public required string Currency { get; set; }
        public required List<LineItemDto> LineItems { get; set; }
        public required BillingDto Billing { get; set; }
        public required ShippingDto Shipping { get; set; }

        public class LineItemDto
        {
            public long Id { get; set; }
            public required string Name { get; set; }
            public required string ProductId { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public decimal Total { get; set; }
        }

        public class BillingDto
        {
            public required string FirstName { get; set; }
            public required string LastName { get; set; }
            public required string Address1 { get; set; }
            public string? Address2 { get; set; }
            public required string City { get; set; }
            public required string State { get; set; }
            public required string Postcode { get; set; }
            public required string Country { get; set; }
            public required string Email { get; set; }
            public required string Phone { get; set; }
        }

        public class ShippingDto
        {
            public required string FirstName { get; set; }
            public required string LastName { get; set; }
            public required string Address1 { get; set; }
            public string? Address2 { get; set; }
            public required string City { get; set; }
            public required string State { get; set; }
            public required string Postcode { get; set; }
            public required string Country { get; set; }
        }
    }
}
