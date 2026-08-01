namespace WooFoodIntegration.Application.DTOs
{
    public class SynchronizationLogDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public required string EventType { get; set; }
        public required string SourceSystem { get; set; }
        public required string SourceEntityId { get; set; }
        public required string TargetSystem { get; set; }
        public required string TargetEntityId { get; set; }
        public required string Status { get; set; }
        public required string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
