using System.ComponentModel.DataAnnotations;

namespace WooFoodIntegration.Application.DTOs
{
    public class CreateTenantCredentialDto
    {
        [Required]
        public Guid TenantId { get; set; }

        [Required]
        public required string SystemType { get; set; } // e.g., "WooCommerce", "Foodics"

        [Required]
        public required string ApiKey { get; set; } // Raw API Key, will be encrypted by the service

        // Add this new property as optional (since Foodics doesn't need it)
        public string? ApiSecret { get; set; } 
    }
}