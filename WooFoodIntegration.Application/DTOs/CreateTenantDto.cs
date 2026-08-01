using System.ComponentModel.DataAnnotations;

namespace WooFoodIntegration.Application.DTOs
{
    public class CreateTenantDto
    {
        [Required]
        public required string Name { get; set; }

        [Required]
        [Url]
        public required string WooCommerceBaseUrl { get; set; }

        [Required]
        [Url]
        public required string FoodicsBaseUrl { get; set; }
    }
}
