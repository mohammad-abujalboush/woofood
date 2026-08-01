namespace WooFoodIntegration.Domain.Models
{
    public class Tenant
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string WooCommerceBaseUrl { get; set; }
        public required string FoodicsBaseUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TenantCredential> TenantCredentials { get; set; } = new List<TenantCredential>();
        public ICollection<SynchronizationLog> SynchronizationLogs { get; set; } = new List<SynchronizationLog>();
        public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
    }
}
