using System.ComponentModel.DataAnnotations;

namespace WooFoodIntegration.Domain.Models
{
    public class TenantCredential
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        [Required]
        public required string SystemType { get; set; } // e.g., "WooCommerce", "Foodics"

        [Required]
        public required string EncryptedApiKey { get; set; }

        // Store the secret without breaking the single IV/Salt architecture
        public string? ApiSecret { get; set; }

        [Required]
        public required string Iv { get; set; } // Initialization Vector for encryption

        [Required]
        public required string Salt { get; set; } // Salt for key derivation

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Tenant? Tenant { get; set; }
    }
}