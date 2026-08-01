using System.ComponentModel.DataAnnotations;

namespace WooFoodIntegration.Domain.Models
{
    public class SynchronizationLog
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        [Required]
        public required string EventType { get; set; } // e.g., "OrderCreated", "StockUpdated", "ReturnProcessed"

        [Required]
        public required string SourceSystem { get; set; } // e.g., "WooCommerce", "Foodics"
        public required string SourceEntityId { get; set; } // ID of the entity in the source system

        public required string TargetSystem { get; set; }
        public required string TargetEntityId { get; set; } // ID of the entity in the target system, if applicable

        [Required]
        public required string Status { get; set; } // e.g., "Success", "Failed", "Pending"
        public required string Message { get; set; } // Detailed message, including error details if failed
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Tenant? Tenant { get; set; }
    }
}
