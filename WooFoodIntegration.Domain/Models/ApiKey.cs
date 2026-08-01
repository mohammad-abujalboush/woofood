using System.ComponentModel.DataAnnotations;

namespace WooFoodIntegration.Domain.Models
{
    public class ApiKey
    {
        public Guid Id { get; set; }
        public Guid? TenantId { get; set; } // Optional: An API key can be global or tenant-specific

        [Required]
        public required string Key { get; set; } // Hashed API Key

        [Required]
        public required string Label { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; } // Nullable: API key might not expire

        // Navigation property
        public Tenant? Tenant { get; set; }
    }
}
