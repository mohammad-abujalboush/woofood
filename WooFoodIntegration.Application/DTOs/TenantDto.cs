namespace WooFoodIntegration.Application.DTOs
{
    public class TenantDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string WooCommerceBaseUrl { get; set; }
        public required string FoodicsBaseUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
