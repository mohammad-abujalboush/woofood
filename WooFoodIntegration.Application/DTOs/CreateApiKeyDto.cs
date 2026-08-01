using System.ComponentModel.DataAnnotations;

namespace WooFoodIntegration.Application.DTOs
{
    public class CreateApiKeyDto
    {
        public Guid? TenantId { get; set; }

        [Required]
        public required string Label { get; set; }

        public DateTime? ExpiresAt { get; set; }
    }
}
